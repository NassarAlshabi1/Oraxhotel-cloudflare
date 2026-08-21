from __future__ import annotations

import json
import re
import sqlite3
from pathlib import Path

BASE = Path('/home/ubuntu/oraxhotel2024')
MIGRATIONS = BASE / 'mobile/worker/migrations'
EXTRACTED = BASE / 'tools/orax_desktop_schema_extracted.json'
RELATIONS = BASE / 'tools/orax_desktop_relationships.json'


def main() -> None:
    files = sorted(MIGRATIONS.glob('*.sql'))
    if [path.name for path in files] != [
        '0001_desktop_mirror.sql',
        '0002_sync_infrastructure.sql',
        '0003_flutter_projections.sql',
    ]:
        raise AssertionError(f'unexpected migration order: {[path.name for path in files]}')
    con = sqlite3.connect(':memory:')
    con.execute('PRAGMA foreign_keys = ON')
    for path in files:
        con.executescript(path.read_text(encoding='utf-8'))
    actual = {
        row[0] for row in con.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")
    }
    extracted = json.loads(EXTRACTED.read_text(encoding='utf-8'))
    all_relationships = json.loads(RELATIONS.read_text(encoding='utf-8'))['relationships']
    relationships = [item for item in all_relationships if item.get('confidence') == 'source']
    desktop = {table['table_name'] for table in extracted['tables']}
    expected_infra = {
        'users', 'devices', 'rate_limits', 'sync_log', 'sync_conflicts', 'idempotency_log',
        'desktop_sync_events', 'desktop_sync_commands', 'desktop_sync_checkpoints',
    }
    projections = {
        'rooms', 'bookings', 'booking_notes', 'employees', 'expenses',
        'cash_transactions', 'payments', 'debts', 'shift_notes', 'booking_nights',
        'hotel_day_ledger', 'price_adjustments', 'booking_price_adjustments',
        'audit_logs', 'payment_voids', 'guest_infos', 'salary_cycles',
        'salary_payments', 'salary_withdrawals', 'salary_carry_over_logs',
    }
    expected = desktop | expected_infra | projections
    assert len(desktop) == 47
    assert not expected - actual, f'missing tables: {sorted(expected - actual)}'
    assert not actual - expected, f'unexpected tables: {sorted(actual - expected)}'
    assert len(actual) == 76, len(actual)
    for table in sorted(desktop):
        cols = {row[1] for row in con.execute(f'PRAGMA table_info(\"{table}\")')}
        if not cols:
            raise AssertionError(f'empty/missing desktop table {table}')
    for relation in relationships:
        child_table = relation['child_table'].replace('"', '""')
        rows = list(con.execute(f'PRAGMA foreign_key_list("{child_table}")'))
        if not any(row[2] == relation['parent_table'] and row[3] == relation['child_column'] and row[4] == relation['parent_column'] for row in rows):
            raise AssertionError(f"missing desktop FK {relation['child_table']}.{relation['child_column']} -> {relation['parent_table']}.{relation['parent_column']}")
    for table in sorted(projections):
        cols = {row[1] for row in con.execute(f'PRAGMA table_info("{table}")')}
        missing = {'local_uuid', 'server_id', 'updated_at', 'version', 'vector_clock'} - cols
        if missing:
            raise AssertionError(f'{table}: missing sync columns {sorted(missing)}')
    for table, parent, child in [
        ('rooms', 'bookings', 'room_number'),
        ('bookings', 'booking_notes', 'booking_id'),
        ('bookings', 'payments', 'booking_local_id'),
        ('employees', 'salary_cycles', 'employee_id'),
        ('salary_cycles', 'salary_payments', 'cycle_id'),
    ]:
        rows = list(con.execute(f'PRAGMA foreign_key_list("{parent}")'))
        if not any(row[2] == table and row[3] == child for row in rows):
            raise AssertionError(f'{parent}: missing FK {child} -> {table}')
    print(json.dumps({
        'valid_sqlite_schema': True,
        'migration_count': len(files),
        'desktop_table_count': len(desktop),
        'desktop_column_count': sum(item['column_count'] for item in extracted['tables']),
        'desktop_relationship_count': len(relationships),
        'desktop_relationships_pending_review': len(all_relationships) - len(relationships),
        'infra_table_count': len(expected_infra),
        'projection_table_count': len(projections),
        'total_table_count': len(actual),
        'message': 'desktop-first D1 migrations parse and expected tables/columns/selected FKs exist',
    }, ensure_ascii=False))


if __name__ == '__main__':
    main()
