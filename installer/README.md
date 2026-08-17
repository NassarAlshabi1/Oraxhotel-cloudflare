# Orax Hotel Windows Installer — المُثبّت المتكامل

هذا المجلد يحتوي مصدر مُثبّت Windows **ذاتي التشغيل ومتكامل**. المُثبّت يثبت `HotelSys.exe` وكل الملحقات، ويهيّئ اتصال SQL Server، ويستعيد قاعدة `Hotel_alkheer` من النسخة الاحتياطية المضمّنة، ويكتب `appsettings.json` بالكامل، ثم يُشغّل التطبيق ويفتح المتصفح.

## 📋 المزايا

- ✅ **تثبيت صامت كامل** — لا يطلب أي إدخال من المستخدم
- ✅ **إعدادات SQL Server مُسبقة** — كل الأسرار في `installer-config.json`
- ✅ **استعادة قاعدة البيانات** — من ملف `.bak` المضمّن تلقائياً
- ✅ **كتابة `appsettings.json` بالكامل** — كل connection strings المطلوبة
- ✅ **إنشاء حساب مشرف افتراضي** — تلقائياً داخل قاعدة البيانات
- ✅ **اختصار سطح المكتب** — تلقائياً بعد التثبيت
- ✅ **فتح منفذ 5080 في جدار الحماية** — تلقائياً
- ✅ **تشغيل التطبيق** — تلقائياً وفتح المتصفح على `http://localhost:5080`
- ✅ **وضع تفاعلي اختياري** — عبر `--interactive`
- ✅ **سجل تفصيلي** — عبر `--verbose`

## 📁 محتويات المجلد

| الملف | الوصف |
|---|---|
| `Program.cs` | كود المُثبّت الرئيسي (C# / .NET 8) |
| `Installer.csproj` | ملف المشروع (يضمّن `installer-config.json` و`payload.7z` و`7zr.exe`) |
| `installer-config.json` | **كل الأسرار والإعدادات** — اقرأه قبل النشر |
| `Build-Installer.ps1` | سكربت بناء الحزمة الكامل (يبني التطبيق + الحمولة + المُثبّت) |
| `README.md` | هذا الملف |
| `payload.7z` | الحمولة المضغوطة (يُنتجها `Build-Installer.ps1`) — غير مضمّن في Git |
| `7zr.exe` | أداة 7-Zip للاستخراج (تُنزّل تلقائياً عند البناء) |

## 🔧 متطلبات البناء

- **Windows 10/11 x64** (أو Ubuntu 22.04+ مع `dotnet SDK 8` للبناء المتقاطع)
- `dotnet SDK 8.0` أو أحدث
- DevExpress NuGet feed صالح (للحصول على مكتبات DevExpress اللازمة لـ HotelSys)
- حوالي 1 GB مساحة حرة

## 🏗️ بناء المُثبّت

```powershell
# على Windows PowerShell 5.1+ أو PowerShell 7+
cd installer
.\Build-Installer.ps1
```

النتيجة في `installer\build-output\installer\OraxHotel-Setup.exe`.

### خيارات البناء

```powershell
# تخطي بناء التطبيق (إن كان جاهزاً)
.\Build-Installer.ps1 -SkipAppBuild

# تخطي بناء المُثبّت (للاختبار فقط)
.\Build-Installer.ps1 -SkipInstallerBuild

# وضع Verbose
.\Build-Installer.ps1 -Verbose
```

## 🚀 استخدام المُثبّت

### الوضع الصامت (الافتراضي)

```cmd
OraxHotel-Setup.exe
```

سيتولّى المُثبّت كل شيء تلقائياً. يُفضّل تشغيل كمسؤول (Run as administrator).

### الوضع التفاعلي

```cmd
OraxHotel-Setup.exe --interactive
```

يطلب من المستخدم إدخال اسم الخادم وكلمة المرور عند فشل الاتصال الافتراضي.

### الوضع التفصيلي (verbose)

```cmd
OraxHotel-Setup.exe --verbose
```

يطبع كل خطوة على الشاشة للتشخيص.

## ⚙️ تعديل الإعدادات

كل الأسرار في `installer-config.json`. عدّلها قبل البناء لتغيير:

- خادم SQL Server الافتراضي (الحالي: `.\SQLEXPRESS`)
- حساب SQL المستخدم (الحالي: `sa` / `orax055266`)
- قاعدة البيانات (الحالية: `Hotel_alkheer`)
- حساب المشرف الافتراضي (الحالي: `admin` / `Admin@2024!`)
- عنوان الاستماع (الحالي: `http://localhost:5080`)
- رابط خادم الإنتاج البعيد (`95.216.218.251,12356`)

## ⚠️ تنبيه أمني

ملف `installer-config.json` يحوي كلمات مرور فعلية. قبل النشر:

1. **لا ترفع `installer-config.json` إلى مستودع عام**. (.gitignore الحالي يستثنيه)
2. إذا نشرت `OraxHotel-Setup.exe` علناً، فكلمات المرور بداخله قابلة للاستخراج. استخدم حزمة خاصة لكل عميل.
3. غيّر كلمة مرور `sa` على خوادم الإنتاج قبل أي نشر علني.
4. استخدم tokens GitHub ذات scope محدود لرفع الحزمة.

## 📝 ملاحظات تقنية

- المُثبّت يبني كـ `Self-contained` (`PublishSingleFile=true`) — لا يحتاج .NET Runtime مُثبّتاً على جهاز الهدف.
- حجم الحزمة النهائي حوالي 150–250 MB (يشمل .NET Runtime + DevExpress + wwwroot + قاعدة البيانات).
- الخدمات المطلوبة على جهاز الهدف: **SQL Server 2019+ أو SQL Server Express 2019+**.
