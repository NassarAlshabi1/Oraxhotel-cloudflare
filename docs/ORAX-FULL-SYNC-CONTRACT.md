# عقد المزامنة الكامل بين Orax وFlutter عبر Appwrite

## القرار المعماري

يبقى Orax وSQL Server مصدر الحقيقة للسجلات التي ينشئها المكتب، بينما يعمل Appwrite Cloud كطبقة نقل ومزامنة يقرأ منها Flutter. لا يتصل الهاتف بقاعدة SQL Server مباشرة. الكتابة العكسية من Flutter إلى Orax ليست جزءاً من هذه المرحلة ما لم تُنفذ عبر API أعمال صريح داخل Orax؛ لا تُكتب جداول Orax مباشرة من الهاتف.

## الهوية

تستخدم المستندات التي ينشئها Orax معرفات ثابتة لا تتغير بين دورات المزامنة:

| الكيان | مصدر الهوية | Appwrite document ID | server field |
|---|---|---|---|
| الغرفة | `RoomsTable.Id` | `orax-room-{id}` أو المستند المطابق الحالي | `serverId` |
| الحجز | `RecetionTable.Id` | `orax-booking-{id}` أو المستند server legacy المطابق | `serverBookingId` |
| معلومات النزيل | `CustomerTable.Id` عبر `MyCustomer` | `orax-guest-{id}` أو المستند server legacy المطابق | `serverId` |
| الدفعة/الفاتورة المدفوعة | `BillsTable.Id` | `orax-payment-{id}` أو المستند server legacy المطابق | `serverPaymentId` |

يُنشأ `localUuid` ثابت لكل مستند Orax بواسطة UUID حتمي مشتق من نوع الكيان ورقم السجل. لا يُستخدم `bookingLocalId` أو أي auto-increment محلي من جهاز Flutter كرابط بين الأجهزة. عند قراءة Appwrite، تُقرأ خصائص المستند top-level عبر `JsonExtensionData`؛ لا يُفترض وجود كائن `data` متداخل في استجابة REST.

## الحجوزات

يُحوّل `RecetionTable` إلى collection `bookings`. الغرفة تأتي من `IdRoom` عبر `RoomsTable.NameR`. النزيل يأتي من `IdMyCustomer` عبر `MyCustomer.IdCustomer` ثم `CustomerTable`. حالة Orax المثبتة هي `1` للحجز، `2` بعد تسجيل الدخول، و`3` بعد تسجيل الخروج؛ القيم المنشورة إلى Flutter هي `مؤقت`، `checked_in`، و`checked_out` على الترتيب. تواريخ الحجز هي `StartDate` و`EndDate`، وتواريخ الدخول والخروج الفعلية هي `CheckinDate` و`ChechoutDate`.

## معلومات النزلاء

يُحوّل سجل `CustomerTable` المرتبط بـ `MyCustomer` إلى collection `guest_infos`. هذا الكيان في Flutter مسطح ولا يحمل booking foreign key، ولذلك يُزامن كسجل هوية مستقل. الحقول الأساسية هي الاسم والجنسية ونوع ورقم الإثبات وتاريخ ومكان الإصدار ورقم الغرفة عند وجود حجز نشط. عند غياب `serverId` في مستند server قديم، تكون المطابقة الآمنة على `guestName + idNumber + nationality` بشرط وجود رقم الإثبات؛ التكرار يؤدي إلى تعارض، ومستندات `origin=local` لا تُستبدل.

## المدفوعات

لا يُحوّل كل `BillsTable` إلى دفعة تلقائياً. العقد الحالي يزامن **الفواتير المرتبطة بحجز والتي لها `PayAmount > 0`** إلى collection `payments`. `PayAmount` هو amount، و`Date` هو paymentDate، و`TypePay` هو paymentMethod، و`NumReference` هو referenceNumber، و`Note` هو notes، و`IdReception` هو serverBookingId. `RestAmount > 0` ينتج `isPendingBalance=true`. الفواتير التي لا تحتوي دفعة فعلية لا تُرسل إلى payments حتى لا تظهر إيرادات وهمية. عند غياب `serverPaymentId` في مستند server قديم، تكون المطابقة على `roomNumber + paymentDate` حتى مستوى الثانية + `amount`، مع رفض التكرار. لا يدخل `paymentMethod` في الهوية لأنه قد يختلف نصياً بين Orax وFlutter. يلزم مسار مستقل لاحقاً إذا أثبتت قاعدة البيانات أن سندات `BondTable` تمثل دفعات لا تنعكس في `BillsTable`.

## pagination والتعارضات

تستخدم خدمة Orax صيغة Appwrite JSON queries الرسمية، مثل `{"method":"limit","values":[100]}` و`{"method":"offset","values":[0]}`، وتتحقق من أن عدد المستندات المجمّع يساوي `total`. إذا وُجد تكرار في هوية موثقة أو fallback، لا تختار الخدمة مستنداً عشوائياً بل تسجل تعارضاً وتتخطى السجل. يفرض `AppwriteSyncCoordinator` قفلاً مستقلاً لكل كيان لمنع تداخل endpoint اليدوي مع HostedService؛ وتعيد نقاط الإدارة HTTP 409 عند وجود دورة لنفس الكيان. وتبقى عمليات Orax أحادية الاتجاه في هذه المرحلة؛ فلا يجوز لـ Flutter الكتابة فوق سجلات Orax المحاسبية دون API أعمال.

## مراجع

[1]: https://appwrite.io/docs/apis/rest "Appwrite REST API: headers, arrays, and JSON query strings"

[2]: https://appwrite.io/docs/queries "Appwrite Query operators and pagination"
