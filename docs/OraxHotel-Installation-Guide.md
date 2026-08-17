# دليل تثبيت Orax Hotel — الترتيب الصحيح المتسلسل

> **الإصدار:** 1.0.0
> **الملف:** `OraxHotel-Setup.exe`
> **الحجم:** ~74 MB (self-contained .NET 8 — لا يحتاج تثبيت .NET Runtime)
> **SHA-256:** `f74c0f3662a0a2ff76d46fae4ecded3fc153104acac1ec490d3cb9391ab1e808`
> **عنوان التطبيق بعد التثبيت:** `http://localhost:5080`

---

## الترتيب الصحيح للتثبيت (اقرأ بالترتيب قبل التنفيذ)

### 📋 الخطوة 0: المتطلبات قبل البدء (تحقق أولاً)

| المتطلب | الحد الأدنى | ملاحظات |
|---|---|---|
| نظام التشغيل | Windows 10 (build 1809+) / Windows 11 | x64 فقط |
| المعالج | 1.4 GHz ×64 | 2+ cores مُوصى به |
| الذاكرة RAM | 4 GB | 8 GB مُوصى به |
| مساحة القرص | 2 GB حرة | 5 GB لقاعدة البيانات والتقارير |
| SQL Server | 2019+ أو Express 2019+ | **يجب تثبيته قبل Step 1** |
| صلاحيات | Administrator | لفتح منفذ جدار الحماية |
| متصفح إنترنت | Edge / Chrome / Firefox | حديث (≤2024) |

**تنزيل SQL Server Express:** https://www.microsoft.com/en-us/sql-server/sql-server-downloads

---

### 🔽 الخطوة 1: الحصول على ملف المُثبّت

اختر **إحدى** الطرق التالية:

**الطريقة أ) تنزيل من GitHub Release:**
```
https://github.com/Nassaralshabi/oraxhotel2024/releases/download/windows-installer-v1.0.0/OraxHotel-Setup.exe
```

**الطريقة ب) نسخة محلية:** ملف `OraxHotel-Setup.exe` المرفق (74 MB).

**الطريقة ج) البناء من المصدر:**
```powershell
git clone https://github.com/Nassaralshabi/oraxhotel2024.git
cd oraxhotel2024
git checkout feature/windows-installer
cd installer
.\Build-Installer.ps1
```
(يتطلب dotnet SDK 8 + DevExpress 22.1 NuGet feed)

**تحقق من سلامة الملف (اختياري):**
```cmd
certutil -hashfile OraxHotel-Setup.exe SHA256
```
يجب أن يكون: `f74c0f3662a0a2ff76d46fae4ecded3fc153104acac1ec490d3cb9391ab1e808`

---

### 🗄️ الخطوة 2: التأكد من SQL Server (إجباري قبل Step 3)

1. افتح `Services.msc` (ابحث في قائمة ابدأ).
2. ابحث عن خدمة باسم:
   - `SQL Server (MSSQLSERVER)` — للنسخة الكاملة، أو
   - `SQL Server (SQLEXPRESS)` — للنسخة Express.
3. تأكد أن الحالة: **Running** (قيد التشغيل).
   - إن لم تكن كذلك، اضغط يميناً → **Start**.
4. افتح `SQL Server Configuration Manager`:
   - فعّل بروتوكول **TCP/IP** للـ instance المطلوب.
   - تأكد أن المنفذ الافتراضي 1433 (لـ default instance) أو ديناميكي (لـ SQLEXPRESS).
5. تأكد أن **SQL Server Authentication** مفعّل:
   - في SSMS: Properties → Security → "SQL Server and Windows Authentication mode".

> ⚠ **إن لم يكن SQL Server مُثبّتاً:** ثبّته من رابط Microsoft، ثم عُد إلى هنا. المُثبّت سيفشل بدونه.

---

### ▶️ الخطوة 3: تشغيل المُثبّت (Administrator)

1. اضغط يميناً على `OraxHotel-Setup.exe` → **Run as administrator**.
2. اقبل مُطالبة UAC (User Account Control).
3. انتظر ظهور النافذة:
   ```
   ================================================
     Orax Hotel - مُثبّت Windows المتكامل
     الإصدار: 1.0.0  |  التثبيت الصامت التلقائي
   ================================================
   ```

**الخيارات المتاحة (وسائط سطر الأوامر):**

| الوسيط | الوصف |
|---|---|
| (بدون وسائط) | الوضع الصامت التلقائي — مُفضّل |
| `--interactive` | يطلب إدخال يدوي عند فشل الاتصال الافتراضي |
| `--verbose` | طباعة كل خطوة على الشاشة للتشخيص |
| `--help` | عرض كل الخيارات |

لتشغيل بوسائط:
```cmd
OraxHotel-Setup.exe --verbose
```

---

### ⚙️ الخطوة 4: ماذا يحدث تلقائياً خلف الكواليس

عند تشغيل `OraxHotel-Setup.exe` بدون وسائط، يتم تنفيذ **19 خطوة متسلسلة تلقائياً**:

| # | الخطوة | الوقت التقريبي |
|---|---|---|
| 1 | قراءة `installer-config.json` المضمّن | <1 ثانية |
| 2 | فحص خدمة SQL Server (`MSSQL$SQLEXPRESS` أو `MSSQLSERVER`) | 1 ثانية |
| 3 | محاولة تشغيل الخدمة إن كانت متوقفة | حتى 30 ثانية |
| 4 | اختبار اتصال بحساب `sa` / `orax055266` على `.\SQLEXPRESS` | 1 ثانية |
| 5 | استخراج أداة `7zr.exe` المضمّنة (أو تنزيلها إن لم تكن مضمّنة) | <1 ثانية |
| 6 | استخراج/تنزيل حمولة التطبيق `payload.7z` | 5–60 ثانية |
| 7 | نسخ `HotelSys.exe` + `wwwroot` إلى `%LOCALAPPDATA%\OraxHotel` | 5–30 ثانية |
| 8 | نسخ `Hotel_alkheer20232009552241.bak` إلى `%ProgramData%\OraxHotel\Database` | 1–5 ثانية |
| 9 | منح SQL Server صلاحية قراءة على مجلد النسخة الاحتياطية (icacls) | 1 ثانية |
| 10 | التحقق من وجود قاعدة `Hotel_alkheer` | 1 ثانية |
| 11 | **استعادة القاعدة** من `.bak` إن لم تكن موجودة | 10–120 ثانية |
| 12 | **إنشاء حساب مشرف `admin` / `Admin@2024!`** إن لم يوجد | 1 ثانية |
| 13 | **كتابة `appsettings.json`** كاملاً بكل connection strings (`cc`, `cc0-2`, `Hotel_alkheerContext*`, `HotelDb_2Context*`) | <1 ثانية |
| 14 | إنشاء ملف `start-oraxhotel.cmd` | <1 ثانية |
| 15 | إنشاء ملف `uninstall-oraxhotel.cmd` | <1 ثانية |
| 16 | إنشاء اختصار `Orax Hotel` على سطح المكتب | 1 ثانية |
| 17 | فتح منفذ **5080** في جدار حماية Windows (`netsh advfirewall`) | 1 ثانية |
| 18 | تشغيل `HotelSys.exe` في الخلفية | 2–10 ثانية |
| 19 | فتح المتصفح على `http://localhost:5080` | 1 ثانية |

**الوقت الكلي المتوقع:** 30 ثانية – 3 دقائق حسب أداء الجهاز.

---

### 🔐 الخطوة 5: الدخول الأول للتطبيق

عند فتح المتصفح على `http://localhost:5080`:

1. ستظهر صفحة تسجيل الدخول.
2. أدخل البيانات الافتراضية:

| الحقل | القيمة |
|---|---|
| اسم المستخدم | `admin` |
| كلمة المرور | `Admin@2024!` |

3. اضغط **Login**.

> 💡 **إن لم ينجح الدخول** بـ `admin`، فالقاعدة المُستعادة تحتوي حساب المشرف الأصلي من النسخة الاحتياطية. استخدمه إن كنت تعرفه.

---

### 🛡️ الخطوة 6: تقوية الأمان (إجباري للإنتاج)

**بعد أول دخول ناجح، نفّذ فوراً:**

1. **تغيير كلمة مرور `admin`**:
   - من داخل التطبيق: `الملف الشخصي` → `تغيير كلمة المرور`
   - أو في SSMS على قاعدة `Hotel_alkheer`:
     ```sql
     -- عرض المستخدمين الحاليين
     SELECT UserName, Email FROM dbo.AspNetUsers;
     ```

2. **تغيير كلمة مرور `sa` على SQL Server**:
   ```sql
   ALTER LOGIN sa WITH PASSWORD = '<كلمة_مرور_قوية_جديدة>';
   -- ثم تحديث appsettings.json يدوياً بالقيمة الجديدة
   ```
   ملف `appsettings.json` في: `%LOCALAPPDATA%\OraxHotel\appsettings.json`

3. **تقييد منفذ 5080**:
   ```cmd
   netsh advfirewall firewall delete rule name="OraxHotel-5080"
   netsh advfirewall firewall add rule name="OraxHotel-5080-local" dir=in action=allow protocol=TCP localport=5080 remoteip=LocalSubnet
   ```

4. **تأكيد تشغيل HTTPS** (إن لزم):
   - حالياً التطبيق يعمل بـ HTTP على localhost. لتأمينه:
   ```cmd
   cd %LOCALAPPDATA%\OraxHotel
   HotelSys.exe --urls=https://localhost:5081
   ```

---

### 📂 الخطوة 7: التحقق من التثبيت ومواقع الملفات

**مواقع مهمة بعد التثبيت:**

| المسار | المحتوى |
|---|---|
| `%LOCALAPPDATA%\OraxHotel\HotelSys.exe` | التطبيق |
| `%LOCALAPPDATA%\OraxHotel\appsettings.json` | الإعدادات |
| `%LOCALAPPDATA%\OraxHotel\wwwroot\` | ملفات الواجهة (CSS/JS/صور) |
| `%LOCALAPPDATA%\OraxHotel\start-oraxhotel.cmd` | مُشغّل التطبيق |
| `%LOCALAPPDATA%\OraxHotel\uninstall-oraxhotel.cmd` | مُزيل التثبيت |
| `%ProgramData%\OraxHotel\Database\Hotel_alkheer_seed.bak` | نسخة احتياطية محفوظة |
| `%USERPROFILE%\Desktop\Orax Hotel.lnk` | اختصار سطح المكتب |
| `SQL Server → Hotel_alkheer` | قاعدة البيانات |

**للتحقق من تشغيل التطبيق:**
```cmd
tasklist | findstr HotelSys.exe
netstat -an | findstr :5080
```

---

### 🔄 الخطوة 8: التشغيل اللاحق (بعد إغلاق التطبيق)

**3 طرق لتشغيل التطبيق لاحقاً:**

1. **الاختصار:** اضغط مرتين على `Orax Hotel` على سطح المكتب.
2. **قائمة ابدأ:** ابحث عن `Orax Hotel`.
3. **يدوياً:**
   ```cmd
   cd %LOCALAPPDATA%\OraxHotel
   start-oraxhotel.cmd
   ```

سيتم فتح المتصفح تلقائياً على `http://localhost:5080`.

---

### 🗑️ الخطوة 9: إزالة التثبيت (عند الحاجة)

**لإزالة التطبيق فقط (تبقى قاعدة البيانات):**
```cmd
%LOCALAPPDATA%\OraxHotel\uninstall-oraxhotel.cmd
```

**لإزالة كاملة (التطبيق + قاعدة البيانات):**
1. شغّل `uninstall-oraxhotel.cmd` أعلاه.
2. في SSMS:
   ```sql
   DROP DATABASE Hotel_alkheer;
   ```
3. احذف يدوياً:
   ```cmd
   rmdir /s /q "%ProgramData%\OraxHotel"
   netsh advfirewall firewall delete rule name="OraxHotel-5080"
   ```

---

## 🔧 استكشاف الأخطاء (بالترتيب)

### المشكلة 1: "لم يتم العثور على SQL Server"
- **السبب:** SQL Server غير مُثبّت أو الخدمة متوقفة.
- **الحل:**
  1. ثبّت SQL Server Express من Microsoft.
  2. شغّل الخدمة: `services.msc` → `SQL Server (SQLEXPRESS)` → Start.
  3. أعد تشغيل `OraxHotel-Setup.exe`.

### المشكلة 2: "فشل الاتصال بحساب sa"
- **السبب:** كلمة مرور `sa` مختلفة عن `orax055266` أو الوضع المختلط معطّل.
- **الحل:**
  1. شغّل في الوضع التفاعلي: `OraxHotel-Setup.exe --interactive --verbose`
  2. أدخل اسم الخادم الصحيح وكلمة المرور.
  3. تأكد من تفعيل "SQL Server Authentication" في SSMS.

### المشكلة 3: "payload.7z غير متوفر" أو "فشل استخراج ملفات التطبيق"
- **السبب:** المُثبّت الحالي لا يحوي حمولة مضمّنة (built without DevExpress).
- **الحل:** أحد الخيارات التالية:
  1. ضع ملف `payload.7z` بجانب `OraxHotel-Setup.exe`.
  2. عدّل `installer-config.json` وغيّر `Payload.DownloadUrl` إلى رابط صالح.
  3. ابنِ المُثبّت على Windows مع DevExpress مُثبّتاً عبر `Build-Installer.ps1`.

### المشكلة 4: "فشل استعادة قاعدة البيانات"
- **السبب:** صلاحيات غير كافية أو مساحة قرص غير كافية.
- **الحل:**
  1. شغّل المُثبّت كـ Administrator.
  2. تأكد أن `sa` عضو في `sysadmin`.
  3. تحقق من مساحة قرص SQL Server.
  4. راجع Event Viewer: `Windows Logs → Application`.

### المشكلة 5: "تعذر الوصول إلى http://localhost:5080"
- **السبب:** التطبيق لم يبدأ أو المنفذ مشغول.
- **الحل:**
  ```cmd
  tasklist | findstr HotelSys.exe    :: يجب أن يظهر
  netstat -an | findstr :5080        :: يجب أن يظهر LISTENING
  ```
  - إن لم يعمل: شغّله يدوياً:
    ```cmd
    cd %LOCALAPPDATA%\OraxHotel
    HotelSys.exe
    ```
  - إن كان المنفذ مشغولاً بتطبيق آخر:
    ```cmd
    netstat -ano | findstr :5080
    taskkill /PID <PID> /F
    ```

### المشكلة 6: "DevExpress.Reporting ... license required"
- **السبب:** ترخيص DevExpress غير مُفعّل في بناء HotelSys.
- **الحل:**
  1. على جهاز به DevExpress مُثبّت ومرخّص، شغّل `Build-Installer.ps1`.
  2. سيُضمّن ترخيص DevExpress في HotelSys.exe تلقائياً.

---

## 📊 ملخص سريع (Quick Reference)

| المعلومة | القيمة |
|---|---|
| اسم الملف | `OraxHotel-Setup.exe` |
| الحجم | ~74 MB |
| الإصدار | 1.0.0 |
| SHA-256 | `f74c0f3662a0a2ff76d46fae4ecded3fc153104acac1ec490d3cb9391ab1e808` |
| خادم SQL الافتراضي | `.\SQLEXPRESS` |
| حساب SQL الافتراضي | `sa` / `orax055266` |
| قاعدة البيانات | `Hotel_alkheer` |
| حساب المشرف الافتراضي | `admin` / `Admin@2024!` |
| عنوان التطبيق | `http://localhost:5080` |
| مسار التثبيت | `%LOCALAPPDATA%\OraxHotel` |
| خادم الإنتاج البعيد | `95.216.218.251,12356` (مرجعي فقط) |

---

## ⚠️ تنبيهات أمنية هامة

1. **`installer-config.json` مرفوع على GitHub** مع كلمات مرور فعلية. بما أن المستودع خاص، هذا مقبول مؤقتاً، لكن:
   - غيّر كلمة مرور `sa` على خوادم الإنتاج بعد أي نشر علني.
   - لا ترفع `OraxHotel-Setup.exe` إلى GitHub Releases العامة — يحوي أسراراً قابلة للاستخراج.
   - استخدم GitHub Releases خاصة (private) أو قناة توزيع آمنة.

2. **الثغرات المعلنة من GitHub:** يوجد ثغرتان (1 high, 1 moderate) في فرع `main`. راجعهما:
   https://github.com/Nassaralshabi/oraxhotel2024/security/dependabot

3. **التراخيص:** المُثبّت لا يتجاوز تراخيص DevExpress. على المستخدم ضمان امتلاك ترخيص صالح للاستخدام التجاري.

4. **النسخ الاحتياطي:** قبل التحديث أو الترقية:
   ```sql
   BACKUP DATABASE Hotel_alkheer TO DISK = 'C:\Backup\Hotel_alkheer_pre_update.bak';
   ```

---

## 📞 الدعم والتحديثات

- المستودع: https://github.com/Nassaralshabi/oraxhotel2024
- الفرع النشط للتطوير: `feature/windows-installer`
- الإصدارات: https://github.com/Nassaralshabi/oraxhotel2024/releases
- للإبلاغ عن أخطاء أو طلب مزايا: https://github.com/Nassaralshabi/oraxhotel2024/issues
