from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path('/home/ubuntu/oraxhotel2024/mobile')
DART = ROOT / 'mobile/lib/services/local_db.dart'
OUT = Path('/home/ubuntu/oraxhotel2024/tools/orax_schema_extracted.json')


def snake_case(value: str) -> str:
    # Drift's default SQL naming splits every uppercase character.
    # Confirmed in local_db.g.dart: employeeID -> employee_i_d.
    return re.sub(r'(?<!^)(?=[A-Z])', '_', value).lower()


def constant_value(expr: str) -> str | None:
    match = re.search(r'withDefault\(\s*const\s+Constant\((.*?)\)\s*\)', expr, re.S)
    return match.group(1).strip() if match else None


def parse_column(expr: str, dart_name: str) -> dict:
    type_match = re.search(r'\b(TextColumn|IntColumn|RealColumn|BoolColumn)\b', expr)
    dart_type = type_match.group(1) if type_match else 'UnknownColumn'
    sqlite_type = {
        'TextColumn': 'TEXT',
        'IntColumn': 'INTEGER',
        'RealColumn': 'REAL',
        'BoolColumn': 'INTEGER',
    }.get(dart_type, 'UNKNOWN')
    sql_name_match = re.search(r"\.named\(\s*['\"]([^'\"]+)['\"]\s*\)", expr)
    sql_name = sql_name_match.group(1) if sql_name_match else snake_case(dart_name)
    nullable = bool(re.search(r'\.nullable\(\)', expr))
    auto_increment = bool(re.search(r'\.autoIncrement\(\)', expr))
    unique = bool(re.search(r'\.unique\(\)', expr))
    default = constant_value(expr)
    reference = None
    ref_match = re.search(r'\.references\(\s*(\w+)\s*,\s*#(\w+)\s*\)', expr)
    if ref_match:
        reference = {
            'table_class': ref_match.group(1),
            'field': ref_match.group(2),
        }
    return {
        'dart_name': dart_name,
        'sql_name': sql_name,
        'dart_column_type': dart_type,
        'sqlite_affinity': sqlite_type,
        'nullable': nullable,
        'auto_increment': auto_increment,
        'unique': unique,
        'default_expression': default,
        'reference': reference,
        'declaration': ' '.join(expr.split()),
    }


def extract_table_blocks(text: str):
    pattern = re.compile(r'^class\s+(\w+)\s+extends\s+Table(?P<mixin>\s+with\s+SyncFields)?\s*\{', re.M)
    matches = list(pattern.finditer(text))
    for index, match in enumerate(matches):
        end = matches[index + 1].start() if index + 1 < len(matches) else text.find('@DriftDatabase', match.end())
        if end < 0:
            end = len(text)
        yield match.group(1), bool(match.group('mixin')), text[match.end():end], text.count('\n', 0, match.start()) + 1


def extract_columns(block: str) -> list[dict]:
    # Declarations are simple getters, but expressions may span multiple lines.
    pattern = re.compile(r'\b(?:TextColumn|IntColumn|RealColumn|BoolColumn)\s+get\s+(\w+)\s*=>\s*(.*?);', re.S)
    columns = []
    for match in pattern.finditer(block):
        # Pass the complete declaration so parse_column can see TextColumn/IntColumn/etc.
        columns.append(parse_column(match.group(0), match.group(1)))
    return columns


def extract_unique_keys(block: str) -> list[list[str]]:
    keys = []
    for match in re.finditer(r'\{([^{}]+)\}', block):
        inside = match.group(1)
        if 'uniqueKeys' not in block[max(0, match.start() - 180):match.start()]:
            continue
        names = [part.strip() for part in inside.split(',') if part.strip()]
        if names:
            keys.append(names)
    return keys


def main() -> None:
    text = DART.read_text(encoding='utf-8')
    tables = []
    for class_name, sync_fields, block, line in extract_table_blocks(text):
        table = {
            'class_name': class_name,
            'table_name': snake_case(class_name),
            'source_line': line,
            'has_sync_fields': sync_fields,
            'columns': extract_columns(block),
            'unique_keys_dart_names': extract_unique_keys(block),
        }
        tables.append(table)

    shared = [
        {'dart_name': 'localUuid', 'sql_name': 'local_uuid', 'dart_column_type': 'TextColumn', 'sqlite_affinity': 'TEXT', 'nullable': False, 'auto_increment': False, 'unique': True, 'default_expression': None, 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'serverId', 'sql_name': 'server_id', 'dart_column_type': 'IntColumn', 'sqlite_affinity': 'INTEGER', 'nullable': True, 'auto_increment': False, 'unique': False, 'default_expression': None, 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'createdAt', 'sql_name': 'created_at', 'dart_column_type': 'IntColumn', 'sqlite_affinity': 'INTEGER', 'nullable': False, 'auto_increment': False, 'unique': False, 'default_expression': None, 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'updatedAt', 'sql_name': 'updated_at', 'dart_column_type': 'IntColumn', 'sqlite_affinity': 'INTEGER', 'nullable': False, 'auto_increment': False, 'unique': False, 'default_expression': None, 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'deletedAt', 'sql_name': 'deleted_at', 'dart_column_type': 'IntColumn', 'sqlite_affinity': 'INTEGER', 'nullable': True, 'auto_increment': False, 'unique': False, 'default_expression': None, 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'lastModified', 'sql_name': 'last_modified', 'dart_column_type': 'IntColumn', 'sqlite_affinity': 'INTEGER', 'nullable': False, 'auto_increment': False, 'unique': False, 'default_expression': None, 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'createdAtIso', 'sql_name': 'created_at_iso', 'dart_column_type': 'TextColumn', 'sqlite_affinity': 'TEXT', 'nullable': True, 'auto_increment': False, 'unique': False, 'default_expression': None, 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'updatedAtIso', 'sql_name': 'updated_at_iso', 'dart_column_type': 'TextColumn', 'sqlite_affinity': 'TEXT', 'nullable': True, 'auto_increment': False, 'unique': False, 'default_expression': None, 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'deletedAtIso', 'sql_name': 'deleted_at_iso', 'dart_column_type': 'TextColumn', 'sqlite_affinity': 'TEXT', 'nullable': True, 'auto_increment': False, 'unique': False, 'default_expression': None, 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'createdAtEpoch', 'sql_name': 'created_at_epoch', 'dart_column_type': 'IntColumn', 'sqlite_affinity': 'INTEGER', 'nullable': False, 'auto_increment': False, 'unique': False, 'default_expression': '0', 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'lastModifiedEpoch', 'sql_name': 'last_modified_epoch', 'dart_column_type': 'IntColumn', 'sqlite_affinity': 'INTEGER', 'nullable': False, 'auto_increment': False, 'unique': False, 'default_expression': '0', 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'version', 'sql_name': 'version', 'dart_column_type': 'IntColumn', 'sqlite_affinity': 'INTEGER', 'nullable': False, 'auto_increment': False, 'unique': False, 'default_expression': '1', 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'origin', 'sql_name': 'origin', 'dart_column_type': 'TextColumn', 'sqlite_affinity': 'TEXT', 'nullable': False, 'auto_increment': False, 'unique': False, 'default_expression': "'local'", 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'vectorClock', 'sql_name': 'vector_clock', 'dart_column_type': 'TextColumn', 'sqlite_affinity': 'TEXT', 'nullable': False, 'auto_increment': False, 'unique': False, 'default_expression': "'{}'", 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'deviceId', 'sql_name': 'device_id', 'dart_column_type': 'TextColumn', 'sqlite_affinity': 'TEXT', 'nullable': False, 'auto_increment': False, 'unique': False, 'default_expression': "''", 'reference': None, 'declaration': 'SyncFields'},
        {'dart_name': 'idempotencyKey', 'sql_name': 'idempotency_key', 'dart_column_type': 'TextColumn', 'sqlite_affinity': 'TEXT', 'nullable': True, 'auto_increment': False, 'unique': False, 'default_expression': None, 'reference': None, 'declaration': 'SyncFields'},
    ]
    for table in tables:
        if table['has_sync_fields']:
            existing_shared = {column['sql_name'] for column in shared}
            table['columns'] = shared + [column for column in table['columns'] if column['sql_name'] not in existing_shared]

    registry_match = re.search(r'@DriftDatabase\(\s*tables:\s*\[(.*?)\]\s*,', text, re.S)
    registry = []
    if registry_match:
        registry = re.findall(r'\b([A-Z]\w+),', registry_match.group(1))
    for table in tables:
        table['registered_in_drift'] = table['class_name'] in registry

    result = {
        'source': str(DART),
        'schema_version': int(re.search(r'schemaVersion\s*=>\s*(\d+)', text).group(1)),
        'shared_sync_fields': shared,
        'registered_tables': registry,
        'tables': tables,
    }
    OUT.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({
        'schema_version': result['schema_version'],
        'table_count': len(tables),
        'registered_count': len(registry),
        'sync_table_count': sum(1 for table in tables if table['has_sync_fields']),
        'output': str(OUT),
    }, ensure_ascii=False))


if __name__ == '__main__':
    main()
