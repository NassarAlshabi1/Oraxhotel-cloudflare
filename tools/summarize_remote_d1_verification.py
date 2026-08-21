from __future__ import annotations

import json
from pathlib import Path

ROOT = Path('/home/ubuntu/oraxhotel2024')
verification = json.loads((ROOT / 'tools/remote_d1_column_verification.json').read_text(encoding='utf-8'))
source_tables = {
    table['table_name']
    for table in json.loads((ROOT / 'tools/orax_desktop_schema_extracted.json').read_text(encoding='utf-8'))['tables']
}
missing = {tuple(item) for item in verification['missing_columns']}
extra = {tuple(item) for item in verification['extra_columns']}
types = {tuple(item['table_column']) for item in verification['type_mismatches']}
null_pk = {tuple(item['table_column']) for item in verification['null_pk_mismatches']}
projection_tables = {item[0] for item in missing | extra | types | null_pk if item[0] not in source_tables}
source_missing = sorted(item for item in missing if item[0] in source_tables)
source_extra = sorted(item for item in extra if item[0] in source_tables)
source_types = sorted(item for item in types if item[0] in source_tables)
source_null_pk = sorted(item for item in null_pk if item[0] in source_tables)
result = {
    'remote_table_count': verification['remote_table_count'],
    'remote_column_count': verification['remote_column_count'],
    'desktop_source_table_count': len(source_tables),
    'desktop_source_missing_columns': source_missing,
    'desktop_source_extra_columns': source_extra,
    'desktop_source_type_mismatches': source_types,
    'desktop_source_null_pk_mismatches': source_null_pk,
    'desktop_source_exact': not source_missing and not source_extra and not source_types and not source_null_pk,
    'projection_tables_with_legacy_drift': sorted(projection_tables),
    'legacy_drift_missing_columns': sorted(missing - set(source_missing)),
    'legacy_drift_extra_columns': sorted(extra),
    'legacy_drift_type_mismatches': sorted(types),
    'legacy_drift_null_pk_mismatches': sorted(null_pk),
}
print(json.dumps(result, ensure_ascii=False, indent=2))
(ROOT / 'tools/remote_d1_source_summary.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
