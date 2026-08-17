# دليل تثبيت وتشغيل وإدارة Orax Hotel — المُثبّت المتكامل v1.0.0

**اسم المنتج:** Orax Hotel
**ملف التثبيت:** `OraxHotel-Setup.exe`
**نوع النظام:** تطبيق ويب محلي يعمل على Windows
**الإصدار:** 1.0.0
**عنوان التشغيل المحلي:** `http://localhost:5080`

---

## 1. نظرة عامة على النظام

Orax Hotel هو نظام إدارة فندق مبني على ASP.NET Core، يعمل كخادم محلي على جهاز Windows. بعد التشغيل يفتح المستخدم المتصفح للوصول إلى الواجهة، بينما تُحفظ بيانات الفندق والمستخدمين في قاعدة SQL Server باسم `Hotel_alkheer`.

يحتوي النظام على وحدات: الاستقبال، الحجوزات، الغرف، العملاء، الفواتير، السندات، الحسابات، الخدمات، التقارير، الإعدادات العامة، تقارير الصندوق والعملاء، إضافة إلى أدوات إنشاء QR.

> **ميّزة v1.0.0:** المُثبّت الجديد يقوم بكل شيء تلقائياً — لا حاجة لأي إدخال من المستخدم.

---

## 2. محتويات حزمة التثبيت

| العنصر | الوظيفة |
|---|---|
| `OraxHotel-Setup.exe` | المُثبّت المتكامل (self-contained .NET 8 binary) |
| `HotelSys.exe` | ملف التطبيق (مضمّن داخل الحمولة) |
| `wwwroot/` | ملفات CSS/JS والصور والخطوط والتقارير |
| `database/Hotel_alkheer20232009552241.bak` | نسخة SQL Server الاحتياطية |
| `database/Hotel_alkheer_init.sql` | ملف SQL بديل (إن لم تتوفر النسخة الاحتياطية) |
| `appsettings.json` | قالب أولي (يُكتب فوقه المُثبّت بكل الإعدادات) |
| `installer-config.json` | كل الأسرار والإعدادات (مضمّن داخل .exe) |

---

## 3. المتطلبات قبل التثبيت

### 3.1 نظام التشغيل
- Windows 10 (build 1809+) أو Windows 11
- معمارx64 (64-bit)
- 4 GB RAM كحد أدنى (8 GB مُوصى به)
- 2 GB مساحة حرة

### 3.2 SQL Server
- **SQL Server 2019+** أو **SQL Server Express 2019+**
- يجب أن تعمل خدمة SQL Server (MSSQLSERVER أو MSSQL$SQLEXPRESS)
- تنزيل Express مجاناً: https://www.microsoft.com/en-us/sql-server/sql-server-downloads

### 3.3 صلاحيات
- يُفضّل تشغيل المُثبّت كـ **Administrator** (لفتح منفذ جدار الحماية)
- حساب SQL المستخدم يحتاج صلاحية `sysadmin` أو `dbcreator` لاستعادة قاعدة البيانات

---

## 4. خطوات التثبيت

### 4.1 الطريقة السريعة (الوضع الصامت)

1. ثبّت SQL Server Express إن لم يكن مُثبّتاً.
2. شغّل `OraxHotel-Setup.exe` كـ Administrator.
3. انتظر 30 ثانية إلى 3 دقائق حسب أداء الجهاز.
4. سيفتح المتصفح تلقائياً على `http://localhost:5080`.
5. سجّل الدخول بـ:
   - اسم المستخدم: `admin`
   - كلمة المرور: `Admin@2024!`

### 4.2 الوضع التفاعلي (يدوي)

```cmd
OraxHotel-Setup.exe --interactive
```

يطلب من المستخدم:
- اسم خادم SQL Server (الافتراضي: `.\SQLEXPRESS`)
- نوع المصادقة (Windows / SQL)
- اسم مستخدم SQL (الافتراضي: `sa`)
- كلمة مرور SQL

### 4.3 الوضع التفصيلي

```cmd
OraxHotel-Setup.exe --verbose
```

يطبع كل خطوة على الشاشة — مفيد للتشخيص.

---

## 5. ماذا يفعل المُثبّت تلقائياً؟

عند تشغيل `OraxHotel-Setup.exe` (بدون أي وسائط):

| الخطوة | الإجراء التلقائي |
|---|---|
| 1 | قراءة `installer-config.json` المضمّن |
| 2 | فحص خدمة SQL Server (MSSQL$SQLEXPRESS أو MSSQLSERVER) |
| 3 | محاولة تشغيل الخدمة إن كانت متوقفة |
| 4 | اختبار اتصال بحساب `sa` / `orax055266` |
| 5 | استخراج أداة 7z المضمّنة |
| 6 | استخراج حمولة التطبيق `payload.7z` |
| 7 | نسخ ملفات التطبيق إلى `%LOCALAPPDATA%\OraxHotel` |
| 8 | نسخ ملف `.bak` إلى `%ProgramData%\OraxHotel\Database` |
| 9 | منح SQL Server صلاحية قراءة على المجلد (icacls) |
| 10 | التحقق من وجود قاعدة `Hotel_alkheer` |
| 11 | استعادة القاعدة من النسخة الاحتياطية (إن لم توجد) |
| 12 | إنشاء حساب مشرف `admin` / `Admin@2024!` (إن لم يوجد) |
| 13 | كتابة `appsettings.json` بكل connection strings المطلوبة |
| 14 | إنشاء ملف `start-oraxhotel.cmd` |
| 15 | إنشاء ملف `uninstall-oraxhotel.cmd` |
| 16 | إنشاء اختصار على سطح المكتب |
| 17 | فتح منفذ 5080 في جدار حماية Windows |
| 18 | تشغيل التطبيق (`HotelSys.exe`) |
| 19 | فتح المتصفح على `http://localhost:5080` |

---

## 6. الإعدادات الافتراضية (قابلة للتعديل)

كل القيم في `installer-config.json` قبل البناء:

| الإعداد | القيمة الافتراضية |
|---|---|
| خادم SQL | `.\SQLEXPRESS` |
| مستخدم SQL | `sa` |
| كلمة مرور SQL | `orax055266` |
| قاعدة البيانات | `Hotel_alkheer` |
| حساب المشرف | `admin` |
| كلمة مرور المشرف | `Admin@2024!` |
| بريد المشرف | `admin@oraxhotel.local` |
| عنوان التطبيق | `http://localhost:5080` |
| خادم الإنتاج البعيد | `95.216.218.251,12356` |
| قاعدة بيانات الإنتاج | `Hotel_talal_2` |

---

## 7. بناء المُثبّت من المصدر

### المتطلبات
- Windows 10/11 x64 (أو Ubuntu 22.04+ مع `dotnet SDK 8`)
- `dotnet SDK 8.0` أو أحدث
- DevExpress NuGet feed صالح

### البناء

```powershell
git clone https://github.com/Nassaralshabi/oraxhotel2024.git
cd oraxhotel2024
git checkout feature/windows-installer
cd installer
.\Build-Installer.ps1
```

النتيجة: `installer\build-output\installer\OraxHotel-Setup.exe`

### خيارات البناء

```powershell
# تخطي بناء التطبيق
.\Build-Installer.ps1 -SkipAppBuild

# تخطي بناء المُثبّت
.\Build-Installer.ps1 -SkipInstallerBuild

# إخراج إلى مجلد مخصص
.\Build-Installer.ps1 -OutputDir "D:\Releases\v1.0.0"
```

---

## 8. استكشاف الأخطاء

### 8.1 "لم يتم العثور على SQL Server"
- تأكد من تثبيت SQL Server Express.
- تأكد من تشغيل الخدمة: `services.msc` → ابحث عن `SQL Server (SQLEXPRESS)`.
- شغّل المُثبّت كـ Administrator.

### 8.2 "فشل الاتصال بحساب sa"
- تأكد من تفعيل وضع SQL Authentication في إعدادات الخادم.
- تأكد من كلمة المرور في `installer-config.json`.
- استخدم الوضع التفاعلي: `OraxHotel-Setup.exe --interactive`.

### 8.3 "تعذر الوصول إلى http://localhost:5080"
- تحقق من تشغيل التطبيق: `tasklist | findstr HotelSys.exe`.
- تحقق من فتح المنفذ في جدار الحماية.
- شغّل يدوياً: `start-oraxhotel.cmd`.

### 8.4 "فشل استعادة قاعدة البيانات"
- تأكد من صلاحية `sa` كـ `sysadmin`.
- شغّل المُثبّت كـ Administrator.
- تحقق من مساحة كافية على قرص SQL Server.
- راجع سجلات SQL Server: `Event Viewer → Windows Logs → Application`.

### 8.5 "DevExpress غير مفعّل"
- DevExpress runtime مُضمّن في الحمولة عبر NuGet — لا يحتاج تفعيلاً.
- في حال ظهور أخطاء ترخيص، تأكد من وجود ملف `license.licx` في مشروع HotelSys.

---

## 9. إزالة التثبيت

### 9.1 إزالة التطبيق فقط (يبقى قاعدة البيانات)
شغّل: `%LOCALAPPDATA%\OraxHotel\uninstall-oraxhotel.cmd`

### 9.2 إزالة كاملة (التطبيق + قاعدة البيانات)
```sql
-- في SQL Server Management Studio
DROP DATABASE Hotel_alkheer;
```
ثم شغّل `uninstall-oraxhotel.cmd`.

### 9.3 تنظيف يدوي
```cmd
rmdir /s /q "%LOCALAPPDATA%\OraxHotel"
rmdir /s /q "%ProgramData%\OraxHotel"
del "%USERPROFILE%\Desktop\Orax Hotel.lnk"
netsh advfirewall firewall delete rule name="OraxHotel-5080"
```

---

## 10. الأمان والمسؤولية

### 10.1 كلمات المرور في الحزمة
ملف `installer-config.json` يحوي كلمات مرور فعلية (`sa` / `orax055266` و `admin` / `Admin@2024!`). عند نشر `OraxHotel-Setup.exe`:

- ⚠ **لا ترفعه إلى مستودع GitHub عام** — استخدم GitHub Releases خاصة.
- ⚠ **غيّر كلمات المرور قبل كل نشر** في `installer-config.json`.
- ⚠ **استخدم token GitHub ذا scope محدود** لرفع الحزمة.

### 10.2 التراخيص
- المُثبّت لا يتجاوز تراخيص DevExpress أو أي مكوّن خارجي.
- على المستخدم ضمان امتلاك ترخيص DevExpress صالح للاستخدام التجاري.

### 10.3 النسخ الاحتياطي
قبل التحديث أو الترقية:
```sql
BACKUP DATABASE Hotel_alkheer TO DISK = 'C:\Backup\Hotel_alkheer_pre_update.bak';
```

---

## 11. الدعم والتحديثات

- المستودع: https://github.com/Nassaralshabi/oraxhotel2024
- الفرع النشط للتطوير: `feature/windows-installer`
- الفرع المستقر: `main`
- الإصدارات: https://github.com/Nassaralshabi/oraxhotel2024/releases

للإبلاغ عن أخطاء أو طلب مزايا، استخدم `Issues` على GitHub.
