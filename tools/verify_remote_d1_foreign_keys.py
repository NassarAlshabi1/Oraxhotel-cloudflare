from __future__ import annotations

import json
import os
import subprocess
from pathlib import Path

ROOT = Path('/home/ubuntu/oraxhotel2024')
ACCOUNT_ID = '81a73bba9acc1de5693ff929d0a372ce'
DATABASE_ID = '607f1090-83b1-4281-975f-d81b8f6154e7'
RELATIONS = ROOT / 'tools/orax_desktop_relationships.json'
OUT = ROOT / 'tools/remote_d1_foreign_key_verification.json'


def query(sql: str) -> list[dict]:
    token = os.environ['CLOUDFLARE_API_TOKEN']
    body = json.dumps({'sql': sql})
    result = subprocess.run([
        'curl', '-fsS', '-X', 'POST',
        f'https://api.cloudflare.com/client/v4/accounts/{ACCOUNT_ID}/d1/database/{DATABASE_ID}/query',
        '-H', f'Authorization: Bearer {token}',
        '-H', 'Content-Type: application/json',
        '--data', body,
    ], check=True, capture_output=True, text=True)
    response = json.loads(result.stdout)
    if not response.get('success'):
        raise RuntimeError(json.dumps(response.get('errors'), ensure_ascii=False))
    rows = []
    for item in response.get('result', []):
        rows.extend(item.get('results', []))
    return rows


def main() -> None:
    all_relations = json.loads(RELATIONS.read_text(encoding='utf-8'))['relationships']
    expected = {
        (r['child_table'], r['child_column'], r['parent_table'], r['parent_column'])
        for r in all_relations if r.get('confidence') == 'source'
    }
    actual: set[tuple[str, str, str, str]] = set()
    for table in sorted({relation[0] for relation in expected}):
        safe = table.replace('"', '""')
        for row in query(f'PRAGMA foreign_key_list("{safe}")'):
            actual.add((table, row.get('from'), row.get('table'), row.get('to')))
    result = {
        'expected_confirmed_relationships': len(expected),
        'remote_relationships': len(actual),
        'missing_relationships': [list(item) for item in sorted(expected - actual)],
        'unexpected_relationships': [list(item) for item in sorted(actual - expected)],
        'exact_match': expected == actual,
    }
    OUT.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False))


if __name__ == '__main__':
    main()
