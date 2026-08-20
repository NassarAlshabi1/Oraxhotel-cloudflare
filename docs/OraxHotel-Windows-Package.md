# حزمة Orax Hotel Windows — الإصدار الشامل

تتضمن الحزمة النهائية ملف `OraxHotel-Setup.exe` self-contained لتثبيت Orax Hotel وSQL Server 2022 Express Core x64 وقاعدة `Hotel_alkheer` وملفات الواجهة والملحقات. يُنشر الملف الكبير كأصل GitHub Release داخل المستودع الخاص، وليس كملف ثنائي عادي داخل Git.

## المكونات

| المكوّن | الوظيفة |
|---|---|
| `OraxHotel-Setup.exe` | تثبيت التطبيق، تثبيت SQL Server Express عند الحاجة، واستعادة القاعدة |
| `HotelSys.exe` | التطبيق المنشور لنظام Windows x64 |
| `database/Hotel_alkheer20232009552241.bak` | النسخة الاحتياطية التي تحفظ بيانات النظام والحسابات الأصلية |
| `database/Hotel_alkheer_init.sql` | مخطط بديل من 47 جدولاً وبيانات عامة غير حساسة، دون حساب مشرف |
| `wwwroot` وملفات Data | الواجهة والموارد المحلية والتقارير |

## سلوك التثبيت

يطلب Windows صلاحية Administrator لأن تثبيت SQL Server Express ينشئ خدمة Windows. يستخدم المُثبّت النسخة المحلية ` .\SQLEXPRESS `، ويعيد استخدامها إذا كانت موجودة وقابلة للاتصال. وإذا لم تكن موجودة، يثبت الوسيط المضمّن محلياً دون الحاجة إلى تنزيل SQL Server مسبقاً.

بعد تشغيل المحرك، إذا لم تكن قاعدة `Hotel_alkheer` موجودة يستعيد المُثبّت ملف `.bak` مع نقل ملفات البيانات والسجل إلى مسارات SQL Server الجديدة. لا يستبدل قاعدة قائمة. وإذا غاب ملف `.bak`، يمكن استخدام ملف SQL البديل، مع العلم أنه لا ينشئ مستخدمًا أو كلمة مرور.

## تسجيل الدخول

لا ينشئ المُثبّت حساب `admin` جديداً ولا يضع كلمة مرور عامة. تتحقق شاشة الدخول من `PasswordHash` في `dbo.AspNetUsers`، مع توافق احتياطي للحسابات القديمة في `dbo.admin_table`. يجب استخدام اسم المستخدم وكلمة المرور الأصليين اللذين يعرفهما مالك النظام.

> لا توجد كلمات مرور افتراضية منشورة في هذه الوثيقة أو داخل المستودع. لا تُرسل كلمة مرور المشرف أو SQL Server إلى GitHub، ولا تُضمّنها في `appsettings.json`.

## خطوات الاستخدام

استخرج الحزمة ثم شغّل `OraxHotel-Setup.exe` كمسؤول. بعد اكتمال التثبيت افتح اختصار **Orax Hotel**، وسيبدأ الخادم المحلي على `http://localhost:5080`. إذا فشلت استعادة النسخة الاحتياطية، لا تتابع باستخدام قاعدة بديلة قبل فحص سجل SQL والتأكد من أن النسخة تخص النظام الصحيح.

## التحقق من الإصدار

لإنشاء بصمة للملف الذي تم تنزيله استخدم:

```bash
sha256sum OraxHotel-Setup.exe
```

إصدار التسليم v1.3.0 المبني من commit اجتاز Windows acceptance بعد إصلاحات المثبت:

| الملف | الحجم | SHA-256 |
|---|---:|---|
| `OraxHotel-Setup.exe` | 422333380 bytes | `5750f1b18615bec51352549596f2adbecba85122403bdf9afbabe45b0c1d2dca` |

يجب مقارنة الناتج مع بصمة أصل GitHub Release المنشورة من مالك المستودع. لا تعتمد على بصمة قديمة إذا تغير ملف EXE أو إصدار الحزمة.

## المتطلبات

يتطلب التشغيل Windows 10 أو Windows 11 x64، وصلاحية Administrator أثناء التثبيت، ومساحة كافية لاستخراج التطبيق ووسيط SQL Server Express. لا يلزم تثبيت SQL Server مسبقاً. تم إثبات التثبيت التنفيذي على runner فعلي بنظام `windows-2022` في GitHub Actions، مع بقاء اختبار UAC وتسجيل الدخول اليدوي اختيارياً على جهاز مستخدم.

## الدعم والمصدر

رابط الإصدار الشامل الحالي: <https://github.com/Nassaralshabi/oraxhotel2024/releases/tag/windows-installer-v1.3.0>

المستودع الخاص: <https://github.com/Nassaralshabi/oraxhotel2024>

الفرع الذي يحتوي آخر إصلاحات التكامل والمثبت: `feature/orax-flutter-appwrite-integration`

صفحة Microsoft الرسمية لوسيط SQL Server Express: <https://www.microsoft.com/en-us/download/details.aspx?id=104781>
