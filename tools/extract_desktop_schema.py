from __future__ import annotations

import json
import re
from pathlib import Path

SOURCE = Path('/home/ubuntu/oraxhotel2024/HotelSys/database/Hotel_alkheer_init.sql')
OUTPUT = Path('/home/ubuntu/oraxhotel2024/tools/orax_desktop_schema_extracted.json')


def parse_column(line: str) -> dict | None:
    match = re.match(r'^\s*\[([^]]+)\]\s+(.+?)(?:,)?\s*$', line)
    if not match:
        return None
    name, definition = match.groups()
    definition = definition.strip().rstrip(',')
    identity = bool(re.search(r'\bIDENTITY\s*\(', definition, re.I))
    nullable = not bool(re.search(r'\bNOT\s+NULL\b', definition, re.I))
    default_match = re.search(r'\bDEFAULT\s+(.+?)(?=\s+(?:CONSTRAINT|REFERENCES)\b|$)', definition, re.I)
    default = default_match.group(1).strip() if default_match else None
    type_match = re.match(r'([A-Za-z]+(?:\s*\([^)]*\))?)', definition)
    sql_type = type_match.group(1).upper().replace(' ', '') if type_match else definition
    base_type = re.match(r'([A-Za-z]+)', sql_type)
    return {
        'name': name,
        'sql_server_type': sql_type,
        'base_type': base_type.group(1).upper() if base_type else sql_type,
        'nullable': nullable,
        'identity': identity,
        'default': default,
        'definition': definition,
    }


def main() -> None:
    text = SOURCE.read_text(encoding='utf-8')
    table_pattern = re.compile(
        r'CREATE TABLE\s+\[dbo\]\.\[([^]]+)\]\s*\((.*?)\n\s*\);',
        re.I | re.S,
    )
    tables = []
    for match in table_pattern.finditer(text):
        table_name, body = match.groups()
        columns = []
        primary_key = []
        for raw_line in body.splitlines():
            column = parse_column(raw_line)
            if column:
                columns.append(column)
            pk_match = re.search(r'PRIMARY KEY\s*\(([^)]+)\)', raw_line, re.I)
            if pk_match:
                primary_key = re.findall(r'\[([^]]+)\]', pk_match.group(1))
        tables.append({
            'table_name': table_name,
            'schema': 'dbo',
            'source_line': text.count('\n', 0, match.start()) + 1,
            'columns': columns,
            'primary_key': primary_key,
            'column_count': len(columns),
        })
    result = {
        'source': str(SOURCE),
        'database_name': 'Hotel_alkheer',
        'dialect': 'Microsoft SQL Server',
        'table_count': len(tables),
        'tables': tables,
    }
    OUTPUT.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({
        'database': result['database_name'],
        'dialect': result['dialect'],
        'table_count': result['table_count'],
        'column_count': sum(table['column_count'] for table in tables),
        'output': str(OUTPUT),
    }, ensure_ascii=False))


if __name__ == '__main__':
    main()
