# تكامل Orax Hotel مع Flutter عبر Appwrite Cloud

## القرار المعماري

يعمل **Orax Hotel** كتطبيق الكمبيوتر ومصدر بيانات التشغيل المحلي في SQL Server، بينما يعمل **Flutter** كتطبيق الهاتف ويستمر في استخدام قاعدة SQLite المحلية مع `AppwriteSyncManager` الموجود في المشروع. تقوم خدمة التكامل الجديدة في Orax بنشر كتالوج الغرف إلى collection `rooms` في Appwrite Cloud، ثم يسحب Flutter التغييرات عبر مزامنته الحالية. لا يتصل الهاتف مباشرةً بقاعدة SQL Server.

هذا هو أول تدفق رأسي قابل للاختبار: **SQL Server/Orax → Appwrite Cloud → Flutter**. لم يتم الادعاء بأن الحجوزات والمدفوعات وبقية الجداول أصبحت متزامنة؛ ستحتاج كل مجموعة إلى محول حقول وعقد تعارض خاص بها.

## ما تم تنفيذه

أضيفت في Orax خدمة `AppwriteRoomSyncService` التي تقرأ الغرف من `RoomsTable`، وتستخرج اسم نوع الغرفة من `TypeRoomsTable`، والسعر من `PriceRoomsTable`، والحالة الحالية من `StatusCurrentTable`. بعد ذلك تستخدم REST API الخادمية في Appwrite لإنشاء أو تحديث مستند الغرفة في collection `rooms`. يطابق المستند بواسطة `serverId` أو `roomNumber`، ويستخدم معرفاً ثابتاً بصيغة `orax-room-{id}` عند عدم وجود مستند سابق، مع إنشاء `localUuid` بصيغة UUID قياسية حتى يقبله `IdResolver` في Flutter.

أضيفت خدمة خلفية `AppwriteRoomSyncHostedService`. عند تفعيل `AutoSyncRooms` تنتظر 15 ثانية بعد تشغيل Orax، ثم تنفذ المزامنة وتعيدها حسب `SyncIntervalMinutes`. فشل Appwrite لا يوقف Orax؛ تسجل الخدمة الخطأ وتعيد المحاولة في الدورة التالية.

أضيفت نقاط API إدارية خلف مصادقة Orax الحالية:

| المسار | الوظيفة |
|---|---|
| `GET /api/appwrite/health` | يعرض حالة تفعيل Appwrite وcollection والفحص الأساسي للإعدادات |
| `POST /api/appwrite/sync/rooms` | يشغّل مزامنة الغرف فوراً ويعيد أعداد الإنشاء والتحديث والفشل |

Flutter لا يحتاج إلى عميل API جديد لهذا التدفق؛ `AppwriteService` يستخدم `Databases` و`AppwriteSyncManager` يقرأ collection `rooms`، بينما `RoomsAdapter` يتعرف على `roomNumber`, `type`, `price`, `status`, `serverId` وحقول المزامنة التي تنشرها خدمة Orax.

## إعداد Orax

يوجد قسم `Appwrite` في `HotelSys/appsettings.json` يتضمن endpoint وproject/database IDs وcollection ID وفترة المزامنة. يمكن تعطيل التكامل عبر `Enabled: false`، أو تعطيل التشغيل التلقائي فقط عبر `AutoSyncRooms: false` مع الإبقاء على endpoint اليدوي متاحاً للحساب المصادق.

يجب تشغيل Orax بعد توافر اتصال SQL Server. عند التشغيل، يراجع السجل عن رسائل `Appwrite room sync completed`. ويمكن اختبار الحالة من جلسة تسجيل دخول Orax عبر `/api/appwrite/health`، ثم تشغيل `POST /api/appwrite/sync/rooms` عند الحاجة.

## حدود الإصدار الحالي

النسخة الحالية تنفذ مزامنة **الغرف فقط** من Orax إلى Appwrite. لا تكتب الخدمة حجوزات أو نزلاء أو مدفوعات، ولا تنفذ بعد مزامنة ثنائية الاتجاه من Flutter إلى SQL Server. كما أن قراءة rooms تستخدم الاستجابة الكاملة وتتحقق من أن عدد المستندات المستلم يساوي `total`؛ إذا تجاوزت collection الحد الافتراضي في Appwrite، تتوقف المزامنة برسالة واضحة بدلاً من إسقاط سجلات بصمت. يجب معالجة pagination في مرحلة الكيانات التالية قبل اعتمادها على مجموعات أكبر.

اختُبر اتصال المشروع الحالي بـ Appwrite Cloud فعلياً: collection `rooms` أعادت 20 مستنداً، كما اجتاز payload الكامل اختباراً مؤقتاً للإنشاء ثم التحديث عبر PUT ثم الحذف، ولم يترك الاختبار مستنداً. اجتاز HotelSys البناء وpublish موجهاً إلى `win-x64` مع `0 Error(s)`؛ التحذيرات المتبقية من Views قديمة وليست من ملفات التكامل. اختبار تشغيل Orax مع SQL Server الفعلي واختبار دورة Flutter على جهاز Android أو iOS يظلان مطلوبين خارج بيئة Linux الحالية.

## المراجع

[1]: https://appwrite.io/docs/quick-starts/dotnet "Appwrite .NET quick start"

[2]: https://appwrite.io/docs/references/cloud/server-rest/databases#list-documents "Appwrite Databases REST API"

[3]: https://appwrite.io/docs/queries "Appwrite query syntax and pagination"

[4]: https://appwrite.io/docs/partners/project/api-keys "Appwrite API keys and scopes"
