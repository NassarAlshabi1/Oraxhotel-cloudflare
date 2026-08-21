# Oraxhotel Production Release Runbook

## نطاق الإصدار

المستودع هو `NassarAlshabi1/Oraxhotel-cloudflare`، والفرع المرشح للإصدار هو `feature/orax-flutter-appwrite-integration`. مصدر الحقيقة لبيانات الفندق هو SQL Server `Hotel_alkheer`، بينما Cloudflare D1 يحتفظ بمرآة الجداول وبنية المزامنة والإسقاطات.

## بوابة ما قبل الإصدار

يجب أن تكون شجرة Git نظيفة، وأن يطابق commit المحلي commit البعيد. يجب تشغيل `npm ci --ignore-scripts` و`npm run typecheck` و`npx wrangler deploy --dry-run` داخل `mobile/worker`. يجب تطبيق ترحيلات D1 على بيئة اختبار أولاً والتحقق من عدد الجداول والحقول والعلاقات قبل الإنتاج.

لا تُوضع كلمات المرور أو مفاتيح Cloudflare أو Appwrite أو مفاتيح توقيع Android داخل الكود أو سجل CI. تُمرر الأسرار إلى Worker عبر Cloudflare Secrets، وإلى Flutter عبر آلية إصدار آمنة، ولا تعتمد نسخة الإنتاج على قيم افتراضية حساسة.

## Worker وD1

يُنشر Worker بعد اجتياز dry-run وفحوص TypeScript. تُطبق الترحيلات عبر `npx wrangler d1 migrations apply marina-hotel-db --remote`. يجب الاحتفاظ بنسخة تصدير/نسخة احتياطية من D1 قبل أي ترحيل بنيوي، ثم تنفيذ smoke test لمسارات login وsync وdesktop commands.

## Android

من داخل `mobile/mobile` يجب تشغيل:

```bash
flutter pub get
dart run build_runner build --delete-conflicting-outputs
dart analyze --fatal-infos .
dart format --set-exit-if-changed .
flutter build apk --release --split-per-abi --obfuscate --split-debug-info=./debug_symbols
flutter build appbundle --release --obfuscate --split-debug-info=./debug_symbols
```

يجب توقيع الإصدار بمفتاح release محفوظ خارج المستودع أو في Secret Manager. لا يُنشر App Bundle قبل التحقق من `applicationId` وversionCode وversionName وسياسة الخصوصية.

## Windows

يُبنى المثبّت على Windows عبر `installer/Build-Installer.ps1` أو GitHub Actions. يجب اختبار تثبيت SQL Server Express، إنشاء/اكتشاف `Hotel_alkheer`، تشغيل `HotelSys.exe`، والوصول إلى endpoint الصحة. لا يمكن بناء هذا المثبّت محلياً على Linux من دون .NET SDK وبيئة Windows.

## فجوة التكامل التي تمنع إعلان اكتمال المسار المركزي

التدقيق الحالي يثبت أن `HotelSys` يحتوي على خدمات Appwrite القديمة فقط، ولا يحتوي على عميل Cloudflare Worker أو ناشر أحداث إلى D1. في المقابل، يحتوي Flutter وWorker على مسار Cloudflare/desktop-first. لذلك فإن بناء المثبّت واختبار endpoint المحلي لا يثبت بعد أن كل تغيير يُسجّل في SQL Server سيصل إلى Worker/D1. يجب تنفيذ ناشر سطح مكتب مصادق، أو اعتماد وسيط مزامنة موثّق، قبل وصف النظام بأنه تكامل إنتاجي كامل بين SQL Server وFlutter عبر Cloudflare.

حتى تنفيذ هذه الفجوة، يكون قرار الإصدار الآمن هو تشغيل HotelSys محلياً مع SQL Server، وإبقاء Appwrite القديم معطلاً افتراضياً، وتشغيل مسار Flutter/Worker فقط وفق اختبارات المزامنة المتاحة. لا يجوز إعادة تفعيل Appwrite في Production إلا بعد تدوير المفتاح القديم ووضع مفتاح جديد خارج Git، ولا يجوز اعتبار ذلك بديلاً عن Cloudflare desktop publisher.

## متغيرات البيئة المقترحة لـ HotelSys

يُشغّل HotelSys في Windows مع `ASPNETCORE_ENVIRONMENT=Production`. تُمرّر سلسلة اتصال SQL Server عبر `ConnectionStrings__Hotel_alkheerContext`. إذا تقرر تشغيل Appwrite القديم مؤقتاً، تُمرّر قيمه عبر `Appwrite__Endpoint` و`Appwrite__ProjectId` و`Appwrite__DatabaseId` و`Appwrite__ApiKey` مع إبقاء `Appwrite__Enabled=true` فقط بعد اعتماد المفتاح الجديد. لا تُحفظ هذه القيم داخل installer أو المستودع.

## قرار النشر

نجاح البناء والاختبارات لا يعني نشر Worker أو إصدار تطبيق للجمهور. يلزم اعتماد منفصل للبيئة الحية، ونسخة D1 المستهدفة، وملف التوقيع، ونطاق المستخدمين. في حال فشل أي بوابة، يُوقف الإصدار ولا يُتجاوز الفشل بخيار force.
