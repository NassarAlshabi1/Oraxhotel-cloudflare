from __future__ import annotations

import json
import re
from pathlib import Path

BASE = Path('/home/ubuntu/oraxhotel2024')
MOBILE = BASE / 'mobile'
DATA = BASE / 'tools/orax_desktop_schema_extracted.json'
RELATIONS = BASE / 'tools/orax_desktop_relationships.json'
OLD_SCHEMA = MOBILE / 'worker/schema.sql'
MIGRATIONS = MOBILE / 'worker/migrations'
SCHEMA_OUT = MOBILE / 'worker/schema.sql'

PROJECTIONS = [
    'rooms', 'bookings', 'booking_notes', 'employees', 'expenses',
    'cash_transactions', 'payments', 'debts', 'shift_notes', 'booking_nights',
    'hotel_day_ledger', 'price_adjustments', 'booking_price_adjustments',
    'audit_logs', 'payment_voids', 'guest_infos', 'salary_cycles',
    'salary_payments', 'salary_withdrawals', 'salary_carry_over_logs',
]
INFRA = ['users', 'devices', 'rate_limits', 'sync_log', 'sync_conflicts', 'idempotency_log']


def d1_type(column: dict) -> str:
    base = column['base_type'].upper()
    if base in {'INT', 'BIGINT', 'SMALLINT', 'TINYINT'}:
        return 'INTEGER'
    if base in {'FLOAT', 'REAL', 'DECIMAL', 'NUMERIC', 'MONEY', 'SMALLMONEY'}:
        return 'REAL'
    if base == 'BIT':
        return 'INTEGER'
    if base in {'DATE', 'DATETIME', 'DATETIME2', 'DATETIMEOFFSET', 'TIME'}:
        return 'TEXT'
    if base in {'NVARCHAR', 'VARCHAR', 'NCHAR', 'CHAR', 'NTEXT', 'TEXT', 'UNIQUEIDENTIFIER'}:
        return 'TEXT'
    if base in {'VARBINARY', 'BINARY', 'IMAGE'}:
        return 'BLOB'
    return 'TEXT'


def q(value: str) -> str:
    return '"' + value.replace('"', '""') + '"'


def render_desktop_table(table: dict, relationships: list[dict]) -> str:
    columns = table['columns']
    pk = table['primary_key']
    identity = {column['name'] for column in columns if column['identity']}
    lines: list[str] = [f'CREATE TABLE IF NOT EXISTS {q(table["table_name"])} (']
    definitions: list[str] = []
    for column in columns:
        name = q(column['name'])
        typ = d1_type(column)
        if len(pk) == 1 and pk[0] == column['name'] and column['identity']:
            definition = f'  {name} INTEGER PRIMARY KEY AUTOINCREMENT'
        else:
            definition = f'  {name} {typ}'
            if not column['nullable']:
                definition += ' NOT NULL'
        definitions.append(definition)
    table_relations = [
        relation for relation in relationships
        if relation['child_table'] == table['table_name']
        and relation['child_column'] in {column['name'] for column in columns}
    ]
    for relation in table_relations:
        definitions.append(
            '  FOREIGN KEY (' + q(relation['child_column']) + ') REFERENCES '
            + q(relation['parent_table']) + ' (' + q(relation['parent_column']) + ')'
        )

    if len(pk) == 1 and not identity.intersection(pk):
        for index, definition in enumerate(definitions):
            if definition.startswith(f'  {q(pk[0])} '):
                definitions[index] = definition + ' PRIMARY KEY'
                break
    elif len(pk) > 1:
        definitions.append('  PRIMARY KEY (' + ', '.join(q(value) for value in pk) + ')')
    lines.append(',\n'.join(definitions))
    lines.append(');')
    return '\n'.join(lines)


def extract_blocks(schema: str, names: list[str]) -> list[str]:
    result = []
    for name in names:
        match = re.search(
            rf'CREATE TABLE IF NOT EXISTS {re.escape(name)}\s*\((.*?\n\);(?:\n(?:CREATE INDEX[^\n]*\n?)*)*)',
            schema,
            re.S,
        )
        if not match:
            raise RuntimeError(f'Projection/infra table missing from previous schema: {name}')
        result.append(match.group(0).rstrip())
    return result


def main() -> None:
    data = json.loads(DATA.read_text(encoding='utf-8'))
    all_relationships = json.loads(RELATIONS.read_text(encoding='utf-8'))['relationships']
    relationships = [item for item in all_relationships if item.get('confidence') == 'source']
    old_schema = OLD_SCHEMA.read_text(encoding='utf-8')
    MIGRATIONS.mkdir(parents=True, exist_ok=True)

    desktop_lines = [
        '-- Migration 0001: exact desktop Oraxhotel SQL Server mirror.',
        '-- Source: HotelSys/database/Hotel_alkheer_init.sql (47 tables, 378 columns).',
        '-- SQL Server types are translated to SQLite/D1 affinities without renaming columns.',
        '-- This mirror is the source-side preservation layer; mobile projections are in 0003.',
        '',
    ]
    for table in data['tables']:
        desktop_lines.append(render_desktop_table(table, relationships))
        desktop_lines.append('')
    migration1 = '\n'.join(desktop_lines).rstrip() + '\n'

    infra_blocks = extract_blocks(old_schema, INFRA)
    infra_lines = [
        '-- Migration 0002: Worker infrastructure and desktop-first command/event ledger.',
        '-- The desktop SQL Server remains the source of truth. Mobile writes become commands;',
        '-- canonical desktop publications are recorded as events and projected for Flutter.',
        '',
        *infra_blocks,
        '',
        'CREATE TABLE IF NOT EXISTS "desktop_sync_events" (',
        '  "event_id" TEXT PRIMARY KEY,',
        '  "source_system" TEXT NOT NULL,',
        '  "source_device" TEXT NOT NULL,',
        '  "entity_table" TEXT NOT NULL,',
        '  "entity_key" TEXT NOT NULL,',
        '  "operation" TEXT NOT NULL,',
        '  "occurred_at" TEXT NOT NULL,',
        '  "actor_id" TEXT,',
        '  "payload_json" TEXT NOT NULL,',
        '  "idempotency_key" TEXT NOT NULL UNIQUE,',
        '  "status" TEXT NOT NULL DEFAULT \'published\',',
        '  "created_at" INTEGER NOT NULL,',
        '  "applied_at" INTEGER',
        ');',
        'CREATE INDEX IF NOT EXISTS "idx_desktop_events_entity" ON "desktop_sync_events" ("entity_table", "entity_key");',
        'CREATE INDEX IF NOT EXISTS "idx_desktop_events_occurred" ON "desktop_sync_events" ("occurred_at");',
        '',
        'CREATE TABLE IF NOT EXISTS "desktop_sync_commands" (',
        '  "command_id" TEXT PRIMARY KEY,',
        '  "idempotency_key" TEXT NOT NULL UNIQUE,',
        '  "entity" TEXT NOT NULL,',
        '  "operation" TEXT NOT NULL,',
        '  "local_uuid" TEXT NOT NULL,',
        '  "payload_json" TEXT NOT NULL,',
        '  "vector_clock" TEXT NOT NULL DEFAULT \'{}\',',
        '  "requested_at" INTEGER NOT NULL,',
        '  "requested_by" TEXT,',
        '  "status" TEXT NOT NULL DEFAULT \'pending\',',
        '  "processed_at" INTEGER,',
        '  "result_json" TEXT,',
        '  "error" TEXT',
        ');',
        'CREATE INDEX IF NOT EXISTS "idx_desktop_commands_status" ON "desktop_sync_commands" ("status", "requested_at");',
        'CREATE INDEX IF NOT EXISTS "idx_desktop_commands_entity" ON "desktop_sync_commands" ("entity", "local_uuid");',
        '',
        'CREATE TABLE IF NOT EXISTS "desktop_sync_checkpoints" (',
        '  "source_system" TEXT NOT NULL,',
        '  "entity_table" TEXT NOT NULL,',
        '  "checkpoint_value" TEXT NOT NULL,',
        '  "updated_at" INTEGER NOT NULL,',
        '  PRIMARY KEY ("source_system", "entity_table")',
        ');',
    ]
    migration2 = '\n'.join(infra_lines).rstrip() + '\n'

    projection_blocks = extract_blocks(old_schema, PROJECTIONS)
    migration3 = '\n'.join([
        '-- Migration 0003: Flutter/Dart projection tables.',
        '-- These tables keep the mobile contract stable while desktop mirror tables',
        '-- preserve the original Oraxhotel SQL Server schema.',
        '',
        *projection_blocks,
        '',
    ])

    (MIGRATIONS / '0001_desktop_mirror.sql').write_text(migration1, encoding='utf-8')
    (MIGRATIONS / '0002_sync_infrastructure.sql').write_text(migration2, encoding='utf-8')
    (MIGRATIONS / '0003_flutter_projections.sql').write_text(migration3, encoding='utf-8')
    SCHEMA_OUT.write_text('\n'.join([migration1, migration2, migration3]), encoding='utf-8')
    print(json.dumps({
        'desktop_tables': len(data['tables']),
        'desktop_columns': sum(table['column_count'] for table in data['tables']),
        'desktop_relationships': len(relationships),
        'desktop_relationships_pending_review': len(all_relationships) - len(relationships),
        'projection_tables': len(PROJECTIONS),
        'infrastructure_tables': len(INFRA) + 3,
        'migration_files': [str(MIGRATIONS / name) for name in ['0001_desktop_mirror.sql', '0002_sync_infrastructure.sql', '0003_flutter_projections.sql']],
        'schema': str(SCHEMA_OUT),
    }, ensure_ascii=False))


if __name__ == '__main__':
    main()
