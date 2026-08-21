from __future__ import annotations

import json
from pathlib import Path

BASE = Path('/home/ubuntu/oraxhotel2024')
DATA = BASE / 'tools/orax_desktop_schema_extracted.json'
OUT = BASE / 'docs/oraxhotel-desktop-source-audit.md'


def main() -> None:
    data = json.loads(DATA.read_text(encoding='utf-8'))
    tables = data['tables']
    total_columns = sum(table['column_count'] for table in tables)
    lines: list[str] = [
        '# التدقيق المرجعي لنسخة الكمبيوتر Oraxhotel',
        '',
        '> مصدر الحقيقة لهذا التقرير هو ملف SQL Server الأصلي المضمّن في نسخة الكمبيوتر، وليس مخطط Flutter الحالي أو مخطط D1 السابق.',
        '',
        '## المصدر المرجعي',
        '',
        '| العنصر | القيمة |',
        '|---|---|',
        f"| قاعدة البيانات | `{data['database_name']}` |",
        f"| المحرك | `{data['dialect']}` |",
        '| ملف المصدر | `HotelSys/database/Hotel_alkheer_init.sql` |',
        f"| الجداول | **{len(tables)}** |",
        f"| الحقول | **{total_columns}** |",
        '| المفاتيح الأساسية | مستخرجة من قيود `PRIMARY KEY` لكل جدول |',
        '',
        '## خلاصة مقارنة المصادر',
        '',
        '| المصدر | عدد الجداول | المعنى الهندسي |',
        '|---|---:|---|',
        f"| نسخة الكمبيوتر SQL Server | {len(tables)} | المصدر المرجعي الأول الذي يجب أن تُبنى عليه المواءمة. |",
        '| Flutter/Drift الحالي | 30 | نسخة تطبيق محلي ومخطط تشغيل/مزامنة؛ لا يُعتبر بديلاً عن مخطط الكمبيوتر. |',
        '| Worker/D1 الحالي | 26 | مخطط تكامل سابق/مستهدف؛ يجب إعادة تصميمه بعد اعتماد مخطط الكمبيوتر. |',
        '',
        '## قواعد التحويل المرشحة إلى D1',
        '',
        '| SQL Server في الكمبيوتر | Cloudflare D1/SQLite | ضابط التحويل |',
        '|---|---|---|',
        '| `int`, `bigint` | `INTEGER` | تُحفظ القيمة الرقمية دون تحويل نصي. |',
        '| `float` | `REAL` | يجب فحص الدقة المالية؛ المبالغ الحساسة تحتاج قراراً منفصلاً بشأن minor units أو NUMERIC-as-text. |',
        '| `bit` | `INTEGER` بقيمتي 0/1 | Flutter/Dart يجب أن يحول Boolean إلى 0/1. |',
        '| `date`, `datetime` | `TEXT` بصيغة ISO-8601 أو `INTEGER` epoch | يجب اختيار صيغة موحدة قبل المزامنة؛ لا يجوز الخلط بين الصيغ. |',
        '| `nvarchar(n/max)` | `TEXT` | حد الطول يُحفظ كقاعدة تحقق في التطبيق إن كان مهماً. |',
        '| `IDENTITY(1,1)` | `INTEGER PRIMARY KEY` | أثناء migration يجب الحفاظ على IDs القديمة؛ لا نعيد توليدها. |',
        '',
        '## الجداول والحقول المستخرجة بالكامل',
        '',
    ]
    for table in tables:
        pk = ', '.join(f'`{value}`' for value in table['primary_key']) or '—'
        lines += [
            f"### `{table['table_name']}`",
            '',
            f"المخطط: `dbo` · عدد الحقول: **{table['column_count']}** · المفتاح الأساسي: {pk} · سطر المصدر: {table['source_line']}",
            '',
            '| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |',
            '|---:|---|---|---|---|---|---|---|',
        ]
        for index, column in enumerate(table['columns'], 1):
            definition = column['definition'].replace('|', '\\|')
            default = (column.get('default') or '').replace('|', '\\|')
            lines.append(
                f"| {index} | `{column['name']}` | `{column['sql_server_type']}` | `{column['base_type']}` | {'نعم' if column['nullable'] else 'لا'} | {'نعم' if column['identity'] else 'لا'} | `{default}` | `{definition}` |"
            )
        lines.append('')
    lines += [
        '## قرار المصدر قبل التنفيذ',
        '',
        'لا يجوز توليد D1 النهائي من جداول Flutter الحالية قبل عمل mapping موثق من جداول الكمبيوتر. بعض الجداول لها تشابه اسمي أو وظيفي جزئي فقط؛ على سبيل المثال `rooms_table` في نسخة الكمبيوتر يحتوي خصائص نوع/تكوين الغرف، بينما `rooms` في Flutter يمثل سجل غرفة تشغيلياً. كذلك `recetion_table` يمثل سجلات استقبال/إقامة، ولا يجوز إسقاطه آلياً على `bookings` دون تحديد علاقة الحقول.',
        '',
        'المطلوب في الخطوة التالية هو بناء mapping لكل جدول كمبيوتر إلى أحد التصنيفات: **يُزامن كما هو**، **يُدمج مع كيان Flutter**، **يُقسّم إلى أكثر من كيان**، أو **يبقى خاصاً بسطح المكتب**. بعد اعتماد هذا mapping فقط يُعاد توليد D1 وطبقة Worker ومحولات Flutter.',
        '',
        '## حدود الدليل الحالي',
        '',
        'الملف المرجعي يثبت مخطط SQL Server وتعريفات النماذج، لكنه لا يثبت وجود بيانات إنتاجية داخل ملفات المستودع؛ ملفات `HotelSys/Data/*.db` منفصلة ولا تُعامل تلقائياً على أنها قاعدة Oraxhotel الأساسية. كما أن تطبيق المزامنة للكمبيوتر لم يُثبت بعد من الكود الحالي، ولذلك لا ينبغي الادعاء بأن أحداث الكمبيوتر تصل إلى الهاتف قبل تنفيذ عميل/Bridge واضح.',
        '',
        '## مرجع المصدر',
        '',
        '[1]: `../HotelSys/database/Hotel_alkheer_init.sql` — SQL Server initialization script for the desktop Oraxhotel model.',
        '[2]: `../HotelSys/Models/Hotel_alkheerContext.cs` — EF model and relationship configuration.',
        '[3]: `../HotelSys/db/db.generated.cs` — generated Linq2DB table/column type definitions.',
    ]
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print(json.dumps({
        'report': str(OUT),
        'table_count': len(tables),
        'column_count': total_columns,
        'report_bytes': OUT.stat().st_size,
    }, ensure_ascii=False))


if __name__ == '__main__':
    main()
