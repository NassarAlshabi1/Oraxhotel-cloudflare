from __future__ import annotations

import json
import sqlite3
from pathlib import Path

ROOT = Path('/home/ubuntu/oraxhotel2024/mobile')
SCHEMA = ROOT / 'worker/schema.sql'
EXTRACTED = Path('/home/ubuntu/oraxhotel2024/tools/orax_schema_extracted.json')

EXPECTED_INFRA = {
    'users', 'devices', 'rate_limits', 'sync_log', 'sync_conflicts', 'idempotency_log'
}
EXPECTED_D1 = {
    'rooms', 'bookings', 'booking_notes', 'employees', 'expenses',
    'cash_transactions', 'payments', 'debts', 'shift_notes', 'booking_nights',
    'hotel_day_ledger', 'price_adjustments', 'booking_price_adjustments',
    'audit_logs', 'payment_voids', 'guest_infos', 'salary_cycles',
    'salary_payments', 'salary_withdrawals', 'salary_carry_over_logs',
}


def main() -> None:
    data = json.loads(EXTRACTED.read_text(encoding='utf-8'))
    con = sqlite3.connect(':memory:')
    con.execute('PRAGMA foreign_keys = ON')
    con.executescript(SCHEMA.read_text(encoding='utf-8'))
    tables = {
        row[0] for row in con.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")
    }
    expected = EXPECTED_INFRA | EXPECTED_D1
    missing = sorted(expected - tables)
    unexpected = sorted(tables - expected)
    assert not missing, f'missing tables: {missing}'
    assert not unexpected, f'unexpected tables: {unexpected}'

    problems: list[str] = []
    for table in sorted(EXPECTED_D1):
        columns = {row[1]: row for row in con.execute(f'PRAGMA table_info({table})')}
        expected_dart = next(item for item in data['tables'] if item['table_name'] == table)
        for column in expected_dart['columns']:
            name = column['sql_name']
            if name not in columns:
                problems.append(f'{table}.{name}: missing')
                continue
            row = columns[name]
            actual_type = row[2].upper()
            if actual_type != column['sqlite_affinity']:
                problems.append(f'{table}.{name}: type {actual_type} != {column["sqlite_affinity"]}')
        if 'local_uuid' not in columns:
            problems.append(f'{table}.local_uuid: missing sync key')
        if 'updated_at' not in columns:
            problems.append(f'{table}.updated_at: missing sync cursor field')
    assert not problems, '\n'.join(problems)

    required_fks = {
        'bookings': ('rooms', 'room_number'),
        'booking_notes': ('bookings', 'booking_id'),
        'payments': ('bookings', 'booking_local_id'),
        'booking_price_adjustments': ('bookings', 'booking_local_uuid'),
        'salary_cycles': ('employees', 'employee_id'),
        'salary_payments': ('salary_cycles', 'cycle_id'),
        'salary_withdrawals': ('employees', 'employee_id'),
    }
    fk_rows = {}
    for table in EXPECTED_D1:
        fk_rows[table] = [tuple(row) for row in con.execute(f'PRAGMA foreign_key_list({table})')]
    for table, (parent, child) in required_fks.items():
        if not any(row[2] == parent and row[3] == child for row in fk_rows[table]):
            raise AssertionError(f'{table}: missing FK {child} -> {parent}')

    print(json.dumps({
        'valid_sqlite_schema': True,
        'table_count': len(tables),
        'infra_table_count': len(EXPECTED_INFRA),
        'd1_entity_table_count': len(EXPECTED_D1),
        'drift_schema_version': data['schema_version'],
        'checked_foreign_keys': len(required_fks),
        'message': 'DDL parses and expected tables/columns/types/selected FKs are present',
    }, ensure_ascii=False))


if __name__ == '__main__':
    main()
