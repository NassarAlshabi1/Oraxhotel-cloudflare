# Production Audit Findings — Oraxhotel

## نتائج مؤكدة

ثبت أن Worker يمر بفحص TypeScript وdry-run بحجم 74.21 KiB وبلا ثغرات في dependencies الإنتاجية، كما نجح اختبار Windows Acceptance على commit `896428e`.

ثبت أن `HotelSys` لا يحتوي حالياً على عميل Cloudflare Worker أو ناشر أحداث إلى D1؛ تكامل سطح المكتب الموجود هو Appwrite القديم. أما `/api/desktop/commands` في Worker فيُنشئ سجلات pending في D1 فقط، ولا يطبق الأمر على SQL Server ولا ينشر الصف القانوني إلى الإسقاطات. لذلك لا توجد بعد حلقة إنتاجية كاملة من SQL Server إلى Worker/D1 أو من أمر الهاتف إلى SQL Server.

يستخدم Worker قيمة `CORS_ORIGIN = "*"` في `wrangler.toml`. يجب تقييدها إلى origin معروف قبل النشر العام، خصوصاً مع وجود مسارات مصادقة وأوامر هاتفية.

يتطلب login في Worker مستخدماً موجوداً في D1؛ لا توجد بيانات اعتماد تجريبية احتياطية. يلزم bootstrap مصادق لمستخدم admin قبل اختبار Flutter الحقيقي، مع تمرير username/password/device ID وقت التشغيل وعدم تضمينها في APK.

محرك Flutter يرسل أوامر الهاتف إلى `/api/desktop/commands` ويسحب التغييرات من `/api/sync/pull`، ويحافظ على outbox محلي وcursor. نجاح هذا المحرك على مستوى الكود لا يثبت وصول الأوامر إلى SQL Server ما لم تُنفذ خدمة desktop publisher.

## قرار الإصدار الحالي

نسخة Windows قابلة للبناء والاختبار الآلي، وWorker قابل للفحص والنشر بعد مراجعة CORS واعتماد البيئة. نسخة Flutter لا يمكن بناؤها في sandbox الحالية لغياب Flutter/Dart، كما أن وجود keystore المحلي لا يكفي لإنتاج APK قابل لإعادة البناء في CI لأن ملف التوقيع غير متعقّب ويجب حقنه من Secrets.

لا يُنشر Worker إلى البيئة الحية قبل اعتماد CORS، bootstrap للمستخدم، اختبار smoke مصادق، وتحديد آلية desktop publisher. لا تُعاد مفاتيح Appwrite أو Cloudflare أو GitHub التي ظهرت في السجل؛ يجب تدويرها من مالك الحساب وفق إجراء مستقل.
