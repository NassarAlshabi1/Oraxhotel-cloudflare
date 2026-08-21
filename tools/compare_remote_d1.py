from __future__ import annotations

import json
import sqlite3
from pathlib import Path

ROOT = Path('/home/ubuntu/oraxhotel2024')
REMOTE = Path('/tmp/orax_remote_d1_schema.json')
MIGRATIONS = ROOT / 'mobile/worker/migrations'
OUT = ROOT / 'tools/remote_d1_comparison.json'


def main() -> None:
    remote_data = json.loads(REMOTE.read_text(encoding='utf-8'))
    remote_rows = []
    for result in remote_data.get('result', []):
        remote_rows.extend(result.get('results', []))
    remote_tables = {row['name']: row.get('sql') for row in remote_rows if row.get('type') == 'table'}

    con = sqlite3.connect(':memory:')
    con.execute('PRAGMA foreign_keys=ON')
    for path in sorted(MIGRATIONS.glob('*.sql')):
        con.executescript(path.read_text(encoding='utf-8'))
    local_tables = {
        row[0]: con.execute(f'PRAGMA table_info("{row[0]}")').fetchall()
        for row in con.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")
    }
    missing = sorted(set(local_tables) - set(remote_tables))
    extra = sorted(set(remote_tables) - set(local_tables))
    common = sorted(set(local_tables) & set(remote_tables))
    field_diffs = {}
    for table in common:
        local_info = {row[1]: (row[2] or '').upper() for row in local_tables[table]}
        remote_sql = remote_tables[table] or ''
        remote_columns: dict[str, str] = {}
        body_match = remote_sql[remote_sql.find('(') + 1:remote_sql.rfind(')')] if '(' in remote_sql and ')' in remote_sql else ''
        for raw_line in body_match.splitlines():
            line = raw_line.strip().rstrip(',')
            match = __import__('re').match(r'^[`\"\[]?([^`\"\] ]+)[`\"\]]?\\s+([A-Za-z]+(?:\\s*\\([^)]*\\))?)', line)
            if match and match.group(1).upper() not in {'PRIMARY', 'FOREIGN', 'UNIQUE', 'CONSTRAINT', 'CHECK'}:
                remote_columns[match.group(1)] = match.group(2).upper().replace(' ', '')
        if remote_columns:
            missing = sorted(set(local_info) - set(remote_columns))
            extra = sorted(set(remote_columns) - set(local_info))
            type_mismatches = sorted(
                column for column in set(local_info) & set(remote_columns)
                if local_info[column] != remote_columns[column]
            )
            if missing or extra or type_mismatches:
                field_diffs[table] = {
                    'local_missing_remote': missing,
                    'remote_extra_local': extra,
                    'type_mismatch_columns': type_mismatches,
                }
    result = {
        'remote_table_count': len(remote_tables),
        'local_expected_table_count': len(local_tables),
        'missing_remote_tables': missing,
        'extra_remote_tables': extra,
        'common_table_count': len(common),
        'field_diffs_from_remote_sql_text': field_diffs,
        'remote_response': str(REMOTE),
    }
    OUT.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False))


if __name__ == '__main__':
    main()
