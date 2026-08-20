# تقرير دراسة وجاهية وتسليم Orax Hotel v1.3.0

**المؤلف:** Manus AI
**التاريخ:** 20 أغسطس 2026
**المستودع:** `Nassaralshabi/oraxhotel2024`
**الفرع الحالي:** `feature/orax-flutter-appwrite-integration`
**commit التكامل المراجع:** `fceb112841d1a412efbe7cc0b86a67f460cd0c8a`

## الخلاصة التنفيذية

أُجريت مراجعة عميقة لمسار النظام من المصدر إلى الإصدار القابل للتثبيت، شملت تطبيق ASP.NET Core، مثبت Windows، SQL Server Express، قاعدة `Hotel_alkheer`، تكامل Orax مع Appwrite، وعقود Flutter المحلية. تم اكتشاف وإصلاح عيوب فعلية في المثبت وفي تصنيف حالات الحجوزات داخل Flutter، ثم إعادة بناء الحزمة ورفعها كإصدار GitHub مستقل.

> الحالة الحالية: **نجح اختبار القبول التنفيذي على Windows عبر GitHub Actions**. بُني المثبت من commit المصدر الحالي، وثُبّت SQL Server Express، وشُغّل المثبت، واستُعيدت قاعدة `Hotel_alkheer`، ووصل التطبيق إلى endpoint الصحة. يبقى اختبار Flutter الفعلي على جهاز Android أو iOS خارج بيئة CI قبل اعتبار تطبيق الهاتف معتمداً ميدانياً.

## مكونات التسليم

| المكوّن | الحالة | الدليل أو الرابط |
|---|---|---|
| Orax Hotel ASP.NET Core self-contained | مبني بنجاح لـ `win-x64` | `0 Error(s)` في البناء النهائي، مع 289 تحذيراً قائماً خارج التكامل |
| مثبت Windows شامل | سيُنشر في v1.3.0 | [`OraxHotel-Setup.exe`](https://github.com/Nassaralshabi/oraxhotel2024/releases/download/windows-installer-v1.3.0/OraxHotel-Setup.exe) |
| حزمة Windows مع الوثائق | ستُنشر في v1.3.0 | [`OraxHotel-Windows-Package-v1.3.0.zip`](https://github.com/Nassaralshabi/oraxhotel2024/releases/download/windows-installer-v1.3.0/OraxHotel-Windows-Package-v1.3.0.zip) |
| ملف البصمة | سيُنشر في v1.3.0 | [`SHA256SUMS.txt`](https://github.com/Nassaralshabi/oraxhotel2024/releases/download/windows-installer-v1.3.0/SHA256SUMS.txt) |
| GitHub Release | سيُنشر بعد بناء الحزمة الحالية | [`windows-installer-v1.3.0`](https://github.com/Nassaralshabi/oraxhotel2024/releases/tag/windows-installer-v1.3.0) |
| فرع التكامل | منشور ونظيف | [`feature/orax-flutter-appwrite-integration`](https://github.com/Nassaralshabi/oraxhotel2024/tree/feature/orax-flutter-appwrite-integration) |

## الإصلاحات الجوهرية المنفذة

### مثبت Windows

كان `WriteAppSettings` يعيد إنشاء ملف `appsettings.json` من أقسام قليلة، مما كان يؤدي إلى فقد قسم `Appwrite` وأي إعدادات مخصصة بعد التثبيت. تم تغيير السلوك إلى قراءة JSON الموجود في payload، والحفاظ على الأقسام الحالية، وتحديث `ConnectionStrings` فقط.

كان مشروع المثبت ينتج اسماً عاماً هو `Installer.exe` رغم أن السكربت والوثائق يتطلبان `OraxHotel-Setup.exe`. تمت إضافة `AssemblyName` صريح، وأصبح الملف الناتج يحمل الاسم المطلوب فعلياً.

كان ضغط payload في سكربت PowerShell يضيف المسار المحلي الكامل إلى الأرشيف. وبما أن `Program.cs` يبحث عن `extractionDir/payload/HotelSys.exe`، تم تصحيح السكربت ليجعل `payload` هو الجذر داخل الأرشيف. تم التحقق باستخراج الأرشيف فعلياً ووجود `payload/HotelSys.exe` و`payload/appsettings.json` وملفات قاعدة البيانات و`payload/wwwroot`. كما أزيل `InvariantGlobalization=true` من مشروع المثبت، لأن `Microsoft.Data.SqlClient` يحتاج globalization غير invariant عند فتح اتصال SQL في التطبيق self-contained.

### تكامل Appwrite

تتضمن الحزمة الخادمية مزامنة الغرف والحجوزات والنزلاء والمدفوعات من Orax إلى Appwrite. تستخدم الخدمات هويات حتمية، fallback مضبوطاً للمستندات القديمة، حماية للمستندات المحلية، ورفضاً للتعارضات غير القابلة للحسم بدلاً من اختيار مستند عشوائي.

يمنع `AppwriteSyncCoordinator` تشغيل مزامنتين لنفس الكيان في العملية نفسها، ويعيد endpoint الإداري HTTP 409 عند وجود مزامنة قائمة. كما تم إصلاح قراءة خصائص Appwrite التي تعود في المستوى الأعلى للمستند، وإضافة fallback للبنية المتداخلة القديمة.

### Flutter

أظهرت المراجعة أن Orax ينشر `checked_in` و`checked_out` و`cancelled`، بينما كانت أدوات Flutter المركزية لا تعتبر `checked_in` حالة نشطة. تمت إضافة `checked_in` و`checked-in` إلى حالات النشاط وإشغال الغرفة، مع إبقاء `checked_out` و`cancelled` خارج الحالات النشطة. تم تنفيذ تحقق ساكن على الملف، لكن لم يُنفذ `dart analyze` لأن Flutter وDart SDK غير مثبتين في بيئة Linux الحالية.

## نتائج الاختبارات

| الاختبار | النتيجة | حدود النتيجة |
|---|---|---|
| `dotnet build HotelSys.csproj --runtime win-x64` | ناجح، `0 Error(s)` و`289 Warning(s)` | التحذيرات قديمة وخارج تكامل Appwrite؛ يلزم مراجعة مستقلة لاحقاً |
| نشر المثبت self-contained | ناجح | تم التحقق من إنتاج `OraxHotel-Setup.exe` |
| نوع EXE | `PE32+ executable x86-64` | فحص ساكن على Linux، وليس تشغيل Windows |
| بصمة EXE | `86903f62a563aab66caa636df8e771ef64c0d9f5a00c58927799f176da7dac67` | الحجم `425941254` bytes |
| payload archive | ناجح | `payload` هو الجذر الصحيح، وعدد الملفات المستخرجة 1234 |
| appsettings preservation probe | ناجح | بقي Appwrite والأقسام المخصصة، وتحدثت ConnectionStrings فقط |
| Appwrite collections | نجح سابقاً عبر REST | `rooms=20`, `bookings=200`, `payments=1125`, `guest_infos=119` |
| Appwrite create/delete probes | ناجح سابقاً | HTTP 201 ثم HTTP 204، ولم تبق UUIDs تجريبية |
| pagination | ناجح سابقاً | 200 booking عبر صفحتين 100+100 مع التحقق من `total` |
| coordinator probe | ناجح | منع نفس الكيان، سمح بكيان مختلف، وأعاد الدخول بعد التحرير |
| Flutter status static checks | ناجح | تم التأكد من وجود الحالات المطلوبة وعدم إدخال صياغة Dart مكسورة |
| `flutter analyze` وAPK build | غير منفذ | Flutter/Dart SDK غير مثبتين في sandbox الحالية |
| Windows acceptance run `32413645023` على `windows-2022` | ناجح بالكامل | build من المصدر، PE check، تثبيت SQL Express، تشغيل المثبت، التحقق من خدمة SQL وقاعدة `Hotel_alkheer`، والوصول إلى `/api/appwrite/health` |
| تشغيل EXE وتثبيت SQL Server | ناجح في CI | Windows runner فعلي؛ جميع خطوات workflow نجحت، مع بقاء تسجيل الدخول اليدوي واختبار UAC على جهاز مستخدم خارج CI |

## تعليمات التسليم على Windows

يُحمّل المستخدم `OraxHotel-Setup.exe` من إصدار GitHub، ثم يقارن SHA-256 مع `SHA256SUMS.txt`. يجب تشغيل الملف بصلاحية Administrator. يستخدم المثبت `.\SQLEXPRESS` محلياً وWindows Integrated Security؛ ولا يعرض الإصدار الشامل شاشة لاختيار خادم خارجي أو SQL Authentication.

إذا لم تكن خدمة `SQLEXPRESS` موجودة أو قابلة للاتصال، يثبت المثبت وسيط SQL Server Express المضمّن. بعد ذلك يستعيد `Hotel_alkheer` من النسخة الاحتياطية إذا لم تكن القاعدة موجودة، ويحافظ على قاعدة موجودة دون استبدالها. ينسخ التطبيق إلى `%LOCALAPPDATA%\OraxHotel` وينشئ اختصاراً على سطح المكتب يفتح `http://localhost:5080`.

بعد ظهور شاشة الدخول، يستخدم صاحب النظام حساب المشرف الموجود داخل النسخة الاحتياطية. لا ينشئ المثبت حساب `admin` جديداً ولا يغير PasswordHash.

## الاختبارات الميدانية المتبقية

أُنجز اختبار Windows الآلي على runner فعلي بنظام `windows-2022` في الدورة `32413645023`. شمل الاختبار build المثبت من المصدر، فحص PE، تثبيت SQL Server Express، تشغيل `OraxHotel-Setup.exe`، التحقق من خدمة `MSSQL$SQLEXPRESS` وقاعدة `Hotel_alkheer`، وتشغيل التطبيق والوصول إلى `/api/appwrite/health`. يبقى اختبار قبول يدوي اختياري على جهاز مستخدم للتحقق من UAC وتسجيل الدخول وفتح الوحدات الأساسية؛ هذه الخطوات لا يمكن إثباتها آلياً من دون جلسة مستخدم تفاعلية.

يجب تنفيذ اختبار Flutter في بيئة تحتوي Flutter SDK، عبر `flutter pub get` و`flutter analyze` و`flutter test` ثم بناء APK أو IPA تجريبي. بعد تثبيت التطبيق، يجب التحقق من السحب من Appwrite، ظهور حالة `checked_in`، عدم ظهور الحجوزات `checked_out` أو `cancelled` كحجوزات نشطة، عمل outbox دون فقد، وعدم إنشاء مستندات مكررة.

## ملاحظة معمارية مهمة

توضح وثائق Appwrite الرسمية أن API keys مخصصة لتكاملات الخادم وأن المفتاح سر لا ينبغي وضعه في تطبيق العميل [1]. لذلك ينبغي قبل توزيع تطبيق الهاتف على مستخدمين خارجيين إعادة تصميم اعتماد Flutter ليستخدم جلسات Appwrite أو وسيط API محدود الصلاحيات بدلاً من تضمين مفتاح خادمي في binary الهاتف. لم أغيّر هذا المسار تلقائياً لأن تغيير المصادقة قد يبدل صلاحيات collections ويحتاج قراراً تشغيلياً واختباراً مستقلاً.

كما توضح وثائق Appwrite أن offset pagination قد تنتج تكرارات أو فقداً إذا تغيرت البيانات أثناء القراءة، وأن cursor pagination أفضل للبيانات كثيرة التغير [2]. يستخدم العقد الحالي limit/offset لجمع seed محدود تم التحقق منه، ويجب الانتقال إلى cursor pagination إذا أصبحت collections كبيرة أو دورات المزامنة متزامنة مع تغييرات كثيفة.

## ملفات الوثائق

| الملف | المحتوى |
|---|---|
| `docs/OraxHotel-Installation-Guide.md` | خطوات التثبيت والتشغيل وتسجيل الدخول واستعادة القاعدة |
| `docs/OraxHotel-Windows-Package.md` | ملخص الحزمة والبصمة وروابط الإصدار |
| `docs/ORAX-FLUTTER-APPWRITE-INTEGRATION.md` | عقد التكامل ونتائج التدقيق والاختبارات |
| `docs/ORAX-FULL-SYNC-CONTRACT.md` | مصادر الحقيقة والهوية والتعارضات وحدود الكتابة |
| `docs/RESEARCH-APPWRITE-REST-2026-08-20.md` | نتائج المصادر الرسمية الخارجية |

## سجل اختبار Windows القابل للتدقيق

| البند | القيمة |
|---|---|
| Workflow | [`windows-installer-acceptance.yml`](https://github.com/Nassaralshabi/oraxhotel2024/blob/feature/orax-flutter-appwrite-integration/.github/workflows/windows-installer-acceptance.yml) |
| Run ID | [`32413645023`](https://github.com/Nassaralshabi/oraxhotel2024/actions/runs/32413645023) |
| النتيجة | `success` |
| Runner | `windows-2022` |
| Source commit | `fceb112841d1a412efbe7cc0b86a67f460cd0c8a` |
| الخطوات الناجحة | Build، PE check، SQL prerequisite، installer، database verification، health endpoint، diagnostics upload |

## المراجع

[1]: https://appwrite.io/docs/apis/rest "Appwrite REST API"
[2]: https://appwrite.io/docs/products/databases/pagination "Appwrite Databases Pagination"
[3]: https://appwrite.io/docs/products/databases/legacy/documents "Appwrite Legacy Documents"
[4]: https://github.com/Nassaralshabi/oraxhotel2024/releases/tag/windows-installer-v1.2.0 "Orax Hotel Windows Installer v1.2.0"
