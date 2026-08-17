# حزمة Orax Hotel Windows — الإصدار المتكامل v1.0.0

تم نشر الحزمة الكبيرة كأصل GitHub Release لأن حجمها يتجاوز حد GitHub للملف داخل Git branch.

## 🔗 رابط التنزيل

[تنزيل OraxHotel-Windows-Package.zip](https://github.com/Nassaralshabi/oraxhotel2024/releases/download/windows-installer-v1.0.0/OraxHotel-Windows-Package.zip)

تحتوي الحزمة على:
- `OraxHotel-Setup.exe` — المُثبّت المتكامل (self-contained)
- `docs/` — دليل التثبيت وملاحظات التشغيل
- `database/` — النسخة الاحتياطية لقاعدة البيانات (مدمجة في الـ exe أيضاً)
- `SHA256SUMS.txt` — بصمات التحقق

## ✨ ما الجديد في v1.0.0

- **تثبيت صامت كامل** — لا يطلب أي إدخال من المستخدم
- **إعدادات SQL Server مُسبقة** — كل الأسرار في `installer-config.json`
- **استعادة تلقائية** لقاعدة `Hotel_alkheer` من النسخة الاحتياطية المضمّنة
- **كتابة `appsettings.json` بالكامل** بكل connection strings
- **إنشاء حساب مشرف افتراضي** تلقائياً (`admin` / `Admin@2024!`)
- **اختصار سطح المكتب** تلقائياً
- **فتح منفذ 5080** في جدار الحماية تلقائياً
- **تشغيل التطبيق** وفتح المتصفح تلقائياً

## 🔐 التحقق

بصمة SHA-256 لملف ZIP:

```text
(تُحدّث بعد بناء الحزمة النهائية — شغّل Get-FileHash على Windows أو sha256sum على Linux)
```

## 🛡️ الأمان

> ⚠ **تنبيه مهم:** هذه الحزمة تحتوي إعدادات SQL Server افتراضية جاهزة (خادم، حساب sa، كلمة مرور).
> لا ترفعها إلى مستودع عام. وزّعها فقط عبر GitHub Releases خاصة أو قناة آمنة.

كلمات المرور الافتراضية في الحزمة:

| الحساب | كلمة المرور الافتراضية |
|---|---|
| SQL Server `sa` | `orax055266` |
| تطبيق `admin` | `Admin@2024!` |

**يجب تغيير كل كلمات المرور هذه بعد التثبيت في بيئة الإنتاج.**

## 📋 ما يحتاجه المستخدم قبل التثبيت

- Windows 10/11 x64
- SQL Server 2019+ أو SQL Server Express 2019+
- تنزيل SQL Server Express: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
- صلاحيات Administrator (مُفضّلة)

## 🚀 خطوات التثبيت

1. ثبّت SQL Server Express إن لم يكن مُثبّتاً.
2. استخرج `OraxHotel-Windows-Package.zip`.
3. شغّل `OraxHotel-Setup.exe` كـ Administrator.
4. انتظر 30 ثانية إلى 3 دقائق.
5. سيفتح المتصفح تلقائياً على `http://localhost:5080`.
6. سجّل الدخول بـ `admin` / `Admin@2024!`.
7. غيّر كلمة المرور فوراً من لوحة تحكم المستخدم.

## 📞 الدعم

للاستفسارات والمشاكل: https://github.com/Nassaralshabi/oraxhotel2024/issues
