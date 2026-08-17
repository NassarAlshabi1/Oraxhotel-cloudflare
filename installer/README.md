# Orax Hotel Windows Installer

هذا المجلد يحتوي مصدر مُثبّت Windows ذاتي التشغيل. المُثبّت يثبت `HotelSys.exe`، ويضمّن payload التطبيق، ثم يطلب إعداد SQL Server ويستعيد قاعدة `Hotel_alkheer` الموجودة في النسخة الاحتياطية دون إنشاء حساب `admin` جديد.

## ملفات البناء المطلوبة

يحتاج البناء إلى `dotnet SDK 8`، وملف `payload.7z` الذي يحتوي مجلد `payload`، وملف `7zr.exe` الرسمي داخل مشروع المُثبّت. لا تُضاف هذه الملفات الثنائية إلى Git إذا تجاوزت حدود التخزين أو احتوت على بيانات إنتاجية.

يجب أن يحتوي payload على:

```text
payload/HotelSys.exe
payload/appsettings.json
payload/database/Hotel_alkheer20232009552241.bak
payload/database/Hotel_alkheer_init.sql
payload/wwwroot/...
```

بعد توفير الموارد، يُبنى المُثبّت باستخدام:

```bash
dotnet restore Installer.csproj
dotnet publish Installer.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None
```

## سلوك المُثبّت

يطلب المُثبّت اسم خادم SQL Server ونوع المصادقة. إذا لم تكن قاعدة `Hotel_alkheer` موجودة، يستعيد ملف `.bak` المضمن. إذا كانت القاعدة موجودة، يحافظ عليها. لا ينشئ حساب `admin` ولا يكتب كلمة مرور تطبيق ثابتة؛ شاشة الدخول تستخدم حساب المشرف الموجود في قاعدة البيانات المستعادة.

## تنبيه أمني

لا تضع كلمات مرور SQL Server أو عناوين الخوادم البعيدة داخل ملفات المصدر. يكتب المُثبّت `appsettings.json` بعد إدخال الاتصال أثناء التثبيت. لا تستخدم `sa` للتشغيل اليومي، ولا تضع قاعدة بيانات إنتاجية أو نسخة احتياطية تحتوي بيانات شخصية داخل مستودع عام.
