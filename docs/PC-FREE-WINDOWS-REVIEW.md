# مراجعة PC-Free كبيئة Windows مجانية

## الحكم المختصر

مستودع [PC-Free](https://github.com/jephersonRD/pc-free) ليس خدمة Windows مجانية مستقلة جاهزة للمستخدم، بل وصفة مفتوحة المصدر لتشغيل صورة `dockurr/windows` داخل Docker من GitHub Codespaces والوصول إليها عبر noVNC على المنفذ 8006.

## القيود المثبتة

يذكر README وQuick Start في المستودع أن الإعداد يحتاج Codespace كبيراً، Docker، مساحة لا تقل تقريباً عن 10GB في Codespaces، وملف Compose يمرر `/dev/kvm` و`/dev/net/tun`. كما يذكر أن أول تشغيل ينزّل صورة Windows كبيرة ويستغرق عدة دقائق، وأن Windows يحتاج ترخيصاً صالحاً وفق مسؤولية المستخدم.

وثائق GitHub الرسمية تذكر أن الحسابات الشخصية المجانية تحصل على حصة شهرية من Codespaces، وأن الاستخدام بعد الحصة يُحجب إذا لم توجد وسيلة دفع صالحة، بينما قد يخضع التخزين والوقت الزائد للفوترة. المصدر: [GitHub Codespaces billing](https://docs.github.com/billing/managing-billing-for-github-codespaces/about-billing-for-github-codespaces).

وثائق `dockurr/windows` الرسمية تذكر الحاجة إلى مضيف Linux مع KVM، أو Docker Desktop على Windows 11 مع nested virtualization، وأن Docker Desktop على Linux/macOS/Windows 10 لا يوفر KVM المطلوب حالياً لهذا الاستخدام. المصدر: [dockur/windows](https://github.com/dockur/windows).

## الملاءمة لـ Orax Hotel

PC-Free مناسب نظرياً لاختبار EXE من متصفح إذا نجح Codespace في توفير KVM وكانت الحصة المجانية والمساحة كافيتين. لكنه ليس مضموناً كحل مجاني دائم، ولا يمكن اعتباره بديلاً مستقراً لجهاز Windows أو Cloud PC مدفوع. كما أن تشغيل SQL Server Express وEXE بحجم يقارب 426MB داخل Windows الافتراضي يحتاج وقتاً ومساحة إضافية.

## المسار الموصى به

المسار المجاني الأول هو استخدام fork خاص بالمستخدم في GitHub، فتح Codespace بأكبر machine متاحة، تشغيل Compose، جعل المنفذ 8006 مرئياً للمستخدم فقط، ثم رفع `OraxHotel-Setup.exe` من GitHub Release إلى Windows الافتراضي. بعد ذلك تُجرى خطوات UAC وتثبيت SQL Server وتسجيل الدخول. إذا فشل KVM أو استُهلكت الحصة المجانية، فالبديل العملي هو GitHub Actions Windows runner للاختبار الآلي، أو Windows Cloud PC مدفوع لفترة قصيرة.

## مصادر

[1]: https://github.com/jephersonRD/pc-free "PC-Free repository"
[2]: https://raw.githubusercontent.com/jephersonRD/pc-free/main/quickstart.html "PC-Free Quick Start"
[3]: https://docs.github.com/billing/managing-billing-for-github-codespaces/about-billing-for-github-codespaces "GitHub Codespaces billing"
[4]: https://github.com/features/codespaces "GitHub Codespaces"
[5]: https://github.com/dockur/windows "dockur/windows requirements"

## نتيجة اختبار GitHub Actions الفعلية

تم تشغيل Windows runner فعلياً. نجح تنزيل release والتحقق من SHA-256 وPE، لكن asset `windows-installer-v1.2.0` فشل في خطوة التثبيت لأن النسخة المرفوعة كانت تفاعلية وتنتظر إجابة سؤال تثبيت SQL Server؛ لذلك لا يكفي تشغيلها بلا إدخال. تم تحديث workflow لتمرير الإدخال تلقائياً، ثم استُبدل المسار ليبني من المصدر الحالي.

أول محاولة build من المصدر كشفت سبباً مستقلاً: `NuGet.config` كان يفرض مصدراً محلياً هو `C:\Program Files\DevExpress 22.1\DevExtreme\System\DevExtreme\Bin\AspNetCore` غير موجود على runner. تم التحقق من توفر `DevExpress.AspNetCore.Reporting 25.1.3` على NuGet الرسمي، ثم استبدال المصدر المحلي بمصدر `nuget.org` في `NuGet.config`. workflow الحالي يبني من المصدر بعد هذا الإصلاح ثم يثبت SQL Server ويفحص قاعدة `Hotel_alkheer` وendpoint الصحة.
