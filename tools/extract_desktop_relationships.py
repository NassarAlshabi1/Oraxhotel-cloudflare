from __future__ import annotations

import json
import re
from pathlib import Path

SOURCE = Path('/home/ubuntu/oraxhotel2024/HotelSys/db/db.generated.cs')
OUTPUT = Path('/home/ubuntu/oraxhotel2024/tools/orax_desktop_relationships.json')


def main() -> None:
    text = SOURCE.read_text(encoding='utf-8')
    class_pattern = re.compile(
        r'\[Table\(Schema="(?P<schema>[^"]+)", Name="(?P<table>[^"]+)"\)\]\s*'
        r'public partial class (?P<class>\w+)\s*\{(?P<body>.*?)(?=\n\s*\[Table\(|\Z)',
        re.S,
    )
    class_matches = list(class_pattern.finditer(text))
    table_properties: dict[str, dict[str, str]] = {}
    for class_match in class_matches:
        current_table = class_match.group('table')
        body = class_match.group('body')
        properties: dict[str, str] = {}
        for column_match in re.finditer(
            r'\[Column\("(?P<column>[^"]+)"\)[^\]]*\]\s*'
            r'public\s+[^\s]+\??\s+(?P<property>\w+)',
            body,
        ):
            properties[column_match.group('property')] = column_match.group('column')
        # ASP.NET Identity fields have no explicit Column attribute and use the
        # property name as the database column name.
        for property_match in re.finditer(
            r'public\s+[^\s]+\??\s+(?P<property>\w+)\s*\{\s*get;\s*set;\s*\}',
            body,
        ):
            properties.setdefault(property_match.group('property'), property_match.group('property'))
        table_properties[current_table] = properties

    relations = []
    for class_match in class_matches:
        current_table = class_match.group('table')
        body = class_match.group('body')
        association_pattern = re.compile(
            r'/// <summary>.*?\((?:dbo\.)?(?P<target>[^)]+)\).*?'
            r'\[Association\(ThisKey="(?P<this_key>[^"]+)", OtherKey="(?P<other_key>[^"]+)", CanBeNull=(?P<nullable>true|false)\)\]\s*'
            r'public\s+(?P<property_type>[^;]+);',
            re.S | re.I,
        )
        for match in association_pattern.finditer(body):
            target = match.group('target')
            this_key = match.group('this_key')
            other_key = match.group('other_key')
            property_type = match.group('property_type').strip()
            is_back_reference = property_type.startswith('IEnumerable<')
            if is_back_reference:
                child_table = target
                child_column = table_properties.get(child_table, {}).get(other_key, other_key)
                parent_table = current_table
                parent_column = table_properties.get(parent_table, {}).get(this_key, this_key)
            else:
                child_table = current_table
                child_column = table_properties.get(child_table, {}).get(this_key, this_key)
                parent_table = target
                parent_column = table_properties.get(parent_table, {}).get(other_key, other_key)
            confidence = 'review' if (
                child_table == 'jop_emp_table'
                and child_column == 'id_job_name'
                and parent_table == 'emp_table'
            ) else 'source'
            relations.append({
                'child_table': child_table,
                'child_column': child_column,
                'parent_table': parent_table,
                'parent_column': parent_column,
                'confidence': confidence,
                'nullable': match.group('nullable').lower() == 'true',
                'property_type': property_type,
                'source_class': class_match.group('class'),
            })
    unique = {}
    for relation in relations:
        key = (relation['child_table'], relation['child_column'], relation['parent_table'], relation['parent_column'])
        unique[key] = relation
    result = {
        'source': str(SOURCE),
        'relationship_count': len(unique),
        'source_relationship_count': sum(item['confidence'] == 'source' for item in unique.values()),
        'review_relationship_count': sum(item['confidence'] == 'review' for item in unique.values()),
        'relationships': sorted(unique.values(), key=lambda item: (item['child_table'], item['child_column'], item['parent_table'])),
    }
    OUTPUT.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({
        'relationship_count': len(unique),
        'source_relationship_count': result['source_relationship_count'],
        'review_relationship_count': result['review_relationship_count'],
        'output': str(OUTPUT),
    }, ensure_ascii=False))
    for relation in result['relationships']:
        print(f"{relation['child_table']}.{relation['child_column']} -> {relation['parent_table']}.{relation['parent_column']}")


if __name__ == '__main__':
    main()
