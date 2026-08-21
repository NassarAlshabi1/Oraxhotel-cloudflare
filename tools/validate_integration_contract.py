from __future__ import annotations

import json
import re
from pathlib import Path

BASE = Path('/home/ubuntu/oraxhotel2024')
MOBILE = BASE / 'mobile'
CONFIG = MOBILE / 'mobile/lib/services/cloudflare_config.dart'
MANAGER = MOBILE / 'mobile/lib/services/cloudflare_sync_manager.dart'
DATABASE = MOBILE / 'worker/src/database.ts'
INDEX = MOBILE / 'worker/src/index.ts'
SCHEMA = MOBILE / 'worker/schema.sql'


def extract_map_keys(text: str, block_name: str) -> set[str]:
    match = re.search(rf'{block_name}[^=]*=\s*\{{(.*?)\n\s*\}};', text, re.S)
    if not match:
        raise AssertionError(f'missing map {block_name}')
    return set(re.findall(r"['\"]?([a-z][a-z0-9_]*)['\"]?\s*:", match.group(1)))


def extract_list(text: str, list_name: str) -> list[str]:
    match = re.search(rf'{list_name}\s*=\s*\[(.*?)\n\s*\];', text, re.S)
    if not match:
        raise AssertionError(f'missing list {list_name}')
    return re.findall(r"['\"]([a-z][a-z0-9_]*)['\"]", match.group(1))


def schema_tables(schema: str) -> set[str]:
    return set(re.findall(r'CREATE TABLE IF NOT EXISTS (\w+)\s*\(', schema))


def table_columns(schema: str, table: str) -> set[str]:
    match = re.search(rf'CREATE TABLE IF NOT EXISTS {table}\s*\((.*?)\n\);', schema, re.S)
    if not match:
        raise AssertionError(f'missing table body {table}')
    result: set[str] = set()
    for line in match.group(1).splitlines():
        line = line.strip().rstrip(',')
        column = re.match(r'([a-z][a-z0-9_]*)\s+(?:TEXT|INTEGER|REAL)', line, re.I)
        if column:
            result.add(column.group(1))
    return result


def main() -> None:
    config = CONFIG.read_text(encoding='utf-8')
    database = DATABASE.read_text(encoding='utf-8')
    index = INDEX.read_text(encoding='utf-8')
    manager = MANAGER.read_text(encoding='utf-8')
    schema = SCHEMA.read_text(encoding='utf-8')

    config_entities = extract_map_keys(config, 'entityToTable')
    migration_order = extract_list(config, 'migrationOrder')
    worker_entities = extract_map_keys(database, 'ENTITY_TABLES')
    d1_tables = schema_tables(schema)
    infra = {'users', 'devices', 'rate_limits', 'sync_log', 'sync_conflicts', 'idempotency_log'}

    problems: list[str] = []
    if set(migration_order) != config_entities:
        problems.append(f'config migrationOrder mismatch: {sorted(config_entities ^ set(migration_order))}')
    if not config_entities <= worker_entities:
        problems.append(f'Flutter entities missing in Worker: {sorted(config_entities - worker_entities)}')
    if not worker_entities <= d1_tables:
        problems.append(f'Worker entities missing in D1: {sorted(worker_entities - d1_tables)}')
    if not (infra <= d1_tables):
        problems.append(f'infrastructure tables missing in D1: {sorted(infra - d1_tables)}')
    if len(worker_entities) != 20:
        problems.append(f'expected 20 Worker entities, got {len(worker_entities)}')

    sync_fields = {'local_uuid', 'created_at', 'updated_at', 'deleted_at', 'last_modified', 'version', 'vector_clock', 'device_id', 'idempotency_key'}
    missing_sync_fields = {}
    for table in sorted(worker_entities):
        missing = sorted(sync_fields - table_columns(schema, table))
        if missing:
            missing_sync_fields[table] = missing
    if missing_sync_fields:
        problems.append(f'missing sync fields: {missing_sync_fields}')

    required_routes = [
        "path === '/api/auth/login'", "path === '/api/auth/register'",
        "path === '/api/sync/pull'", "path === '/api/sync/push'",
        "path === '/api/sync/migrate'", "path === '/api/devices/register'",
    ]
    for route in required_routes:
        if route not in index:
            problems.append(f'missing Worker route marker: {route}')
    for endpoint in ['/api/auth/login', '/api/devices/register', '/api/sync/push', '/api/sync/pull']:
        if endpoint not in manager:
            problems.append(f'missing Flutter endpoint: {endpoint}')
    if 'const validColumns = await this.getTableColumns(table);' not in database:
        problems.append('Worker update path does not whitelist columns')

    if problems:
        raise AssertionError('\n'.join(problems))
    print(json.dumps({
        'contract_valid': True,
        'flutter_entity_count': len(config_entities),
        'worker_entity_count': len(worker_entities),
        'd1_table_count': len(d1_tables),
        'infra_table_count': len(infra),
        'migration_order_matches_entity_map': True,
        'all_sync_fields_present': True,
        'required_routes_present': True,
        'update_column_whitelist_present': True,
        'message': 'Flutter, Worker and D1 table contracts are structurally aligned',
    }, ensure_ascii=False))


if __name__ == '__main__':
    main()
