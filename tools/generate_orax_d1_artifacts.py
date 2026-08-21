from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path('/home/ubuntu/orAX_PLACEHOLDER')
ROOT = Path('/home/ubuntu/oraxhotel2024/mobile')
TOOLS = Path('/home/ubuntu/oraxhotel2024/tools')
SOURCE = ROOT / 'mobile/lib/services/local_db.dart'
EXTRACTED = TOOLS / 'orax_schema_extracted.json'
SCHEMA_OUT = ROOT / 'worker/schema.sql'
REPORT_OUT = Path('/home/ubuntu/oraxhotel2024/docs/oraxhotel-schema-integration-audit.md')

D1_TABLES = [
    'rooms', 'bookings', 'booking_notes', 'employees', 'expenses',
    'cash_transactions', 'payments', 'debts', 'shift_notes', 'booking_nights',
    'hotel_day_ledger', 'price_adjustments', 'booking_price_adjustments',
    'audit_logs', 'payment_voids', 'guest_infos', 'salary_cycles',
    'salary_payments', 'salary_withdrawals', 'salary_carry_over_logs',
]

DART_TO_SQL = {
    'Rooms': 'rooms', 'Bookings': 'bookings', 'BookingNotes': 'booking_notes',
    'Employees': 'employees', 'Expenses': 'expenses',
    'CashTransactions': 'cash_transactions', 'Payments': 'payments',
    'Debts': 'debts', 'ShiftNotes': 'shift_notes', 'BookingNights': 'booking_nights',
    'HotelDayLedger': 'hotel_day_ledger', 'PriceAdjustments': 'price_adjustments',
    'BookingPriceAdjustments': 'booking_price_adjustments', 'AuditLogs': 'audit_logs',
    'PaymentVoids': 'payment_voids', 'GuestInfos': 'guest_infos',
    'SalaryCycles': 'salary_cycles', 'SalaryPayments': 'salary_payments',
    'SalaryWithdrawals': 'salary_withdrawals',
    'SalaryCarryOverLogs': 'salary_carry_over_logs',
}

# Explicitly confirmed from local_db.dart uniqueKeys declarations.
COMPOSITE_UNIQUES = {
    'booking_nights': [('booking_local_id', 'hotel_day_key')],
    'hotel_day_ledger': [('hotel_day_key',)],
    'salary_cycles': [('employee_id', 'cycle_key')],
}

# Foreign-key targets are derived from the .references(...) declarations.
FOREIGN_KEY_TARGETS = {
    ('bookings', 'room_number'): ('rooms', 'room_number'),
    ('booking_notes', 'booking_id'): ('bookings', 'id'),
    ('payments', 'booking_local_id'): ('bookings', 'id'),
    ('payments', 'cash_transaction_local_id'): ('cash_transactions', 'id'),
    ('debts', 'booking_local_id'): ('bookings', 'id'),
    ('booking_nights', 'booking_local_id'): ('bookings', 'id'),
    ('booking_price_adjustments', 'booking_local_uuid'): ('bookings', 'local_uuid'),
    ('booking_price_adjustments', 'booking_local_id'): ('bookings', 'id'),
    ('salary_cycles', 'employee_id'): ('employees', 'id'),
    ('salary_payments', 'cycle_id'): ('salary_cycles', 'id'),
    ('salary_withdrawals', 'employee_id'): ('employees', 'id'),
    ('salary_carry_over_logs', 'employee_id'): ('employees', 'id'),
}


def sql_default(value: str | None) -> str | None:
    if value is None:
        return None
    value = value.strip()
    if value == 'false':
        return '0'
    if value == 'true':
        return '1'
    return value


def column_ddl(column: dict, table_name: str) -> str:
    name = column['sql_name']
    typ = column['sqlite_affinity']
    if column['auto_increment']:
        return f'  {name} INTEGER PRIMARY KEY AUTOINCREMENT'
    parts = [f'  {name} {typ}']
    if not column['nullable']:
        parts.append('NOT NULL')
    if column['unique']:
        parts.append('UNIQUE')
    default = sql_default(column.get('default_expression'))
    if default is not None:
        parts.append(f'DEFAULT {default}')
    target = FOREIGN_KEY_TARGETS.get((table_name, name))
    if target:
        parts.append(f'REFERENCES {target[0]}({target[1]})')
    return ' '.join(parts)


def parse_indexes(source: str) -> dict[str, list[str]]:
    found: dict[str, list[str]] = {}
    pattern = re.compile(
        r"Index\(\s*'([^']+)'\s*,\s*'CREATE INDEX [^']+ ON (\w+)\s*\(([^)]+)\)'\s*\)",
        re.S,
    )
    for match in pattern.finditer(source):
        index_name, table, columns = match.groups()
        if table in D1_TABLES:
            found.setdefault(table, []).append(
                f"CREATE INDEX IF NOT EXISTS {index_name} ON {table} ({columns.strip()});"
            )
    return found


def render_schema(data: dict, source: str) -> str:
    by_table = {table['table_name']: table for table in data['tables']}
    indexes = parse_indexes(source)
    out: list[str] = []
    out += [
        '-- Oraxhotel Cloudflare D1 schema',
        '-- Generated from mobile/mobile/lib/services/local_db.dart (Drift schemaVersion 51).',
        '-- D1/Worker infrastructure tables are defined explicitly below.',
        '-- Do not treat hotel_day_ledger as a Flutter Cloudflare sync entity; it is local-only in cloudflare_config.dart.',
        '',
        'PRAGMA foreign_keys = ON;',
        '',
        '-- Worker infrastructure -------------------------------------------------',
        'CREATE TABLE IF NOT EXISTS users (',
        '  id TEXT PRIMARY KEY,',
        '  username TEXT NOT NULL UNIQUE,',
        '  password_hash TEXT NOT NULL,',
        "  role TEXT NOT NULL DEFAULT 'staff',",
        '  device_id TEXT,',
        '  created_at INTEGER NOT NULL,',
        '  updated_at INTEGER NOT NULL,',
        '  deleted_at INTEGER',
        ');',
        'CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);',
        '',
        'CREATE TABLE IF NOT EXISTS devices (',
        '  id TEXT PRIMARY KEY,',
        '  device_id TEXT NOT NULL UNIQUE,',
        '  fcm_token TEXT,',
        "  status TEXT NOT NULL DEFAULT 'active',",
        '  device_name TEXT,',
        '  platform TEXT,',
        '  created_at INTEGER NOT NULL,',
        '  updated_at INTEGER NOT NULL',
        ');',
        'CREATE INDEX IF NOT EXISTS idx_devices_status ON devices(status);',
        '',
        'CREATE TABLE IF NOT EXISTS rate_limits (',
        '  client_id TEXT NOT NULL,',
        '  window_start INTEGER NOT NULL,',
        '  count INTEGER NOT NULL DEFAULT 0,',
        '  PRIMARY KEY (client_id, window_start)',
        ');',
        '',
        'CREATE TABLE IF NOT EXISTS sync_log (',
        '  id INTEGER PRIMARY KEY AUTOINCREMENT,',
        '  entity TEXT NOT NULL,',
        '  entity_id TEXT NOT NULL,',
        '  operation TEXT NOT NULL,',
        '  version INTEGER NOT NULL,',
        '  device_id TEXT,',
        '  timestamp INTEGER NOT NULL,',
        '  payload TEXT',
        ');',
        'CREATE INDEX IF NOT EXISTS idx_sync_log_entity ON sync_log(entity, entity_id);',
        'CREATE INDEX IF NOT EXISTS idx_sync_log_timestamp ON sync_log(timestamp);',
        '',
        'CREATE TABLE IF NOT EXISTS sync_conflicts (',
        '  id INTEGER PRIMARY KEY AUTOINCREMENT,',
        '  entity TEXT NOT NULL,',
        '  entity_id TEXT NOT NULL,',
        '  local_payload TEXT NOT NULL,',
        '  remote_payload TEXT NOT NULL,',
        '  local_vector_clock TEXT,',
        '  remote_vector_clock TEXT,',
        "  resolution TEXT NOT NULL DEFAULT 'last_write_wins',",
        '  resolved_at INTEGER,',
        '  created_at INTEGER NOT NULL,',
        '  device_id TEXT',
        ');',
        'CREATE INDEX IF NOT EXISTS idx_conflicts_entity ON sync_conflicts(entity, entity_id);',
        'CREATE INDEX IF NOT EXISTS idx_conflicts_created ON sync_conflicts(created_at);',
        '',
        'CREATE TABLE IF NOT EXISTS idempotency_log (',
        '  key TEXT PRIMARY KEY,',
        '  entity TEXT NOT NULL,',
        '  operation TEXT NOT NULL,',
        '  entity_id TEXT,',
        '  processed_at INTEGER NOT NULL,',
        '  response TEXT',
        ');',
        'CREATE INDEX IF NOT EXISTS idx_idempotency_entity ON idempotency_log(entity, entity_id);',
        '',
        '-- Flutter sync entities --------------------------------------------------',
    ]
    for table_name in D1_TABLES:
        table = by_table[table_name]
        columns = [column_ddl(column, table_name) for column in table['columns']]
        constraints: list[str] = []
        for unique_group in COMPOSITE_UNIQUES.get(table_name, []):
            constraints.append(f"  UNIQUE ({', '.join(unique_group)})")
        all_lines = columns + constraints
        out.append(f'CREATE TABLE IF NOT EXISTS {table_name} (')
        out.append(',\n'.join(all_lines))
        out.append(');')
        out.extend(indexes.get(table_name, []))
        out.append('')
    return '\n'.join(out).rstrip() + '\n'


def render_report(data: dict, source: str) -> str:
    by_table = {table['table_name']: table for table in data['tables']}
    sync_tables = [table for table in data['tables'] if table['table_name'] in D1_TABLES]
    local_only = [table for table in data['tables'] if table['table_name'] not in D1_TABLES]
    lines: list[str] = []
    lines += [
        '# تدقيق مخطط Oraxhotel وتكامل Flutter/Dart مع Cloudflare D1',
        '',
        '> هذا التقرير مشتق آلياً من تعريفات Drift وملفات Worker الموجودة في المستودع المحلي، وليس من تخمين أو من مخطط خارجي.',
        '',
        '## النطاق والمصادر',
        '',
        '| المصدر | الاستخدام |',
        '|---|---|',
        '| `mobile/mobile/lib/services/local_db.dart` | المصدر المرجعي للجداول والحقول وأنواع Drift والقيود المحلية، schemaVersion 51. |',
        '| `mobile/worker/schema.sql` | مخطط D1 السابق؛ كان ناقصاً مقارنةً بقائمة كيانات Flutter. |',
        '| `mobile/worker/src/database.ts` | خريطة كيانات Worker، CRUD، الحذف المنطقي، السجل والتعارضات. |',
        '| `mobile/worker/src/sync.ts` | عقد HTTP للمزامنة وعمليات push/pull/migrate. |',
        '| `mobile/mobile/lib/services/cloudflare_config.dart` | قائمة كيانات Flutter وترتيب migration؛ يستثني hotel_day_ledger من Cloudflare sync. |',
        '| `mobile/mobile/lib/services/cloudflare_sync_manager.dart` | شكل payload، local_uuid، cursor، الضغط gzip، والحذف/التعارضات محلياً. |',
        '',
        '## نتيجة الاستخراج',
        '',
        f"تم استخراج **{len(data['tables'])} جدولاً** مسجلاً في Drift، منها **{len(sync_tables)} كياناً مرشحاً لمخطط D1** و**{len(local_only)} جداول محلية للبنية التشغيلية**. النسخة المحلية هي schemaVersion **{data['schema_version']}**.",
        '',
        '| التصنيف | الجداول |',
        '|---|---|',
        f"| كيانات D1/Worker | {', '.join(table['table_name'] for table in sync_tables)} |",
        f"| محلية فقط أو بنية Flutter | {', '.join(table['table_name'] for table in local_only)} |",
        '',
        '## عقد الأنواع والتحويل',
        '',
        '| Drift | D1/SQLite | ملاحظة التكامل |',
        '|---|---|---|',
        '| `TextColumn` | `TEXT` | قيم نصية وتواريخ ISO/مفاتيح JSON. |',
        '| `IntColumn` | `INTEGER` | المعرفات الرقمية والطوابع الزمنية بالثواني. |',
        '| `RealColumn` | `REAL` | المبالغ والمعدلات. |',
        '| `BoolColumn` | `INTEGER` | Flutter يحول Boolean إلى 0/1 عند SQL؛ يجب أن يبقى ذلك ثابتاً في D1. |',
        '| `SyncFields.localUuid` | `TEXT NOT NULL UNIQUE` | مفتاح المطابقة بين Flutter وD1؛ لا يجوز الاعتماد على `id` وحده. |',
        '| `SyncFields.serverId` | `INTEGER NULL` | يطابق Drift الحالي، بخلاف مخطط D1 السابق الذي عرّفه كنص. |',
        '',
        '## الجداول والحقول والأنواع والقيود',
        '',
    ]
    for table in data['tables']:
        classification = 'D1/Worker sync' if table['table_name'] in D1_TABLES else 'Flutter local-only'
        lines += [f"### `{table['table_name']}` — `{table['class_name']}` ({classification})", '']
        lines += ['| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |', '|---:|---|---|---|---|---|---|---|---|']
        for index, column in enumerate(table['columns'], 1):
            ref = column.get('reference')
            fk = f"`{ref['table_class']}.{ref['field']}`" if ref else ''
            default = column.get('default_expression') or ''
            lines.append(
                f"| {index} | `{column['dart_name']}` | `{column['sql_name']}` | `{column['dart_column_type']}` | `{column['sqlite_affinity']}` | {'نعم' if column['nullable'] else 'لا'} | `{default}` | {'نعم' if column['unique'] else 'لا'} | {fk} |"
            )
        if table['unique_keys_dart_names']:
            lines += ['', f"**المفاتيح الفريدة المركبة المعلنة في Drift:** `{table['unique_keys_dart_names']}`."]
        lines.append('')
    lines += [
        '## فجوات التكامل المؤكدة',
        '',
        '| الفجوة | الدليل | الأثر | القرار المنفذ/الموصى به |',
        '|---|---|---|---|',
        '| مخطط D1 السابق احتوى على 5 كيانات تشغيلية فقط تقريباً، بينما Worker يعلن 20 كياناً. | `database.ts` و`cloudflare_config.dart` مقابل `worker/schema.sql`. | عمليات push لكيانات مثل debts وsalary_* وguest_infos تفشل إذا لم توجد جداولها. | توليد مخطط D1 كاملاً لكل كيانات Worker مع بنية Drift. |',
        '| `id` و`server_id` في مخطط D1 السابق لا يطابقان Drift. | المخطط السابق يستخدم `id TEXT PRIMARY KEY`، بينما Drift يستخدم `id INTEGER AUTOINCREMENT` و`serverId INTEGER`. | create/update والمطابقة عبر local_uuid لا تعمل بشكل موحد. | اعتماد INTEGER AUTOINCREMENT لـ id وINTEGER لـ server_id، مع `local_uuid TEXT UNIQUE`. |',
        '| حقول SyncFields مفقودة من المخطط السابق. | تعريف `SyncFields` في local_db.dart. | pull/push وPRAGMA filtering لا يملكان العقد الكامل. | إضافة local_uuid وcreated/updated/last_modified والحقول الزمنية وversion/vector_clock/device/origin/idempotency_key. |',
        '| Worker يعتمد على `rate_limits` لكنه لم يكن معرفاً في schema السابق. | `worker/src/index.ts`، `checkRateLimit`. | أول طلب API قد يفشل أو يعمل fail-open دائماً. | إضافة جدول rate_limits بمفتاح مركب client_id/window_start. |',
        '| Worker ينشئ devices وقت التشغيل فقط. | `worker/src/database.ts`, registerDevice. | schema غير مكتمل في بيئة جديدة. | إضافة devices إلى DDL الأساسي. |',
        '| `hotel_day_ledger` موجود في Worker mapping لكنه مستبعد من CloudflareConfig migrationOrder. | `cloudflare_config.dart`. | خطر اعتبار جدول محلياً/بعيداً في آن واحد. | إبقاؤه في DDL للتوافق، وتصنيفه صراحةً محلياً في Flutter إلى أن يُتخذ قرار مزامنة مستقل. |',
        '| cursor الحالي رقم timestamp فقط. | `database.ts` pullChanges و`cloudflare_sync_manager.dart`. | عدة سجلات بنفس updated_at مع LIMIT قد تسبب تخطي سجلات. | يلزم لاحقاً cursor مركب `(updated_at, entity, id/local_uuid)` أو monotonic server sequence؛ لا يجوز اعتبار timestamp وحده ضماناً. |',
        '| `updateRecord` يضم حقول data دون filter مقابل PRAGMA. | `worker/src/database.ts`. | قد يفشل تحديث بسبب حقول غير موجودة أو `_entity`. | يجب تطبيق نفس column whitelist على update قبل اعتماد الإنتاج. |',
        '',
        '## مسار التكامل التشغيلي',
        '',
        'يبدأ Flutter بتسجيل الدخول عبر `POST /api/auth/login`، ثم يرسل التسجيلات المتراكمة من Drift `outbox` إلى `POST /api/sync/push` في دفعات gzip، حيث يحتوي كل عنصر على `idempotencyKey` و`entity` و`operation` و`data` و`vectorClock` و`updatedAt`. يعيد Worker نتائج لكل عنصر، ثم يحذف Flutter العنصر الناجح من outbox ويعيد المحاولة في حالات الشبكة أو الأخطاء المؤقتة.',
        '',
        'يسحب Flutter التغييرات عبر `GET /api/sync/pull?cursor=...&limit=...`، ويطابق السجل بواسطة `local_uuid`، ويطبق soft delete عبر `deleted_at`. عند تعارض vector clock، يطبق Flutter محلل التعارض محلياً وقد يعيد النتيجة إلى outbox. لذلك يجب أن تكون أسماء الأعمدة وقيم Boolean والطوابع الزمنية متطابقة بين Drift وD1، وإلا سيظهر فشل مزامنة صامت أو divergence.',
        '',
        '## خطوات تشغيل D1',
        '',
        'يُنفّذ المخطط من مجلد Worker باستخدام الأمر الموجود في `worker/package.json`: `npm run db:init`. يجب تشغيله أولاً على بيئة preview أو قاعدة جديدة، ثم التحقق بواسطة `PRAGMA table_info(<table>)` و`PRAGMA foreign_key_list(<table>)` ومقارنة عدد الجداول مع هذا التقرير قبل تشغيل migration الإنتاجية.',
        '',
        '## مراجع محلية',
        '',
        'الأرقام في هذا التقرير تشير إلى ملفات المصدر ومساراتها داخل المستودع المحلي؛ لا توجد ادعاءات خارجية تحتاج إلى مصدر ويب. التقرير نفسه مولد من `tools/generate_orax_d1_artifacts.py`، بينما JSON الخام هو `tools/orax_schema_extracted.json`.',
    ]
    return '\n'.join(lines) + '\n'


def main() -> None:
    data = json.loads(EXTRACTED.read_text(encoding='utf-8'))
    source = SOURCE.read_text(encoding='utf-8')
    SCHEMA_OUT.write_text(render_schema(data, source), encoding='utf-8')
    REPORT_OUT.parent.mkdir(parents=True, exist_ok=True)
    REPORT_OUT.write_text(render_report(data, source), encoding='utf-8')
    print(json.dumps({
        'schema': str(SCHEMA_OUT),
        'report': str(REPORT_OUT),
        'd1_table_count': len(D1_TABLES),
        'drift_table_count': len(data['tables']),
        'schema_bytes': SCHEMA_OUT.stat().st_size,
        'report_bytes': REPORT_OUT.stat().st_size,
    }, ensure_ascii=False))


if __name__ == '__main__':
    main()
