from __future__ import annotations

import json
import os
import sqlite3
import subprocess
from pathlib import Path

ROOT = Path('/home/ubuntu/oraxhotel2024')
ACCOUNT_ID = '81a73bba9acc1de5693ff929d0a372ce'
DATABASE_ID = '607f1090-83b1-4281-975f-d81b8f6154e7'
MIGRATIONS = ROOT / 'mobile/worker/migrations'
OUT = ROOT / 'tools/remote_d1_column_verification.json'


def remote_query(sql: str) -> list[dict]:
    token = os.environ.get('CLOUDFLARE_API_TOKEN')
    if not token:
        raise RuntimeError('CLOUDFLARE_API_TOKEN is required')
    body = json.dumps({'sql': sql})
    result = subprocess.run(
        [
            'curl', '-fsS', '-X', 'POST',
            f'https://api.cloudflare.com/client/v4/accounts/{ACCOUNT_ID}/d1/database/{DATABASE_ID}/query',
            '-H', f'Authorization: Bearer {token}',
            '-H', 'Content-Type: application/json',
            '--data', body,
        ],
        check=True,
        capture_output=True,
        text=True,
    )
    response = json.loads(result.stdout)
    if not response.get('success'):
        raise RuntimeError(json.dumps(response.get('errors'), ensure_ascii=False))
    rows: list[dict] = []
    for item in response.get('result', []):
        rows.extend(item.get('results', []))
    return rows


def main() -> None:
    con = sqlite3.connect(':memory:')
    con.execute('PRAGMA foreign_keys = ON')
    for path in sorted(MIGRATIONS.glob('*.sql')):
        con.executescript(path.read_text(encoding='utf-8'))
    local_rows = []
    for table_row in con.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'"):
        table = table_row[0]
        for row in con.execute(f'PRAGMA table_info("{table}")'):
            local_rows.append({
                'table_name': table,
                'column_name': row[1],
                'column_type': (row[2] or '').upper(),
                'notnull_flag': row[3],
                'pk_order': row[5],
            })
    table_names = [row[0] for row in con.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")]
    remote_rows = []
    for table in table_names:
        safe = table.replace('"', '""')
        for row in remote_query(f'PRAGMA table_info("{safe}")'):
            remote_rows.append({
                'table_name': table,
                'column_name': row.get('name'),
                'column_type': row.get('type') or '',
                'notnull_flag': row.get('notnull'),
                'pk_order': row.get('pk'),
            })
    local_map = {(r['table_name'], r['column_name']): r for r in local_rows}
    remote_map = {(r['table_name'], r['column_name']): r for r in remote_rows}
    missing_columns = sorted(set(local_map) - set(remote_map))
    extra_columns = sorted(set(remote_map) - set(local_map))
    type_mismatches = sorted(
        {
            key: {
                'local': local_map[key]['column_type'],
                'remote': (remote_map[key].get('column_type') or '').upper(),
            }
            for key in set(local_map) & set(remote_map)
            if local_map[key]['column_type'] != (remote_map[key].get('column_type') or '').upper()
        }.items()
    )
    null_pk_mismatches = sorted(
        {
            key: {
                'local': [local_map[key]['notnull_flag'], local_map[key]['pk_order']],
                'remote': [remote_map[key].get('notnull_flag'), remote_map[key].get('pk_order')],
            }
            for key in set(local_map) & set(remote_map)
            if [local_map[key]['notnull_flag'], local_map[key]['pk_order']]
            != [remote_map[key].get('notnull_flag'), remote_map[key].get('pk_order')]
        }.items()
    )
    local_tables = {r['table_name'] for r in local_rows}
    remote_tables = {r['table_name'] for r in remote_rows}
    result = {
        'local_table_count': len(local_tables),
        'remote_table_count': len(remote_tables),
        'local_column_count': len(local_rows),
        'remote_column_count': len(remote_rows),
        'missing_columns': [list(key) for key in missing_columns],
        'extra_columns': [list(key) for key in extra_columns],
        'type_mismatches': [{'table_column': list(key), **value} for key, value in type_mismatches],
        'null_pk_mismatches': [{'table_column': list(key), **value} for key, value in null_pk_mismatches],
        'exact_match': not missing_columns and not extra_columns and not type_mismatches and not null_pk_mismatches and local_tables.issubset(remote_tables),
    }
    OUT.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False))


if __name__ == '__main__':
    main()
