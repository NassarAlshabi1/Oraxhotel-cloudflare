# بحث رسمي مختصر: Appwrite REST وpagination

## المصادر الرسمية

[1]: https://appwrite.io/docs/apis/rest "Appwrite REST API"
[2]: https://appwrite.io/docs/products/databases/pagination "Appwrite Databases Pagination"
[3]: https://appwrite.io/docs/products/databases/legacy/documents "Appwrite Legacy Documents"

## النتائج المثبتة

وفق [1]، تتطلب طلبات REST رأس `X-Appwrite-Project`، ويمكن للمزود الخادمي استخدام `X-Appwrite-Key` للمصادقة. يوضح المصدر أن API key سرّ ولا ينبغي استخدامه في تطبيقات العميل. كما يوضح أن query strings تُرسل بصيغة JSON escaped عبر `queries[]`، ويمكن تكرار معامل المصفوفة لإرسال أكثر من query.

وفق [2] و[3]، تعيد عمليات list افتراضياً 25 مستنداً في الصفحة. يمكن التحكم بالحجم بواسطة `limit` وبالموضع بواسطة `offset`. توصي الوثائق باستخدام cursor pagination للبيانات كثيرة التغير، بينما offset مناسب للبيانات قليلة التغير أو القوائم محدودة الصفحات. كما تحذر وثائق pagination من أن offset قد ينتج مستندات مكررة أو مفقودة إذا تغيّرت البيانات أثناء القراءة.

وفق [3]، تحتاج عمليات إنشاء المستندات إلى صلاحيات create، وتحتاج القراءة إلى read، وتحتاج upsert إلى create/update على مستوى collection أو document. هذا يفسر ضرورة اختبار صلاحيات collections الأربع قبل إعلان التكامل جاهزاً.

## أثر النتائج على Orax

يستخدم Orax REST الخادمي مع API key، ولا يضع الكود الخادمي في Flutter. يستخدم العميل المشترك pagination بـ `limit/offset` ويتحقق من `total`؛ لذلك فهو مناسب للـ seed الحالي المحدود، مع بقاء cursor pagination تحسيناً مستقبلياً إذا كبرت collections أو أصبحت كثيرة التغير. يجب اختبار عدم وجود تداخلات أو تكرارات عند تنفيذ دورة تتزامن مع تغييرات Appwrite، خصوصاً قبل تفعيل أي مزامنة عكسية.

## ملاحظة زمنية

تمت قراءة المصادر في 20 أغسطس 2026، ويجب إعادة التحقق من صيغة API عند ترقية Appwrite أو الانتقال من Databases legacy إلى TablesDB.
