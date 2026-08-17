# حزمة Orax Hotel Windows

تم نشر الحزمة الكبيرة كأصل GitHub Release لأن حجمها يتجاوز حد GitHub للملف داخل Git branch.

## رابط التنزيل

[تنزيل OraxHotel-Windows-Package.zip](https://github.com/Nassaralshabi/oraxhotel2024/releases/download/windows-installer-v1.0.0/OraxHotel-Windows-Package.zip)

تحتوي الحزمة على `OraxHotel-Setup.exe` ودليل التثبيت وملاحظات التشغيل وملف `SHA256SUMS.txt`. يقوم المُثبّت بتثبيت النظام والملحقات المضمنة داخله، ويطلب إعداد SQL Server أثناء التثبيت.

## التحقق

بصمة SHA-256 لملف ZIP:

```text
caf6bd409efb768cbcec7caea6997b66046e0fdaa5c2d53cbf1c34e50e7ee1c0
```

## الأمان

لا تحتوي الحزمة أو هذا المستند على كلمة مرور المشرف أو كلمات مرور SQL Server أو رموز GitHub. يستخدم التطبيق حساب المشرف الموجود في قاعدة البيانات المستعادة، وتُدخل بيانات اتصال SQL Server أثناء التثبيت محليًا.
