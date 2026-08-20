# Orax Hotel Windows Installer

يحتوي هذا المجلد على مصدر مُثبّت Windows ذاتي التشغيل لنظام Orax Hotel. النسخة النهائية من المُثبّت تُضمّن برنامج `HotelSys.exe`، وملف النسخة الاحتياطية، وملف SQL البديل، ووسيط **SQL Server 2022 Express Core x64** داخل ملف EXE واحد. عند تشغيل الملف على Windows سيظهر طلب UAC لأن تثبيت SQL Server ينشئ خدمة Windows ويحتاج صلاحيات Administrator.

## ما ينفذه المُثبّت

يفحص المُثبّت أولاً إمكانية الاتصال بالنسخة المحلية ` .\SQLEXPRESS `. إذا كانت الخدمة موجودة ويُمكن الوصول إليها، يُبقيها دون إعادة تثبيت. وإذا لم تكن موجودة، يشغّل وسيط SQL Server Express المضمّن في الوضع الصامت ويثبت محرك `SQLEXPRESS` مع تفعيل TCP وحساب Windows الذي شغّل المُثبّت كمسؤول SQL.

بعد جاهزية المحرك، يستخرج payload التطبيق، ثم ينشئ أو يستعيد قاعدة `Hotel_alkheer` من ملف `.bak` المضمّن باستخدام `RESTORE FILELISTONLY` و`RESTORE DATABASE ... WITH MOVE`. إذا تعذر وجود النسخة الاحتياطية، يوجد ملف `Hotel_alkheer_init.sql` لإنشاء المخطط والبيانات العامة؛ هذا البديل لا ينشئ حساباً أو كلمة مرور جديدة. إذا كانت قاعدة `Hotel_alkheer` موجودة مسبقاً، يحافظ المُثبّت عليها ولا يستبدلها.

لا ينشئ المُثبّت حساب `admin` جديداً ولا يضع كلمة مرور افتراضية. بعد الاستعادة، تتحقق شاشة الدخول من `PasswordHash` في `dbo.AspNetUsers`، مع توافق احتياطي للحسابات القديمة الموجودة في `dbo.admin_table`. لذلك يجب استخدام بيانات المشرف التي يعرفها مالك النظام، ولا تُكتب كلمات المرور في المستودع أو ملف الإعدادات.

## بناء النسخة النهائية

يتطلب البناء Windows x64 أو بيئة بناء قادرة على نشر `win-x64`، و.NET 8 SDK، ووسيط Microsoft الكامل `SQLEXPR_x64_ENU.exe`. لا يُخزّن الوسيط الكبير في Git؛ يُنزّل من Microsoft ويُتحقق من بصمته بواسطة `fetch-sql-express.sh` قبل `dotnet publish`.

رابط Microsoft الرسمي: <https://www.microsoft.com/en-us/download/details.aspx?id=104781>

```text
SHA-256 للوسيط المستخدم في الإصدار الحالي:
bea033e778048748eb1c87bf57597f7f5449b6a15bac55ddc08263c57f7a1ca8
الحجم: 261082544 bytes
```

من جذر المستودع، حضّر الوسيط ثم انشر المُثبّت بالأوامر التالية:

```bash
./installer/fetch-sql-express.sh
```

بعد نجاح التحقق، يمكن نشر المُثبّت بالأمر التالي:

```powershell
dotnet restore Installer.csproj -r win-x64
dotnet publish Installer.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None
```

يحافظ المُثبّت على أقسام `appsettings.json` الموجودة في payload، بما فيها قسم `Appwrite`، ويحدّث `ConnectionStrings` فقط إلى اتصال Windows المحلي بعد التهيئة. النسخة المضمنة لا تحتوي كلمة مرور SQL، ولا تعرض النسخة الشاملة شاشة SQL Authentication أو خادم خارجي؛ ذلك يتطلب إصداراً مخصصاً. لا تنشر ملف `appsettings.json` إذا أضيفت إليه لاحقاً أسرار أو كلمات مرور.
