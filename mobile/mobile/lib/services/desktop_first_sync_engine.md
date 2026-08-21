# DesktopFirstSyncEngine

هذا المحرك يطبق سياسة **Offline-first** مع اعتبار SQL Server/نسخة Oraxhotel المكتبية مصدر الحقيقة.

## مبدأ الاستخدام

يجب أن تنفذ شاشة Flutter العملية داخل Drift أولاً، ثم تستدعي `enqueueLocalChange` داخل نفس دورة العملية. إذا لم توجد شبكة، تبقى العملية في Drift ويبقى سجلها في `outbox`. عند توفر الشبكة يرسل المحرك الأمر إلى `/api/desktop/commands`، ولا يكتب الهاتف مباشرة فوق الإسقاط canonical.

```dart
final engine = DesktopFirstSyncEngine(
  config: const DesktopFirstSyncConfig(
    workerUrl: 'https://<worker-domain>',
    username: '<authenticated-user>',
    password: '<provided-at-runtime>',
    deviceId: 'phone-<stable-id>',
  ),
  outbox: OutboxDao(database),
  adapters: <String, DesktopEntityAdapter>{
    'rooms': roomsAdapter,
    'bookings': bookingsAdapter,
    'guest_infos': guestsAdapter,
    'payments': paymentsAdapter,
  },
);

await engine.enqueueLocalChange(
  entity: 'bookings',
  operation: 'create',
  localUuid: booking.localUuid,
  serverId: booking.serverId,
  payload: booking.toSyncJson(),
  clientTimestamp: DateTime.now().millisecondsSinceEpoch ~/ 1000,
);

final result = await engine.sync();
```

## عقد الـ adapter

كل adapter يجب أن يعرف mappingه من إسقاط الكمبيوتر إلى جدول Drift. لا يجوز استخدام adapter عام ينسخ كل مفاتيح JSON آلياً. عند `pull` يطابق adapter أولاً بـ `local_uuid` ثم بـ `server_id` وفق السياسة المعتمدة، ويطبق الحذف كـ `deleted_at` محلياً بدلاً من حذف الصف نهائياً.

## حالات النتيجة

| الحالة | المعنى |
|---|---|
| `success` | أُرسلت الأوامر وقُرئت الأحداث canonical وطُبقت محلياً. |
| `offlineQueued` | لم تتوفر الشبكة؛ لم تُحذف أي عملية من outbox. |
| `partial` | تمت بعض العمليات وبقيت أخرى للفحص أو إعادة المحاولة. |
| `failed` | رفض دائم أو خطأ في العقد؛ السجل يُحفظ للمراجعة ولا يختفي. |

## ضمانات السلامة

يستخدم المحرك `idempotency_key` لكل أمر، ويترك معالجة SQL Server للناشر المكتبي. لا يتقدم cursor إلا بعد تطبيق صفحة pull كاملة محلياً. أخطاء الشبكة تُسجل كـ `failed` قابلة لإعادة المحاولة، بينما أخطاء العقد الثابتة تُوضع في `dead` للمراجعة اليدوية.

## متطلبات الخادم

المسار `/api/desktop/commands` يستقبل الأوامر في `desktop_sync_commands`. يلزم وجود ناشر مكتبي لاحقاً يسحب الحالات `pending`، يطبق الأمر على SQL Server، ثم ينشر السجل canonical إلى إسقاط D1. لذلك وجود المحرك وحده لا يعني أن تعديل الهاتف سيغير SQL Server؛ هذه حماية مقصودة لمصدر الحقيقة.
