# خطة التكامل Desktop-first لنظام Oraxhotel

> **المبدأ الملزم:** نسخة الكمبيوتر Oraxhotel وقاعدة `Hotel_alkheer` في SQL Server هي مصدر الحقيقة. مخطط Flutter/Dart وCloudflare الحاليان يمثلان أهداف تكامل ومقارنة فقط، ولا يُسمح بتغيير مصدر بيانات الكمبيوتر ليتوافق مع Flutter بشكل أعمى.

## 1. المصدر المؤكد

نسخة الكمبيوتر هي تطبيق ASP.NET Core على `.NET 8` يستخدم `Hotel_alkheer` عبر SQL Server وLinq2DB/EF. ملف `HotelSys/database/Hotel_alkheer_init.sql` يعرّف **47 جدولاً و378 حقلاً**، و`HotelSys/db/db.generated.cs` يثبت أنواع C# وعلامات `PrimaryKey` و`Identity` والارتباطات المولدة. إعداد `Startup.cs` يربط `HotelAlkheerDB` بـ `Configuration.GetConnectionString("cc")` عبر `UseSqlServer`.

يوجد في الكمبيوتر تكامل Appwrite قائم بالفعل، وهو الدليل التنفيذي الأقوى على قواعد التحويل الحالية. خدماته تقرأ SQL Server ثم تنشر إسقاطات مهيأة لتطبيق Flutter؛ لكنها لا تمثل كل جداول قاعدة الكمبيوتر.

## 2. التحويلات المثبتة من الكود الحالي

| كيان Flutter/السحابي | مصدر الكمبيوتر المثبت | قاعدة التحويل المثبتة | درجة الثقة |
|---|---|---|---|
| `rooms` | `rooms_table` مع `type_rooms_table` و`price_rooms_table` و`status_current_table` | `rooms_table.name_r` يصبح `roomNumber`، واسم النوع من `type_rooms_table.name_t`، والسعر من أحدث `price_rooms_table`، والحالة من أحدث `status_current_table`. | مثبت في `AppwriteRoomSyncService`. |
| `bookings` | `recetion_table` مع `rooms_table` و`my_customers` و`customer_table` | كل استقبال/إقامة يصبح حجزاً؛ الغرفة تأتي من `rooms_table`، والضيف عبر `recetion_table.id_my_customer → my_customers.id_customer → customer_table`. | مثبت في `AppwriteBookingSyncService`. |
| `guest_infos` | `customer_table` و`my_customers` و`recetion_table` و`rooms_table` | هوية الضيف من `customer_table`، والغرفة النشطة تستنتج من الاستقبال غير المغلق/غير الملغى. | مثبت في `AppwriteGuestInfoSyncService`. |
| `payments` | `bills_table` مع `recetion_table` و`rooms_table` | تُنشر الفاتورة فقط إذا كان لها `id_reception` و`pay_amount > 0`؛ المبلغ هو `pay_amount`، والتاريخ `date`، وطريقة الدفع `type_pay`. | مثبت في `AppwritePaymentSyncService`. |

المعرف السحابي الحالي لهذه الإسقاطات حتمي من معرف SQL Server، مثل `orax-booking-{id}`، مع `serverId` و`localUuid` deterministic. لذلك يجب الحفاظ على هذا المبدأ عند الانتقال إلى Worker/D1.

## 3. الجداول التي لا يجوز ربطها آلياً بعد

وجود جدول Flutter باسم مشابه لا يثبت أنه يقابل جدول الكمبيوتر. لا يوجد في الكود المدقق mapping كامل ومثبت للكيانات التالية: `expenses`, `employees`, `debts`, `cash_transactions`, `booking_notes`, `shift_notes`, `booking_nights`, `salary_cycles`, `salary_payments`, `salary_withdrawals`, `salary_carry_over_logs`, `price_adjustments`, `booking_price_adjustments`, `audit_logs`, `payment_voids`.

أقرب مرشحات تحتاج دراسة أعمال مستقلة هي `emp_table` للموظفين، و`bond_table` و`bills_table` للحركة المالية، و`price_rooms_table` لتسعير الغرف. لا يجوز تحويل هذه المرشحات إلى mapping نهائي قبل مراجعة شاشات الكمبيوتر واستعلاماته وتحديد معنى كل عملية، خصوصاً لأن `bills_table` و`bond_table` قد يمثلان قيوداً محاسبية متعددة وليسا بالضرورة `payments` أو `expenses` واحداً لواحد.

## 4. البنية الصحيحة للمزامنة

يجب أن يعمل الكمبيوتر كناشر ومصدر حقيقة، لا كعميل يقرأ من D1 ليعيد بناء بياناته. المسار المقترح هو:

```text
SQL Server / Hotel_alkheer
        │
        │  Desktop Publisher: قراءة الجداول + mapping مثبت + checkpoint
        ▼
Cloudflare Worker / D1 — canonical projections + sync log + idempotency
        ▲                                      │
        │                                      │ pull
        │                                      ▼
Flutter/Dart — Drift/SQLite + outbox + adapters
```

بالنسبة للبيانات المنشأة أو المعدلة في الكمبيوتر، ينشر الناشر الإسقاط بعد اكتمال عملية SQL Server. وبالنسبة لتعديلات الهاتف، لا ينبغي أن يكتب Worker مباشرة فوق السجل المملوك للكمبيوتر؛ بل يضع الأمر في طابور `desktop_commands`، ثم يسحبه الكمبيوتر ويطبقه في SQL Server، وبعد نجاحه يعيد نشر النسخة المعتمدة إلى D1. هذا يحافظ على قاعدة أن الكمبيوتر هو مصدر الحقيقة ويمنع أن يعكس الهاتف حالة غير معتمدة.

## 5. الفرق بين سجل الحدث وسجل البيانات

كل عملية أعمال مهمة يجب أن تنتج حدثاً مستقلاً يحوي `event_id` ثابتاً، `source_device`, `source_system`, `entity`, `entity_key`, `operation`, `occurred_at`, `actor_id`, وpayload قبل/بعد عند الحاجة. تخزين الصف النهائي وحده لا يكفي لمعرفة من نفذ العملية أو لإعادة المحاولة.

الـ `outbox` الموجود في Flutter مناسب لتجميع تغييرات الهاتف، لكنه لا يثبت وجود change capture داخل الكمبيوتر. الناشر المكتبي يحتاج checkpoint لكل projection أو Change Tracking/CDC من SQL Server. لا ينبغي استخدام `updated_at` من D1 وحده لاكتشاف تغييرات SQL Server لأن الجداول الأصلية لا تحتوي حقلاً موحداً بهذا الاسم.

## 6. قرار تنفيذ D1

لا يُعتمد `worker/schema.sql` المشتق من Flutter باعتباره نسخة قاعدة الكمبيوتر. يجب إنشاء D1 بطبقتين واضحتين:

| الطبقة | المحتوى |
|---|---|
| طبقة الإسقاطات | الجداول التي يحتاجها Flutter، مع أسماء الحقول المتفق عليها بعد mapping الكمبيوتر المثبت. |
| طبقة المصدر/التدقيق | `source_records` أو جداول إسقاط مكتملة، و`sync_events` و`desktop_commands` وcheckpoints وidempotency. |

أما إنشاء 47 جدولاً بأسماء SQL Server داخل D1 فهو خيار mirror كامل، ويحتاج قراراً مستقلاً لأنه يزيد التعقيد ولا يحل وحده مشكلة اختلاف معنى `recetion_table` و`bills_table` عن نماذج Flutter.

## 7. بوابة الجاهزية

لا يُنقل التنفيذ إلى Cloudflare D1 قبل اكتمال العناصر التالية: اعتماد mapping لكل كيان مطلوب، تحديد صلاحية القراءة/الكتابة لكل جهاز، تحديد مصدر التقاط أحداث الكمبيوتر، تثبيت صيغة التاريخ والمبالغ، اختبار deterministic IDs على نسخة بيانات غير إنتاجية، ثم تشغيل pull/push بين كمبيوتر تجريبي وهاتف تجريبي مع إثبات عدم تكرار الحدث وعدم ضياعه عند انقطاع الشبكة.

## المراجع المحلية

[1]: `../HotelSys/database/Hotel_alkheer_init.sql` — مخطط SQL Server الأصلي لنسخة الكمبيوتر.
[2]: `../HotelSys/db/db.generated.cs` — نماذج Linq2DB والأنواع والارتباطات المولدة.
[3]: `../HotelSys/Startup.cs` — تسجيل اتصال SQL Server وخدمات المزامنة الحالية.
[4]: `../HotelSys/Integrations/Appwrite/AppwriteRoomSyncService.cs` — إسقاط الغرف.
[5]: `../HotelSys/Integrations/Appwrite/AppwriteBookingSyncService.cs` — إسقاط الحجوزات.
[6]: `../HotelSys/Integrations/Appwrite/AppwriteGuestInfoSyncService.cs` — إسقاط الضيوف.
[7]: `../HotelSys/Integrations/Appwrite/AppwritePaymentSyncService.cs` — إسقاط المدفوعات.
