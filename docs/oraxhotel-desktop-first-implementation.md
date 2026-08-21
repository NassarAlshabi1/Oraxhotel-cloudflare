# تسليم تنفيذ Desktop-first لـ Oraxhotel

## النتيجة التنفيذية

تم إنشاء ترحيلات Cloudflare D1 ومحرك Dart/Flutter Offline-first على أساس نسخة الكمبيوتر Oraxhotel. المصدر المرجعي هو `Hotel_alkheer` في SQL Server، وليس مخطط Flutter السابق. تم حفظ التغييرات في commit `88d4a5555d162d31de08429796867e6a0dd7bf07` ورفعها إلى فرع `feature/orax-flutter-appwrite-integration` في مستودع `Oraxhotel-cloudflare`.

## الترحيلات

| الملف | المحتوى | التحقق |
|---|---|---|
| `worker/migrations/0001_desktop_mirror.sql` | مرآة 47 جدولاً و378 حقلاً من SQL Server مع 39 علاقة FK مؤكدة. | تم تنفيذه في SQLite محلياً. |
| `worker/migrations/0002_sync_infrastructure.sql` | بنية Worker السابقة، إضافة `desktop_sync_events` و`desktop_sync_commands` و`desktop_sync_checkpoints`. | تم تنفيذه في SQLite محلياً. |
| `worker/migrations/0003_flutter_projections.sql` | 20 إسقاطاً متوافقاً مع عقد Flutter الحالي. | تم التحقق من حقول المزامنة والفهارس والعلاقات المختارة. |
| `worker/schema.sql` | ملف مجمع للتشغيل المحلي/التهيئة، يضم 76 جدولاً. | تم تطبيقه عبر Wrangler محلياً. |

تم استبعاد علاقة واحدة من قيود D1 مؤقتاً للمراجعة: `jop_emp_table.id_job_name -> emp_table.id`. نموذج الكمبيوتر المولد يثبت هذه العلاقة حرفياً، لكن اسم الحقل ووجود `jobs_name_table` يجعلانها نقطة تعارض تحتاج قراراً من قاعدة البيانات/قواعد الأعمال؛ لم يتم تخمينها.

## محرك Dart

الملف `mobile/lib/services/desktop_first_sync_engine.dart` يوفر:

| القدرة | التنفيذ |
|---|---|
| الكتابة دون إنترنت | يكتب التطبيق في Drift أولاً، ثم يضيف عملية مستقلة إلى `outbox`. |
| الرفع | يرسل batch إلى `/api/desktop/commands`؛ لا يكتب الهاتف مباشرة في canonical desktop projection. |
| المطابقة | يستخدم `local_uuid` و`idempotency_key`، ويدعم `server_id` عبر الـ adapter. |
| السحب | يقرأ `/api/sync/pull` ويحفظ cursor بعد تطبيق صفحة كاملة محلياً. |
| الحذف | يطبق الحذف المنطقي عبر adapter، ولا يحذف بيانات الهاتف عند فشل الشبكة. |
| إعادة المحاولة | أخطاء الشبكة تعيد السجل إلى `failed`، والأخطاء الدائمة تضعه في `dead` للمراجعة. |
| التعارض | يترك قرار التعارض للـ adapter/Worker بدلاً من الكتابة الصامتة فوق سجل الكمبيوتر. |

محرك Dart مصمم ليستخدم adapters صريحة لكل كيان. هذا مقصود؛ لا يمكن نسخ حقول جداول الكمبيوتر إلى جداول Flutter عشوائياً لأن دلالات `recetion_table` و`bills_table` مركبة.

## Worker

تم إضافة `/api/desktop/commands`. المسار يتحقق من شكل الأمر، ويمنع الكيانات غير المعتمدة، ويحفظ الأمر بشكل idempotent في `desktop_sync_commands` بحالة `pending`. لا يقوم المسار بتعديل سجلات Oraxhotel الأصلية، لأن SQL Server هو مصدر الحقيقة.

لكي تكتمل دورة الهاتف إلى الكمبيوتر، يلزم مكوّن مكتبي لاحق يقوم بجلب الأوامر `pending`، وتطبيقها على SQL Server، ثم نشر النتيجة canonical إلى D1 في `desktop_sync_events` والإسقاطات. عدم وجود هذا المكوّن يعني أن أمر الهاتف سيُحفظ بأمان لكنه لن يغير SQL Server تلقائياً بعد.

## الاختبارات المنفذة

تم اجتياز فحص SQLite للترحيلات، وفحص TypeScript عبر `npm run typecheck`، وفحص `git diff --check`، وتطبيق `schema.sql` عبر Wrangler في قاعدة محلية فقط. كما تم فحص عدد الجداول والحقول والعلاقات وعدم وجود حالة Wrangler المحلية ضمن الملفات المرفوعة.

لم يتم تشغيل `dart analyze` أو `flutter analyze` لأن executables الخاصة بـ Dart وFlutter غير متاحة في بيئة التنفيذ. لذلك لا يُدّعى أن الملف خضع لمحلل Dart؛ يجب تشغيله في بيئة Flutter الفعلية قبل إصدار التطبيق.

## الخطوة المطلوبة قبل الإنتاج

قبل تنفيذ الترحيل على D1 البعيدة، يجب اختبار نسخة تجريبية من SQL Server، اعتماد صيغة التواريخ والمبالغ، تنفيذ ناشر الكمبيوتر، تسجيل دخول Wrangler، ثم تطبيق الترحيلات على قاعدة D1 غير إنتاجية واختبار سيناريوهات الإنشاء والتعديل والحذف وانقطاع الشبكة وإعادة الإرسال والتعارض.
