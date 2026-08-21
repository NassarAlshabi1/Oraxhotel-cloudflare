# تدقيق مخطط Oraxhotel وتكامل Flutter/Dart مع Cloudflare D1

> هذا التقرير مشتق آلياً من تعريفات Drift وملفات Worker الموجودة في المستودع المحلي، وليس من تخمين أو من مخطط خارجي.

## النطاق والمصادر

| المصدر | الاستخدام |
|---|---|
| `mobile/mobile/lib/services/local_db.dart` | المصدر المرجعي للجداول والحقول وأنواع Drift والقيود المحلية، schemaVersion 51. |
| `mobile/worker/schema.sql` | مخطط D1 السابق؛ كان ناقصاً مقارنةً بقائمة كيانات Flutter. |
| `mobile/worker/src/database.ts` | خريطة كيانات Worker، CRUD، الحذف المنطقي، السجل والتعارضات. |
| `mobile/worker/src/sync.ts` | عقد HTTP للمزامنة وعمليات push/pull/migrate. |
| `mobile/mobile/lib/services/cloudflare_config.dart` | قائمة كيانات Flutter وترتيب migration؛ يستثني hotel_day_ledger من Cloudflare sync. |
| `mobile/mobile/lib/services/cloudflare_sync_manager.dart` | شكل payload، local_uuid، cursor، الضغط gzip، والحذف/التعارضات محلياً. |

## نتيجة الاستخراج

تم استخراج **30 جدولاً** مسجلاً في Drift، منها **20 كياناً مرشحاً لمخطط D1** و**10 جداول محلية للبنية التشغيلية**. النسخة المحلية هي schemaVersion **51**.

| التصنيف | الجداول |
|---|---|
| كيانات D1/Worker | rooms, bookings, booking_notes, employees, expenses, cash_transactions, payments, debts, shift_notes, booking_nights, hotel_day_ledger, price_adjustments, booking_price_adjustments, audit_logs, payment_voids, guest_infos, salary_cycles, salary_payments, salary_withdrawals, salary_carry_over_logs |
| محلية فقط أو بنية Flutter | auto_fix_runs, integrity_violations, app_sessions, outbox, sync_state, restore_fix_log, sync_queue, sync_log, sync_conflicts, ancestor_cache |

## عقد الأنواع والتحويل

| Drift | D1/SQLite | ملاحظة التكامل |
|---|---|---|
| `TextColumn` | `TEXT` | قيم نصية وتواريخ ISO/مفاتيح JSON. |
| `IntColumn` | `INTEGER` | المعرفات الرقمية والطوابع الزمنية بالثواني. |
| `RealColumn` | `REAL` | المبالغ والمعدلات. |
| `BoolColumn` | `INTEGER` | Flutter يحول Boolean إلى 0/1 عند SQL؛ يجب أن يبقى ذلك ثابتاً في D1. |
| `SyncFields.localUuid` | `TEXT NOT NULL UNIQUE` | مفتاح المطابقة بين Flutter وD1؛ لا يجوز الاعتماد على `id` وحده. |
| `SyncFields.serverId` | `INTEGER NULL` | يطابق Drift الحالي، بخلاف مخطط D1 السابق الذي عرّفه كنص. |

## الجداول والحقول والأنواع والقيود

### `rooms` — `Rooms` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `roomNumber` | `room_number` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 19 | `type` | `type` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 20 | `price` | `price` | `RealColumn` | `REAL` | لا | `` | لا |  |
| 21 | `status` | `status` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 22 | `imageUrl` | `image_url` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 23 | `cleaningStatus` | `cleaning_status` | `TextColumn` | `TEXT` | لا | `'clean'` | لا |  |
| 24 | `lastCleanedHotelDay` | `last_cleaned_hotel_day` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 25 | `lastOccupiedHotelDay` | `last_occupied_hotel_day` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 26 | `requiresMaintenance` | `requires_maintenance` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |

### `bookings` — `Bookings` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `serverBookingId` | `server_booking_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 19 | `roomNumber` | `room_number` | `TextColumn` | `TEXT` | لا | `` | لا | `Rooms.roomNumber` |
| 20 | `guestName` | `guest_name` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 21 | `guestPhone` | `guest_phone` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 22 | `guestIdType` | `guest_id_type` | `TextColumn` | `TEXT` | لا | `'بطاقة شخصية'` | لا |  |
| 23 | `guestIdNumber` | `guest_id_number` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 24 | `guestIdIssueDate` | `guest_id_issue_date` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 25 | `guestIdIssuePlace` | `guest_id_issue_place` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 26 | `guestNationality` | `guest_nationality` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 27 | `guestEmail` | `guest_email` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 28 | `guestAddress` | `guest_address` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 29 | `checkinDate` | `checkin_date` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 30 | `checkoutDate` | `checkout_date` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 31 | `actualCheckout` | `actual_checkout` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 32 | `status` | `status` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 33 | `notes` | `notes` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 34 | `discount` | `discount` | `RealColumn` | `REAL` | لا | `0` | لا |  |
| 35 | `discountType` | `discount_type` | `TextColumn` | `TEXT` | لا | `'per_night'` | لا |  |
| 36 | `discountStartDate` | `discount_start_date` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 37 | `expectedNights` | `expected_nights` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 38 | `calculatedNights` | `calculated_nights` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 39 | `totalNightsCached` | `total_nights_cached` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 40 | `stayDurationIso` | `stay_duration_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 41 | `lastNightEpoch` | `last_night_epoch` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 42 | `isOverdue` | `is_overdue` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |
| 43 | `needsCheckoutReview` | `needs_checkout_review` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |
| 44 | `totalDueCached` | `total_due_cached` | `RealColumn` | `REAL` | لا | `0.0` | لا |  |
| 45 | `totalPaidCached` | `total_paid_cached` | `RealColumn` | `REAL` | لا | `0.0` | لا |  |
| 46 | `remainingBalanceCached` | `remaining_balance_cached` | `RealColumn` | `REAL` | لا | `0.0` | لا |  |
| 47 | `isFullyPaid` | `is_fully_paid` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |
| 48 | `hotelDayCheckin` | `hotel_day_checkin` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 49 | `hotelDayCheckout` | `hotel_day_checkout` | `TextColumn` | `TEXT` | نعم | `` | لا |  |

### `booking_notes` — `BookingNotes` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `bookingId` | `booking_id` | `IntColumn` | `INTEGER` | لا | `` | لا | `Bookings.id` |
| 19 | `noteText` | `note_text` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 20 | `alertType` | `alert_type` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 21 | `alertUntil` | `alert_until` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 22 | `isActive` | `is_active` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |

### `employees` — `Employees` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `name` | `name` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 19 | `basicSalary` | `basic_salary` | `RealColumn` | `REAL` | لا | `` | لا |  |
| 20 | `position` | `position` | `TextColumn` | `TEXT` | لا | `'موظف'` | لا |  |
| 21 | `phone` | `phone` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 22 | `hireDate` | `hire_date` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 23 | `status` | `status` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 24 | `terminationDate` | `termination_date` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 25 | `terminationReason` | `termination_reason` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 26 | `employeeID` | `employee_i_d` | `TextColumn` | `TEXT` | نعم | `` | لا |  |

### `expenses` — `Expenses` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `expenseType` | `expense_type` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 19 | `relatedId` | `related_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 20 | `description` | `description` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 21 | `amount` | `amount` | `RealColumn` | `REAL` | لا | `` | لا |  |
| 22 | `date` | `date` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 23 | `cashTransactionId` | `cash_transaction_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 24 | `hotelDayKey` | `hotel_day_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 25 | `categoryUuid` | `category_uuid` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 26 | `cashFlowUuid` | `cash_flow_uuid` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 27 | `isAutoGenerated` | `is_auto_generated` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |
| 28 | `employeeUuid` | `employee_uuid` | `TextColumn` | `TEXT` | نعم | `` | لا |  |

### `cash_transactions` — `CashTransactions` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `registerId` | `register_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 19 | `transactionType` | `transaction_type` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 20 | `amount` | `amount` | `RealColumn` | `REAL` | لا | `` | لا |  |
| 21 | `referenceType` | `reference_type` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 22 | `referenceId` | `reference_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 23 | `description` | `description` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 24 | `transactionTime` | `transaction_time` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 25 | `createdBy` | `created_by` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |

### `payments` — `Payments` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `serverPaymentId` | `server_payment_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 19 | `bookingLocalId` | `booking_local_id` | `IntColumn` | `INTEGER` | نعم | `` | لا | `Bookings.id` |
| 20 | `serverBookingId` | `server_booking_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 21 | `roomNumber` | `room_number` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 22 | `amount` | `amount` | `RealColumn` | `REAL` | لا | `` | لا |  |
| 23 | `paymentDate` | `payment_date` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 24 | `notes` | `notes` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 25 | `paymentMethod` | `payment_method` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 26 | `revenueType` | `revenue_type` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 27 | `cashTransactionLocalId` | `cash_transaction_local_id` | `IntColumn` | `INTEGER` | نعم | `` | لا | `CashTransactions.id` |
| 28 | `cashTransactionServerId` | `cash_transaction_server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 29 | `referenceNumber` | `reference_number` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 30 | `hotelDayKey` | `hotel_day_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 31 | `isPendingBalance` | `is_pending_balance` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |
| 32 | `linkedDebtUuid` | `linked_debt_uuid` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 33 | `bookingUuidCache` | `booking_uuid_cache` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 34 | `discountAmount` | `discount_amount` | `RealColumn` | `REAL` | نعم | `` | لا |  |
| 35 | `discountStartDate` | `discount_start_date` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 36 | `isVoided` | `is_voided` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |
| 37 | `voidedAt` | `voided_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 38 | `voidedBy` | `voided_by` | `TextColumn` | `TEXT` | نعم | `` | لا |  |

### `debts` — `Debts` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `bookingLocalId` | `booking_local_id` | `IntColumn` | `INTEGER` | نعم | `` | لا | `Bookings.id` |
| 19 | `guestName` | `guest_name` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 20 | `checkinDate` | `checkin_date` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 21 | `checkoutDate` | `checkout_date` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 22 | `dateRecorded` | `date_recorded` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 23 | `debtReason` | `debt_reason` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 24 | `totalAmount` | `total_amount` | `RealColumn` | `REAL` | لا | `` | لا |  |
| 25 | `paidAmount` | `paid_amount` | `RealColumn` | `REAL` | لا | `` | لا |  |
| 26 | `remainingAmount` | `remaining_amount` | `RealColumn` | `REAL` | لا | `` | لا |  |
| 27 | `paymentDate` | `payment_date` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 28 | `isSettled` | `is_settled` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 29 | `pledge` | `pledge` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 30 | `pledgeType` | `pledge_type` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 31 | `note` | `note` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 32 | `debtUuid` | `debt_uuid` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 33 | `hotelDayOpened` | `hotel_day_opened` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 34 | `hotelDayClosed` | `hotel_day_closed` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 35 | `isFromAutoFix` | `is_from_auto_fix` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |
| 36 | `settlementConfirmed` | `settlement_confirmed` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |

### `shift_notes` — `ShiftNotes` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `title` | `title` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 19 | `content` | `content` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 20 | `priority` | `priority` | `TextColumn` | `TEXT` | لا | `'medium'` | لا |  |
| 21 | `shiftType` | `shift_type` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 22 | `isRead` | `is_read` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 23 | `expiresAt` | `expires_at` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 24 | `createdBy` | `created_by` | `TextColumn` | `TEXT` | لا | `'user'` | لا |  |

### `booking_nights` — `BookingNights` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `bookingLocalId` | `booking_local_id` | `IntColumn` | `INTEGER` | لا | `` | لا | `Bookings.id` |
| 19 | `hotelDayKey` | `hotel_day_key` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 20 | `nightStart` | `night_start` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 21 | `nightEnd` | `night_end` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 22 | `nightlyRate` | `nightly_rate` | `RealColumn` | `REAL` | لا | `0.0` | لا |  |
| 23 | `sequence` | `sequence` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 24 | `isProcessedByAutoFix` | `is_processed_by_auto_fix` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |
| 25 | `baseRate` | `base_rate` | `RealColumn` | `REAL` | لا | `0.0` | لا |  |
| 26 | `adjustment` | `adjustment` | `RealColumn` | `REAL` | لا | `0.0` | لا |  |
| 27 | `finalRate` | `final_rate` | `RealColumn` | `REAL` | لا | `0.0` | لا |  |
| 28 | `appliedAdjustmentUuid` | `applied_adjustment_uuid` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 29 | `appliedAdjustmentsJson` | `applied_adjustments_json` | `TextColumn` | `TEXT` | نعم | `` | لا |  |

**المفاتيح الفريدة المركبة المعلنة في Drift:** `[['bookingLocalId', 'hotelDayKey']]`.

### `hotel_day_ledger` — `HotelDayLedger` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `hotelDayKey` | `hotel_day_key` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 19 | `totalIncome` | `total_income` | `RealColumn` | `REAL` | لا | `0.0` | لا |  |
| 20 | `totalExpenses` | `total_expenses` | `RealColumn` | `REAL` | لا | `0.0` | لا |  |
| 21 | `pendingBalances` | `pending_balances` | `RealColumn` | `REAL` | لا | `0.0` | لا |  |
| 22 | `occupancyRate` | `occupancy_rate` | `RealColumn` | `REAL` | لا | `0.0` | لا |  |
| 23 | `bookingsProcessed` | `bookings_processed` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 24 | `paymentsProcessed` | `payments_processed` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 25 | `debtsProcessed` | `debts_processed` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 26 | `expensesProcessed` | `expenses_processed` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 27 | `status` | `status` | `TextColumn` | `TEXT` | لا | `'draft'` | لا |  |

**المفاتيح الفريدة المركبة المعلنة في Drift:** `[['hotelDayKey']]`.

### `price_adjustments` — `PriceAdjustments` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `targetType` | `target_type` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 19 | `targetUuid` | `target_uuid` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 20 | `adjustmentType` | `adjustment_type` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 21 | `previousValue` | `previous_value` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 22 | `newValue` | `new_value` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 23 | `reason` | `reason` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 24 | `effectiveDate` | `effective_date` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 25 | `appliedBy` | `applied_by` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 26 | `hotelDayKey` | `hotel_day_key` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 27 | `isReversed` | `is_reversed` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |
| 28 | `reversedAt` | `reversed_at` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 29 | `reversedBy` | `reversed_by` | `TextColumn` | `TEXT` | نعم | `` | لا |  |

### `booking_price_adjustments` — `BookingPriceAdjustments` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `bookingLocalUuid` | `booking_local_uuid` | `TextColumn` | `TEXT` | لا | `` | لا | `Bookings.localUuid` |
| 19 | `bookingLocalId` | `booking_local_id` | `IntColumn` | `INTEGER` | نعم | `` | لا | `Bookings.id` |
| 20 | `roomNumber` | `room_number` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 21 | `adjustmentType` | `adjustment_type` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 22 | `adjustmentMode` | `adjustment_mode` | `TextColumn` | `TEXT` | لا | `'per_night'` | لا |  |
| 23 | `amount` | `amount` | `RealColumn` | `REAL` | لا | `0.0` | لا |  |
| 24 | `effectiveHotelDay` | `effective_hotel_day` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 25 | `endHotelDay` | `end_hotel_day` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 26 | `isActive` | `is_active` | `BoolColumn` | `INTEGER` | لا | `true` | لا |  |
| 27 | `reason` | `reason` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 28 | `appliedBy` | `applied_by` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 29 | `cancelledAt` | `cancelled_at` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 30 | `cancelledBy` | `cancelled_by` | `TextColumn` | `TEXT` | نعم | `` | لا |  |

### `audit_logs` — `AuditLogs` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `operationType` | `operation_type` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 19 | `entityType` | `entity_type` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 20 | `entityUuid` | `entity_uuid` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 21 | `entityId` | `entity_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 22 | `previousState` | `previous_state` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 23 | `newState` | `new_state` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 24 | `changedFields` | `changed_fields` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 25 | `performedBy` | `performed_by` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 26 | `ipAddress` | `ip_address` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 27 | `hotelDayKey` | `hotel_day_key` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 28 | `timestamp` | `timestamp` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 29 | `timestampIso` | `timestamp_iso` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 30 | `isFinancial` | `is_financial` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |
| 31 | `amountImpact` | `amount_impact` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |

### `payment_voids` — `PaymentVoids` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `originalPaymentUuid` | `original_payment_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 19 | `originalPaymentId` | `original_payment_id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 20 | `bookingUuid` | `booking_uuid` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 21 | `voidedAmount` | `voided_amount` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 22 | `voidReason` | `void_reason` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 23 | `voidedBy` | `voided_by` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 24 | `voidedAt` | `voided_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 25 | `voidedAtIso` | `voided_at_iso` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 26 | `hotelDayKey` | `hotel_day_key` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 27 | `reversalPaymentUuid` | `reversal_payment_uuid` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 28 | `approvedBy` | `approved_by` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 29 | `note` | `note` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 30 | `originalAmount` | `original_amount` | `RealColumn` | `REAL` | نعم | `` | لا |  |
| 31 | `paymentUuid` | `payment_uuid` | `TextColumn` | `TEXT` | نعم | `` | لا |  |

### `guest_infos` — `GuestInfos` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `roomNumber` | `room_number` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 19 | `guestName` | `guest_name` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 20 | `nationality` | `nationality` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 21 | `idNumber` | `id_number` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 22 | `idType` | `id_type` | `TextColumn` | `TEXT` | لا | `'بطاقة شخصية'` | لا |  |
| 23 | `issueDate` | `issue_date` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 24 | `issuePlace` | `issue_place` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 25 | `governorate` | `governorate` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 26 | `notes` | `notes` | `TextColumn` | `TEXT` | نعم | `` | لا |  |

### `auto_fix_runs` — `AutoFixRuns` (Flutter local-only)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 2 | `runUuid` | `run_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 3 | `source` | `source` | `TextColumn` | `TEXT` | لا | `'unknown'` | لا |  |
| 4 | `status` | `status` | `TextColumn` | `TEXT` | لا | `'pending'` | لا |  |
| 5 | `startedAtEpoch` | `started_at_epoch` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 6 | `startedAtIso` | `started_at_iso` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 7 | `completedAtEpoch` | `completed_at_epoch` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 8 | `completedAtIso` | `completed_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `fixesApplied` | `fixes_applied` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 10 | `errorMessage` | `error_message` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 11 | `metadata` | `metadata` | `TextColumn` | `TEXT` | نعم | `` | لا |  |

### `integrity_violations` — `IntegrityViolations` (Flutter local-only)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 2 | `runId` | `run_id` | `IntColumn` | `INTEGER` | لا | `` | لا | `AutoFixRuns.id` |
| 3 | `affectedTableName` | `affected_table_name` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 4 | `recordUuid` | `record_uuid` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 5 | `violationType` | `violation_type` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 6 | `details` | `details` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 7 | `isCritical` | `is_critical` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |
| 8 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 9 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `` | لا |  |

### `app_sessions` — `AppSessions` (Flutter local-only)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 2 | `sessionUuid` | `session_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 3 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 4 | `sessionStartIso` | `session_start_iso` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 5 | `sessionEndIso` | `session_end_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 6 | `durationSeconds` | `duration_seconds` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 7 | `lastKnownVersion` | `last_known_version` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `metadata` | `metadata` | `TextColumn` | `TEXT` | نعم | `` | لا |  |

### `salary_cycles` — `SalaryCycles` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `employeeId` | `employee_id` | `IntColumn` | `INTEGER` | لا | `` | لا | `Employees.id` |
| 19 | `cycleKey` | `cycle_key` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 20 | `hotelDayStart` | `hotel_day_start` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 21 | `hotelDayEnd` | `hotel_day_end` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 22 | `expectedAmount` | `expected_amount` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 23 | `actualPaid` | `actual_paid` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 24 | `remainingAmount` | `remaining_amount` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 25 | `status` | `status` | `TextColumn` | `TEXT` | لا | `'draft'` | لا |  |

**المفاتيح الفريدة المركبة المعلنة في Drift:** `[['employeeId', 'cycleKey']]`.

### `salary_payments` — `SalaryPayments` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `cycleId` | `cycle_id` | `IntColumn` | `INTEGER` | لا | `` | لا | `SalaryCycles.id` |
| 19 | `amount` | `amount` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 20 | `hotelDayKey` | `hotel_day_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 21 | `paymentDateIso` | `payment_date_iso` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 22 | `method` | `method` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 23 | `isAutoGenerated` | `is_auto_generated` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |

### `salary_withdrawals` — `SalaryWithdrawals` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `employeeId` | `employee_id` | `IntColumn` | `INTEGER` | لا | `` | لا | `Employees.id` |
| 19 | `amount` | `amount` | `RealColumn` | `REAL` | لا | `` | لا |  |
| 20 | `withdrawDate` | `withdraw_date` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 21 | `reason` | `reason` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 22 | `hotelDayKey` | `hotel_day_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 23 | `withdrawalType` | `withdrawal_type` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 24 | `description` | `description` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 25 | `expenseId` | `expense_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |

### `salary_carry_over_logs` — `SalaryCarryOverLogs` (D1/Worker sync)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 2 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 3 | `createdAt` | `created_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `updatedAt` | `updated_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 5 | `deletedAt` | `deleted_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `lastModified` | `last_modified` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 7 | `createdAtIso` | `created_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `updatedAtIso` | `updated_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `deletedAtIso` | `deleted_at_iso` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `createdAtEpoch` | `created_at_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 11 | `lastModifiedEpoch` | `last_modified_epoch` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 12 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 13 | `origin` | `origin` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 14 | `vectorClock` | `vector_clock` | `TextColumn` | `TEXT` | لا | `'{}'` | لا |  |
| 15 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `''` | لا |  |
| 16 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 17 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 18 | `employeeId` | `employee_id` | `IntColumn` | `INTEGER` | لا | `` | لا | `Employees.id` |
| 19 | `amount` | `amount` | `RealColumn` | `REAL` | لا | `` | لا |  |
| 20 | `previousCycleStart` | `previous_cycle_start` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 21 | `previousCycleEnd` | `previous_cycle_end` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 22 | `newCycleStart` | `new_cycle_start` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 23 | `newCycleEnd` | `new_cycle_end` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 24 | `reason` | `reason` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 25 | `carriedAt` | `carried_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |

### `outbox` — `Outbox` (Flutter local-only)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 2 | `entity` | `entity` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 3 | `op` | `op` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 4 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 5 | `serverId` | `server_id` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 6 | `payload` | `payload` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 7 | `clientTs` | `client_ts` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 8 | `attempts` | `attempts` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 9 | `lastError` | `last_error` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 10 | `idempotencyKey` | `idempotency_key` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 11 | `processingStatus` | `processing_status` | `TextColumn` | `TEXT` | لا | `'pending'` | لا |  |
| 12 | `processingStartedAt` | `processing_started_at` | `IntColumn` | `INTEGER` | نعم | `` | لا |  |
| 13 | `processingWorker` | `processing_worker` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 14 | `source` | `source` | `TextColumn` | `TEXT` | لا | `'local'` | لا |  |
| 15 | `deliveredToPrimary` | `delivered_to_primary` | `BoolColumn` | `INTEGER` | لا | `false` | لا |  |
| 16 | `deliveredToSecondary` | `delivered_to_secondary` | `BoolColumn` | `INTEGER` | لا | `true` | لا |  |

### `sync_state` — `SyncState` (Flutter local-only)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |
| 2 | `lastServerTs` | `last_server_ts` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 3 | `lastPullTs` | `last_pull_ts` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 4 | `lastPushTs` | `last_push_ts` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 5 | `isSyncing` | `is_syncing` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 6 | `version` | `version` | `IntColumn` | `INTEGER` | لا | `1` | لا |  |

### `restore_fix_log` — `RestoreFixLog` (Flutter local-only)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 2 | `fixId` | `fix_id` | `TextColumn` | `TEXT` | لا | `` | نعم |  |
| 3 | `executedAt` | `executed_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 4 | `targetTable` | `target_table` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 5 | `targetRecordId` | `target_record_id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 6 | `fieldName` | `field_name` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 7 | `oldValue` | `old_value` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 8 | `newValue` | `new_value` | `TextColumn` | `TEXT` | نعم | `` | لا |  |
| 9 | `reason` | `reason` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 10 | `fixType` | `fix_type` | `TextColumn` | `TEXT` | لا | `` | لا |  |

### `sync_queue` — `SyncQueue` (Flutter local-only)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 2 | `uuid` | `uuid` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 3 | `targetTable` | `table_name` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 4 | `operation` | `operation` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 5 | `payload` | `payload` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 6 | `updatedAt` | `updated_at` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 7 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 8 | `status` | `status` | `TextColumn` | `TEXT` | لا | `'pending'` | لا |  |
| 9 | `createdAt` | `created_at` | `TextColumn` | `TEXT` | لا | `` | لا |  |

### `sync_log` — `SyncLog` (Flutter local-only)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 2 | `syncId` | `sync_id` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 3 | `direction` | `direction` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 4 | `deviceId` | `device_id` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 5 | `metadata` | `metadata` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 6 | `operations` | `operations` | `TextColumn` | `TEXT` | نعم | `'[]'` | لا |  |
| 7 | `checksumMatched` | `checksum_matched` | `IntColumn` | `INTEGER` | لا | `0` | لا |  |
| 8 | `status` | `status` | `TextColumn` | `TEXT` | لا | `'success'` | لا |  |
| 9 | `createdAt` | `created_at` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 10 | `completedAt` | `completed_at` | `TextColumn` | `TEXT` | نعم | `` | لا |  |

### `sync_conflicts` — `SyncConflicts` (Flutter local-only)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 2 | `logId` | `log_id` | `IntColumn` | `INTEGER` | لا | `` | لا | `SyncLog.id` |
| 3 | `targetTable` | `table_name` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 4 | `uuid` | `uuid` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 5 | `resolution` | `resolution` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 6 | `localPayload` | `local_payload` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 7 | `remotePayload` | `remote_payload` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 8 | `createdAt` | `created_at` | `TextColumn` | `TEXT` | لا | `` | لا |  |

### `ancestor_cache` — `AncestorCache` (Flutter local-only)

| # | Dart field | SQL column | Drift type | SQL affinity | Null | Default | Unique | FK |
|---:|---|---|---|---|---|---|---|---|
| 1 | `id` | `id` | `IntColumn` | `INTEGER` | لا | `` | لا |  |
| 2 | `entity` | `entity` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 3 | `localUuid` | `local_uuid` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 4 | `dataJson` | `data_json` | `TextColumn` | `TEXT` | لا | `` | لا |  |
| 5 | `capturedAt` | `captured_at` | `IntColumn` | `INTEGER` | لا | `` | لا |  |

**المفاتيح الفريدة المركبة المعلنة في Drift:** `[['entity', 'localUuid']]`.

## فجوات التكامل المؤكدة

| الفجوة | الدليل | الأثر | القرار المنفذ/الموصى به |
|---|---|---|---|
| مخطط D1 السابق احتوى على 5 كيانات تشغيلية فقط تقريباً، بينما Worker يعلن 20 كياناً. | `database.ts` و`cloudflare_config.dart` مقابل `worker/schema.sql`. | عمليات push لكيانات مثل debts وsalary_* وguest_infos تفشل إذا لم توجد جداولها. | توليد مخطط D1 كاملاً لكل كيانات Worker مع بنية Drift. |
| `id` و`server_id` في مخطط D1 السابق لا يطابقان Drift. | المخطط السابق يستخدم `id TEXT PRIMARY KEY`، بينما Drift يستخدم `id INTEGER AUTOINCREMENT` و`serverId INTEGER`. | create/update والمطابقة عبر local_uuid لا تعمل بشكل موحد. | اعتماد INTEGER AUTOINCREMENT لـ id وINTEGER لـ server_id، مع `local_uuid TEXT UNIQUE`. |
| حقول SyncFields مفقودة من المخطط السابق. | تعريف `SyncFields` في local_db.dart. | pull/push وPRAGMA filtering لا يملكان العقد الكامل. | إضافة local_uuid وcreated/updated/last_modified والحقول الزمنية وversion/vector_clock/device/origin/idempotency_key. |
| Worker يعتمد على `rate_limits` لكنه لم يكن معرفاً في schema السابق. | `worker/src/index.ts`، `checkRateLimit`. | أول طلب API قد يفشل أو يعمل fail-open دائماً. | إضافة جدول rate_limits بمفتاح مركب client_id/window_start. |
| Worker ينشئ devices وقت التشغيل فقط. | `worker/src/database.ts`, registerDevice. | schema غير مكتمل في بيئة جديدة. | إضافة devices إلى DDL الأساسي. |
| `hotel_day_ledger` موجود في Worker mapping لكنه مستبعد من CloudflareConfig migrationOrder. | `cloudflare_config.dart`. | خطر اعتبار جدول محلياً/بعيداً في آن واحد. | إبقاؤه في DDL للتوافق، وتصنيفه صراحةً محلياً في Flutter إلى أن يُتخذ قرار مزامنة مستقل. |
| cursor الحالي رقم timestamp فقط. | `database.ts` pullChanges و`cloudflare_sync_manager.dart`. | عدة سجلات بنفس updated_at مع LIMIT قد تسبب تخطي سجلات. | يلزم لاحقاً cursor مركب `(updated_at, entity, id/local_uuid)` أو monotonic server sequence؛ لا يجوز اعتبار timestamp وحده ضماناً. |
| `updateRecord` يضم حقول data دون filter مقابل PRAGMA. | `worker/src/database.ts`. | قد يفشل تحديث بسبب حقول غير موجودة أو `_entity`. | يجب تطبيق نفس column whitelist على update قبل اعتماد الإنتاج. |

## مسار التكامل التشغيلي

يبدأ Flutter بتسجيل الدخول عبر `POST /api/auth/login`، ثم يرسل التسجيلات المتراكمة من Drift `outbox` إلى `POST /api/sync/push` في دفعات gzip، حيث يحتوي كل عنصر على `idempotencyKey` و`entity` و`operation` و`data` و`vectorClock` و`updatedAt`. يعيد Worker نتائج لكل عنصر، ثم يحذف Flutter العنصر الناجح من outbox ويعيد المحاولة في حالات الشبكة أو الأخطاء المؤقتة.

يسحب Flutter التغييرات عبر `GET /api/sync/pull?cursor=...&limit=...`، ويطابق السجل بواسطة `local_uuid`، ويطبق soft delete عبر `deleted_at`. عند تعارض vector clock، يطبق Flutter محلل التعارض محلياً وقد يعيد النتيجة إلى outbox. لذلك يجب أن تكون أسماء الأعمدة وقيم Boolean والطوابع الزمنية متطابقة بين Drift وD1، وإلا سيظهر فشل مزامنة صامت أو divergence.

## خطوات تشغيل D1

يُنفّذ المخطط من مجلد Worker باستخدام الأمر الموجود في `worker/package.json`: `npm run db:init`. يجب تشغيله أولاً على بيئة preview أو قاعدة جديدة، ثم التحقق بواسطة `PRAGMA table_info(<table>)` و`PRAGMA foreign_key_list(<table>)` ومقارنة عدد الجداول مع هذا التقرير قبل تشغيل migration الإنتاجية.

## مراجع محلية

الأرقام في هذا التقرير تشير إلى ملفات المصدر ومساراتها داخل المستودع المحلي؛ لا توجد ادعاءات خارجية تحتاج إلى مصدر ويب. التقرير نفسه مولد من `tools/generate_orax_d1_artifacts.py`، بينما JSON الخام هو `tools/orax_schema_extracted.json`.
