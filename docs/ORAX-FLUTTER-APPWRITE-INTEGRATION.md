# تكامل Orax Hotel مع Flutter عبر Appwrite Cloud

## القرار المعماري

يعمل **Orax Hotel** كتطبيق الكمبيوتر ومصدر بيانات التشغيل المحلي في SQL Server، بينما يعمل **Flutter** كتطبيق الهاتف ويستمر في استخدام قاعدة SQLite المحلية مع `AppwriteSyncManager` الموجود في المشروع. تقوم خدمات التكامل في Orax بنشر بيانات الغرف والحجوزات والنزلاء والمدفوعات إلى Appwrite Cloud، ثم يسحب Flutter التغييرات عبر مزامنته الحالية. لا يتصل الهاتف مباشرةً بقاعدة SQL Server.

هذا هو التدفق الرأسي المعتمد: **SQL Server/Orax → Appwrite Cloud → Flutter**. الكتابة العكسية من Flutter إلى Orax ليست جزءاً من هذا الإصدار؛ ولا يجوز للهاتف الكتابة في جداول Orax مباشرة، بل يلزم لاحقاً API أعمال صريح ومراجع للصلاحيات والتعارضات.

## الكيانات ومصادر الحقيقة

| الكيان | مصدر Orax | Collection | هوية Appwrite | الحقل الخادمي |
|---|---|---|---|---|
| الغرفة | `RoomsTable` مع النوع والسعر والحالة | `rooms` | `orax-room-{id}` أو المستند المطابق الحالي | `serverId` |
| الحجز | `RecetionTable` مع الغرفة والنزيل | `bookings` | `orax-booking-{id}` أو مستند legacy المطابق | `serverBookingId` |
| النزيل | `CustomerTable` المرتبط بـ `MyCustomer` | `guest_infos` | `orax-guest-{customerId}` | `serverId` |
| الدفعة | `BillsTable` المرتبط بحجز وذو `PayAmount > 0` | `payments` | `orax-payment-{billId}` | `serverPaymentId` |

لكل سجل ينشئه Orax قيمة `localUuid` حتمية مشتقة من نوع الكيان ورقم السجل. بذلك تبقى قيمة UUID ثابتة بين دورات المزامنة ولا تعتمد على auto-increment محلي في هاتف Flutter.

## مزامنة الغرف

تقرأ `AppwriteRoomSyncService` الغرف من `RoomsTable`، وتستخرج اسم نوع الغرفة من `TypeRoomsTable`، والسعر من أحدث سجل صالح في `PriceRoomsTable`، والحالة الحالية من أحدث سجل في `StatusCurrentTable`. يطابق المستند أولاً بواسطة `serverId`، ثم بواسطة `roomNumber` عند عدم وجود serverId، بشرط أن تكون الهوية وحيدة وأن يكون المستند موثقاً كمستند Orax أو server. إذا وُجد أكثر من مستند لنفس الهوية فلا تختار الخدمة مستنداً عشوائياً، بل تسجل تعارضاً وتتخطى الغرفة.

| رمز Orax | المعنى المثبت في Orax | القيمة المنشورة لـ Flutter |
|---|---|---|
| `1` | فارغة | `شاغرة` |
| `2` | تنضيف | `cleaning` |
| `3` | صيانة | `maintenance` مع `requiresMaintenance=true` |
| `4` | حجز بدون تسجيل دخول | `مؤقت` |
| `5` | مشغولة | `محجوزة` |

هذا التحويل ضروري لأن `StatusUtils` في Flutter يتعرف على الحالات المعيارية ولا يتعامل مع رموز Orax الرقمية كحالات عرض.

## مزامنة الحجوزات

تقرأ `AppwriteBookingSyncService` سجلات `RecetionTable`، ثم تربط `IdRoom` بـ `RoomsTable` و`IdMyCustomer` بـ `MyCustomer` ثم `CustomerTable`. تنشر الخدمة بيانات الغرفة واسم النزيل وهاتفه وجنسيته ووثيقة هويته وتواريخ الإقامة والتواريخ الفعلية والحالة وعدد الليالي وأيام الفندق.

حالة الحجز منشورة وفق الخريطة المثبتة في `ReceptionService`:

| شرط Orax | قيمة Flutter |
|---|---|
| `Status=1` | `مؤقت` |
| `Status=2` مع `IsChechin=true` | `checked_in` |
| `Status=2` قبل تسجيل الدخول | `مؤقت` |
| `Status=3` | `checked_out` |
| `Status=4` | `cancelled` |

تستخدم المزامنة `serverBookingId` كهوية أساسية. ولتسوية المستندات القديمة التي لا تحمل هذا الحقل، يُستخدم fallback وحيد على `roomNumber + checkinDate` للمستندات الموثقة كمصدر server أو legacy بلا مصدر. المستندات `origin=local` لا تدخل في المطابقة حتى لا يكتب Orax فوق حجز أنشأه الهاتف. إذا تكرر المفتاح تتخطى الخدمة السجل وتسجل تعارضاً. أما السجلات التي لا تطابق شيئاً فتستخدم المعرف الثابت `orax-booking-{id}`.

يُحسب `hotelDayCheckin` و`hotelDayCheckout` بقاعدة Flutter نفسها: بداية اليوم الفندقي عند الساعة 14:01، وما قبلها ينتمي إلى اليوم التقويمي السابق. ويُحسب الحد الأدنى لعدد الليالي بقاعدة `Time.nightsWithCutoff` المكافئة في الخادم.

## مزامنة معلومات النزلاء

تقرأ `AppwriteGuestInfoSyncService` سجلات `MyCustomer` مع سجل `CustomerTable` المرتبط، وتنشر الاسم والجنسية ونوع ورقم الإثبات وتاريخ ومكان الإصدار والملاحظات. تختار الخدمة الغرفة من أحدث حجز نشط للنزيل، مع استبعاد الحجوزات الخارجة أو الملغاة. إذا لم يوجد حجز نشط، تُرسل `roomNumber` كسلسلة فارغة دون اختلاق غرفة.

هوية ملف النزيل هي `CustomerTable.Id` وليس `MyCustomer.Id`، لأن `CustomerTable` هو مصدر حقول الهوية نفسها. ولتسوية المستندات القديمة ذات `serverId=null`، تستخدم الخدمة `guestName + idNumber + nationality` فقط عندما يكون رقم الإثبات موجوداً؛ التكرار يؤدي إلى تعارض وتخطٍ، بينما تبقى مستندات `origin=local` محمية من الكتابة فوقها.

## مزامنة المدفوعات

تقرأ `AppwritePaymentSyncService` الفواتير التي تحقق الشرطين معاً: `IdReception IS NOT NULL` و`PayAmount > 0`. لا تُنشر الفواتير غير المرتبطة بحجز أو ذات الدفع الصفري حتى لا تظهر إيرادات وهمية في الهاتف.

| حقل Orax | حقل Appwrite |
|---|---|
| `BillsTable.Id` | `serverPaymentId` |
| `IdReception` | `serverBookingId` و`bookingUuidCache` الحتمي |
| `PayAmount` | `amount` |
| `Date` | `paymentDate` و`hotelDayKey` |
| `TypePay` | `paymentMethod` |
| `NumReference` | `referenceNumber` |
| `Note` | `notes` |
| `RestAmount > 0` | `isPendingBalance=true` |

تُنشر الدفعة كمستند immutable من مصدر `server` و`sync_origin=orax`. ولتسوية المستندات القديمة ذات `serverPaymentId=null`، تستخدم الخدمة `roomNumber + paymentDate` حتى مستوى الثانية + `amount`، مع رفض أي تكرار. لا يدخل `paymentMethod` في الهوية لأنه يختلف نصياً بين Orax والبيانات الحالية في Flutter. لا يُجرى حذف أو عكس دفعة في هذه المرحلة، ولا يُفترض أن `BillsTable` يغطي سندات أخرى مثل `BondTable` دون تحقق مستقل من مخطط قاعدة البيانات.

## REST وpagination

أضيف `AppwriteRestClient` مشترك لكل الكيانات. يستخدم العميل headers الخادمية المطلوبة، وينفذ upsert عبر `PUT /documents/{documentId}` ببيانات `data` فقط، ثم يستخدم `POST` مع `documentId` عند عدم وجود المستند. وتستخدم قراءة المستندات صيغة Appwrite JSON queries الرسمية:

```text
queries[]={"method":"limit","values":[100]}
queries[]={"method":"offset","values":[0]}
```

يستمر العميل حتى جمع العدد الموجود في `total`، ويتوقف بخطأ واضح إذا انتهت صفحة قصيرة قبل اكتمال العدد أو لم يتطابق العدد النهائي. لا تعتمد خدمات الكيانات على استجابة الصفحة الأولى فقط.

أثبت فحص استجابة Appwrite الفعلية أن خصائص المستند مثل `origin` و`roomNumber` و`serverBookingId` تُعاد على المستوى الأعلى للمستند، وليست داخل كائن `data`. لذلك يستخدم `AppwriteDocument` الآن `JsonExtensionData` لالتقاط الخصائص الديناميكية، ويعرضها داخلياً عبر `Data` موحد؛ كما يدعم الاستجابة المتداخلة القديمة احتياطياً. هذا التفصيل ضروري حتى تعمل مطابقة الهوية فعلياً مع REST Cloud.

## الخدمات ونقاط الإدارة

تُسجل الخدمات الأربعة كـ scoped services، ويعمل لكل دورة HostedService ضمن scope مستقل حتى لا يُستخدم اتصال قاعدة البيانات بعد انتهاء عمره. خدمة الغرف الحالية مستقلة، بينما تنفذ `AppwriteCoreSyncHostedService` الحجوزات ثم النزلاء ثم المدفوعات حسب مفاتيح التشغيل التلقائي.

| المسار | الوظيفة |
|---|---|
| `GET /api/appwrite/health` | يعرض حالة Appwrite، collections، ومفاتيح التشغيل التلقائي دون كشف المفتاح |
| `POST /api/appwrite/sync/rooms` | مزامنة الغرف فوراً |
| `POST /api/appwrite/sync/bookings` | مزامنة الحجوزات فوراً |
| `POST /api/appwrite/sync/guests` | مزامنة ملفات النزلاء فوراً |
| `POST /api/appwrite/sync/payments` | مزامنة المدفوعات فوراً |

جميع نقاط المزامنة خلف مصادقة Orax الحالية عبر `[Authorize]`. Flutter لا يحتاج إلى استدعائها؛ يقرأ Appwrite بواسطة `AppwriteSyncManager` وadapters الموجودة في مجلد `mobile`.

## إعداد Orax

يتضمن قسم `Appwrite` في `HotelSys/appsettings.json` مفاتيح `AutoSyncRooms` و`AutoSyncBookings` و`AutoSyncGuests` و`AutoSyncPayments`، ومعرفات collections الأربع، و`PageSize` الافتراضي 100، وفترة المزامنة. يمكن تعطيل التكامل عبر `Enabled: false`، أو تعطيل أي مزامنة تلقائية مع إبقاء endpoint اليدوي متاحاً للحساب المصادق.

عند التشغيل، يراجع المسؤول رسائل `Appwrite room sync completed` ورسائل `Appwrite bookings/guest_infos/payments sync completed`. يجب تشغيل Orax بعد توافر SQL Server؛ فاختبار دورة البيانات الحقيقية يتطلب بيئة Windows تحتوي SQL Server وقاعدة Orax الفعلية.

## اختبارات منفذة ونتائجها

| الاختبار | النتيجة |
|---|---|
| بناء `HotelSys.csproj` للهدف `win-x64` | ناجح: `0 Error(s)`؛ التحذيرات القائمة من Views وأكواد قديمة خارج التكامل |
| قراءة collections بالت pagination JSON الرسمية | `rooms=20`, `bookings=200`, `payments=1125`, `guest_infos=119`، وكلها HTTP 200 |
| AppwriteRestClient runtime probe | جمع 200 حجزاً فعلياً عبر صفحتين 100+100، مع التحقق من `origin` و`roomNumber` و`serverBookingId` top-level |
| تدقيق legacy identities | `bookings`: 199 server و1 local مع 14 مفاتيح room/date مكررة؛ `guest_infos`: 87 server و32 local مع 5 هويات نزيل مكررة؛ `payments`: 896 server و229 local دون تكرار room/date/amount |
| probe payloads الإنتاجية | قبول إنشاء وحذف payloads واقعية للحجز والدفعة والنزيل مع الحقول الاختيارية `null`: HTTP 201 ثم HTTP 204 |
| probe إنشاء وحذف `rooms` | إنشاء HTTP 201، حذف HTTP 204 |
| probe إنشاء وحذف `bookings` | إنشاء HTTP 201، حذف HTTP 204 |
| probe إنشاء وحذف `payments` | إنشاء HTTP 201، حذف HTTP 204 |
| probe إنشاء وحذف `guest_infos` | إنشاء HTTP 201، حذف HTTP 204 |
| بقاء مستندات probe | لا تُترك مستندات probe؛ التنظيف ينفذ بعد كل اختبار |
| تشغيل Orax مع SQL Server فعلي | لم يُنفذ في Linux؛ يلزم Windows |
| دورة Flutter على Android/iOS | لم تُنفذ لأن Flutter SDK غير مثبت في sandbox الحالية |

## حدود الكتابة العكسية وسلامة outbox

يحتوي `AppwriteSyncManager` في Flutter على مسار push صريح (`sync(push: true, pull: true)`) وعلى `_pushAllEntities` ومؤقت debounced push يعتمد على outbox. هذا المسار مناسب للكيانات التي يكون الهاتف مصدر إنشائها، لكنه ليس عقداً آمناً لكتابة سجلات Orax التشغيلية أو المالية؛ فإذا فُعّل بلا API أعمال قد يكتب الهاتف فوق مستند حجز أو دفعة نشرها Orax، أو ينشئ تعارضاً لا يمكن حسمه على مستوى SQL Server.

لذلك يثبت هذا الإصدار الحد الآمن التالي: **Orax يكتب إلى Appwrite، وFlutter يقرأ بيانات Orax ويحدّث SQLite المحلي فقط**. لا تستقبل خدمات Orax الحالية أي كتابة هاتفية إلى SQL Server، ولا تُعدّل خدمة Appwrite الخادمية سجلات Orax استناداً إلى outbox الهاتف. أي مزامنة عكسية مستقبلية يجب أن تمر عبر API أعمال مصادق عليه، وتتحقق من `serverBookingId` أو `serverPaymentId`، وتفرض idempotency وversion/lastModified، وتسجل تدقيقاً، وتفصل أوامر الحجز عن أوامر الدفع immutable. إلى أن تُنفذ هذه الضوابط، يجب إبقاء الكتابة العكسية للحجوزات والمدفوعات معطلة.

## الحدود المتبقية

هذا الإصدار **أحادي الاتجاه** من Orax إلى Appwrite إلى Flutter. لا ينفذ الكتابة العكسية من Flutter إلى SQL Server، ولا يقرر تعارضات مالية بين مصادر متعددة. كما أن اختبار التشغيل النهائي للمثبت وSQL Server ودورة تسجيل الدخول والمزامنة يجب أن يتم على Windows فعلي، بينما يبقى Linux مناسباً للبناء والفحص الساكن واختبارات REST الخارجية.

## المراجع

[1]: https://appwrite.io/docs/quick-starts/dotnet "Appwrite .NET quick start"

[2]: https://appwrite.io/docs/references/cloud/server-rest/databases#list-documents "Appwrite Databases REST API"

[3]: https://appwrite.io/docs/queries "Appwrite query syntax and pagination"

[4]: https://appwrite.io/docs/partners/project/api-keys "Appwrite API keys and scopes"
