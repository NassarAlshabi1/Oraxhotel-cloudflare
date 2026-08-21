# التدقيق المرجعي لنسخة الكمبيوتر Oraxhotel

> مصدر الحقيقة لهذا التقرير هو ملف SQL Server الأصلي المضمّن في نسخة الكمبيوتر، وليس مخطط Flutter الحالي أو مخطط D1 السابق.

## المصدر المرجعي

| العنصر | القيمة |
|---|---|
| قاعدة البيانات | `Hotel_alkheer` |
| المحرك | `Microsoft SQL Server` |
| ملف المصدر | `HotelSys/database/Hotel_alkheer_init.sql` |
| الجداول | **47** |
| الحقول | **378** |
| المفاتيح الأساسية | مستخرجة من قيود `PRIMARY KEY` لكل جدول |

## خلاصة مقارنة المصادر

| المصدر | عدد الجداول | المعنى الهندسي |
|---|---:|---|
| نسخة الكمبيوتر SQL Server | 47 | المصدر المرجعي الأول الذي يجب أن تُبنى عليه المواءمة. |
| Flutter/Drift الحالي | 30 | نسخة تطبيق محلي ومخطط تشغيل/مزامنة؛ لا يُعتبر بديلاً عن مخطط الكمبيوتر. |
| Worker/D1 الحالي | 26 | مخطط تكامل سابق/مستهدف؛ يجب إعادة تصميمه بعد اعتماد مخطط الكمبيوتر. |

## قواعد التحويل المرشحة إلى D1

| SQL Server في الكمبيوتر | Cloudflare D1/SQLite | ضابط التحويل |
|---|---|---|
| `int`, `bigint` | `INTEGER` | تُحفظ القيمة الرقمية دون تحويل نصي. |
| `float` | `REAL` | يجب فحص الدقة المالية؛ المبالغ الحساسة تحتاج قراراً منفصلاً بشأن minor units أو NUMERIC-as-text. |
| `bit` | `INTEGER` بقيمتي 0/1 | Flutter/Dart يجب أن يحول Boolean إلى 0/1. |
| `date`, `datetime` | `TEXT` بصيغة ISO-8601 أو `INTEGER` epoch | يجب اختيار صيغة موحدة قبل المزامنة؛ لا يجوز الخلط بين الصيغ. |
| `nvarchar(n/max)` | `TEXT` | حد الطول يُحفظ كقاعدة تحقق في التطبيق إن كان مهماً. |
| `IDENTITY(1,1)` | `INTEGER PRIMARY KEY` | أثناء migration يجب الحفاظ على IDs القديمة؛ لا نعيد توليدها. |

## الجداول والحقول المستخرجة بالكامل

### `account_table`

المخطط: `dbo` · عدد الحقول: **7** · المفتاح الأساسي: `id` · سطر المصدر: 26

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 2 | `name` | `NVARCHAR(100)` | `NVARCHAR` | لا | لا | `` | `nvarchar(100) NOT NULL` |
| 3 | `status` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 4 | `is_private` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 5 | `createat` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 6 | `id_group` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 7 | `code` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `admin_table`

المخطط: `dbo` · عدد الحقول: **7** · المفتاح الأساسي: `id` · سطر المصدر: 41

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `BIGINT` | `BIGINT` | لا | نعم | `` | `bigint IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(70)` | `NVARCHAR` | لا | لا | `` | `nvarchar(70) NOT NULL` |
| 3 | `username` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 4 | `password` | `NVARCHAR(200)` | `NVARCHAR` | لا | لا | `` | `nvarchar(200) NOT NULL` |
| 5 | `status` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 6 | `lastdate_login` | `DATE` | `DATE` | نعم | لا | `` | `date NULL` |
| 7 | `adminid` | `BIGINT` | `BIGINT` | نعم | لا | `` | `bigint NULL` |

### `area_table`

المخطط: `dbo` · عدد الحقول: **7** · المفتاح الأساسي: `id` · سطر المصدر: 56

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(200)` | `NVARCHAR` | لا | لا | `` | `nvarchar(200) NOT NULL` |
| 3 | `id_city` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 4 | `name_en` | `NVARCHAR(200)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(200) NULL` |
| 5 | `name_ar_tashkeel` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 6 | `name_ar_normalized` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 7 | `name_en_normalized` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |

### `AspNetRoles`

المخطط: `dbo` · عدد الحقول: **4** · المفتاح الأساسي: `Id` · سطر المصدر: 71

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `Id` | `NVARCHAR(450)` | `NVARCHAR` | لا | لا | `` | `nvarchar(450) NOT NULL` |
| 2 | `Name` | `NVARCHAR(256)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(256) NULL` |
| 3 | `NormalizedName` | `NVARCHAR(256)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(256) NULL` |
| 4 | `ConcurrencyStamp` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |

### `AspNetRoleClaims`

المخطط: `dbo` · عدد الحقول: **4** · المفتاح الأساسي: `Id` · سطر المصدر: 83

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `Id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `RoleId` | `NVARCHAR(450)` | `NVARCHAR` | لا | لا | `` | `nvarchar(450) NOT NULL` |
| 3 | `ClaimType` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 4 | `ClaimValue` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |

### `AspNetUsers`

المخطط: `dbo` · عدد الحقول: **17** · المفتاح الأساسي: `Id` · سطر المصدر: 95

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `Id` | `NVARCHAR(450)` | `NVARCHAR` | لا | لا | `` | `nvarchar(450) NOT NULL` |
| 2 | `UserName` | `NVARCHAR(256)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(256) NULL` |
| 3 | `NormalizedUserName` | `NVARCHAR(256)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(256) NULL` |
| 4 | `Email` | `NVARCHAR(256)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(256) NULL` |
| 5 | `NormalizedEmail` | `NVARCHAR(256)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(256) NULL` |
| 6 | `EmailConfirmed` | `BIT` | `BIT` | لا | لا | `` | `bit NOT NULL` |
| 7 | `PasswordHash` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 8 | `SecurityStamp` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 9 | `ConcurrencyStamp` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 10 | `PhoneNumber` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 11 | `PhoneNumberConfirmed` | `BIT` | `BIT` | لا | لا | `` | `bit NOT NULL` |
| 12 | `TwoFactorEnabled` | `BIT` | `BIT` | لا | لا | `` | `bit NOT NULL` |
| 13 | `LockoutEnd` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 14 | `LockoutEnabled` | `BIT` | `BIT` | لا | لا | `` | `bit NOT NULL` |
| 15 | `AccessFailedCount` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 16 | `FirstName` | `NVARCHAR(100)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(100) NULL` |
| 17 | `LastName` | `NVARCHAR(100)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(100) NULL` |

### `AspNetUserClaims`

المخطط: `dbo` · عدد الحقول: **4** · المفتاح الأساسي: `Id` · سطر المصدر: 120

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `Id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `UserId` | `NVARCHAR(450)` | `NVARCHAR` | لا | لا | `` | `nvarchar(450) NOT NULL` |
| 3 | `ClaimType` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 4 | `ClaimValue` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |

### `AspNetUserLogins`

المخطط: `dbo` · عدد الحقول: **4** · المفتاح الأساسي: `LoginProvider`, `ProviderKey` · سطر المصدر: 132

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `LoginProvider` | `NVARCHAR(128)` | `NVARCHAR` | لا | لا | `` | `nvarchar(128) NOT NULL` |
| 2 | `ProviderKey` | `NVARCHAR(128)` | `NVARCHAR` | لا | لا | `` | `nvarchar(128) NOT NULL` |
| 3 | `ProviderDisplayName` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 4 | `UserId` | `NVARCHAR(450)` | `NVARCHAR` | لا | لا | `` | `nvarchar(450) NOT NULL` |

### `AspNetUserRoles`

المخطط: `dbo` · عدد الحقول: **2** · المفتاح الأساسي: `UserId`, `RoleId` · سطر المصدر: 144

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `UserId` | `NVARCHAR(450)` | `NVARCHAR` | لا | لا | `` | `nvarchar(450) NOT NULL` |
| 2 | `RoleId` | `NVARCHAR(450)` | `NVARCHAR` | لا | لا | `` | `nvarchar(450) NOT NULL` |

### `AspNetUserTokens`

المخطط: `dbo` · عدد الحقول: **4** · المفتاح الأساسي: `UserId`, `LoginProvider`, `Name` · سطر المصدر: 154

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `UserId` | `NVARCHAR(450)` | `NVARCHAR` | لا | لا | `` | `nvarchar(450) NOT NULL` |
| 2 | `LoginProvider` | `NVARCHAR(128)` | `NVARCHAR` | لا | لا | `` | `nvarchar(128) NOT NULL` |
| 3 | `Name` | `NVARCHAR(128)` | `NVARCHAR` | لا | لا | `` | `nvarchar(128) NOT NULL` |
| 4 | `Value` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |

### `bank_table`

المخطط: `dbo` · عدد الحقول: **5** · المفتاح الأساسي: `id` · سطر المصدر: 166

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(300)` | `NVARCHAR` | لا | لا | `` | `nvarchar(300) NOT NULL` |
| 3 | `is_default` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 4 | `id_account` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 5 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `bills_table`

المخطط: `dbo` · عدد الحقول: **27** · المفتاح الأساسي: `id` · سطر المصدر: 179

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `BIGINT` | `BIGINT` | لا | نعم | `` | `bigint IDENTITY(1,1) NOT NULL` |
| 2 | `type` | `NVARCHAR(20)` | `NVARCHAR` | لا | لا | `` | `nvarchar(20) NOT NULL` |
| 3 | `type_pay` | `NVARCHAR(20)` | `NVARCHAR` | لا | لا | `` | `nvarchar(20) NOT NULL` |
| 4 | `num_reference` | `NVARCHAR(100)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(100) NULL` |
| 5 | `date` | `DATETIME` | `DATETIME` | لا | لا | `` | `datetime NOT NULL` |
| 6 | `total` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 7 | `is_for_room` | `BIT` | `BIT` | لا | لا | `` | `bit NOT NULL` |
| 8 | `deserve_amount` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 9 | `type_discount` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 10 | `qty_discount` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 11 | `pay_amount` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 12 | `rest_amount` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 13 | `num_check` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 14 | `num_card` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 15 | `note` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 16 | `createat` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 17 | `id_account` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 18 | `id_reception` | `BIGINT` | `BIGINT` | نعم | لا | `` | `bigint NULL` |
| 19 | `id_bank` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 20 | `customer_or_company` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 21 | `id_currancy` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 22 | `total_tax_price` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 23 | `total_tax_rate` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 24 | `include_tax` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 25 | `total_baladi_tax_price` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 26 | `total_baladi_tax_rate` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 27 | `is_baladi_tax` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |

### `bond_table`

المخطط: `dbo` · عدد الحقول: **22** · المفتاح الأساسي: `id` · سطر المصدر: 214

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `BIGINT` | `BIGINT` | لا | نعم | `` | `bigint IDENTITY(1,1) NOT NULL` |
| 2 | `type` | `NVARCHAR(20)` | `NVARCHAR` | لا | لا | `` | `nvarchar(20) NOT NULL` |
| 3 | `type_pay` | `NVARCHAR(20)` | `NVARCHAR` | لا | لا | `` | `nvarchar(20) NOT NULL` |
| 4 | `num_reference` | `NVARCHAR(100)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(100) NULL` |
| 5 | `date` | `DATETIME` | `DATETIME` | لا | لا | `` | `datetime NOT NULL` |
| 6 | `amount` | `FLOAT` | `FLOAT` | لا | لا | `` | `float NOT NULL` |
| 7 | `loc_pay` | `NVARCHAR(300)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(300) NULL` |
| 8 | `worthy_date` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 9 | `why` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 10 | `hand` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 11 | `num_check` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 12 | `num_card` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 13 | `note` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 14 | `createat` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 15 | `is_done_pay` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 16 | `id_bond_pay` | `BIGINT` | `BIGINT` | نعم | لا | `` | `bigint NULL` |
| 17 | `id_account` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 18 | `id_reception` | `BIGINT` | `BIGINT` | نعم | لا | `` | `bigint NULL` |
| 19 | `id_item_expenses` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 20 | `id_bank` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 21 | `id_currancy` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 22 | `time` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |

### `boxs_table`

المخطط: `dbo` · عدد الحقول: **6** · المفتاح الأساسي: `id` · سطر المصدر: 244

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(300)` | `NVARCHAR` | لا | لا | `` | `nvarchar(300) NOT NULL` |
| 3 | `is_default` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 4 | `id_account` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 5 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 6 | `is_private` | `BIT` | `BIT` | لا | لا | `` | `bit NOT NULL` |

### `boxs_user_table`

المخطط: `dbo` · عدد الحقول: **4** · المفتاح الأساسي: `id` · سطر المصدر: 258

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `id_box` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 3 | `id_aspUser` | `NVARCHAR(450)` | `NVARCHAR` | لا | لا | `` | `nvarchar(450) NOT NULL` |
| 4 | `is_defult` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |

### `change_room_table`

المخطط: `dbo` · عدد الحقول: **9** · المفتاح الأساسي: `id` · سطر المصدر: 270

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `id_room_from` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 3 | `id_room_to` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 4 | `why` | `NVARCHAR(MAX)` | `NVARCHAR` | لا | لا | `` | `nvarchar(max) NOT NULL` |
| 5 | `date` | `DATETIME` | `DATETIME` | لا | لا | `` | `datetime NOT NULL` |
| 6 | `price_old` | `FLOAT` | `FLOAT` | لا | لا | `` | `float NOT NULL` |
| 7 | `price_current` | `FLOAT` | `FLOAT` | لا | لا | `` | `float NOT NULL` |
| 8 | `id_receptoin` | `BIGINT` | `BIGINT` | لا | لا | `` | `bigint NOT NULL` |
| 9 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `city_table`

المخطط: `dbo` · عدد الحقول: **4** · المفتاح الأساسي: `id` · سطر المصدر: 287

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(MAX)` | `NVARCHAR` | لا | لا | `` | `nvarchar(max) NOT NULL` |
| 3 | `id_country` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 4 | `name_en` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |

### `company_table`

المخطط: `dbo` · عدد الحقول: **4** · المفتاح الأساسي: `id` · سطر المصدر: 299

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 3 | `id_account` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 4 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `condition_reception_table`

المخطط: `dbo` · عدد الحقول: **4** · المفتاح الأساسي: `id` · سطر المصدر: 311

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(MAX)` | `NVARCHAR` | لا | لا | `` | `nvarchar(max) NOT NULL` |
| 3 | `num` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 4 | `id_sub` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |

### `country_table`

المخطط: `dbo` · عدد الحقول: **3** · المفتاح الأساسي: `id` · سطر المصدر: 323

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(200)` | `NVARCHAR` | لا | لا | `` | `nvarchar(200) NOT NULL` |
| 3 | `name_en` | `NVARCHAR(200)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(200) NULL` |

### `currency_table`

المخطط: `dbo` · عدد الحقول: **6** · المفتاح الأساسي: `id` · سطر المصدر: 334

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(300)` | `NVARCHAR` | لا | لا | `` | `nvarchar(300) NOT NULL` |
| 3 | `is_default` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 4 | `code` | `NVARCHAR(5)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(5) NULL` |
| 5 | `rate_convert` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 6 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `customer_table`

المخطط: `dbo` · عدد الحقول: **18** · المفتاح الأساسي: `id` · سطر المصدر: 348

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `BIGINT` | `BIGINT` | لا | نعم | `` | `bigint IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(MAX)` | `NVARCHAR` | لا | لا | `` | `nvarchar(max) NOT NULL` |
| 3 | `type` | `NVARCHAR(100)` | `NVARCHAR` | لا | لا | `` | `nvarchar(100) NOT NULL` |
| 4 | `sex` | `NVARCHAR(10)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(10) NULL` |
| 5 | `email` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 6 | `nationality` | `NVARCHAR(300)` | `NVARCHAR` | لا | لا | `` | `nvarchar(300) NOT NULL` |
| 7 | `type_work` | `NVARCHAR(100)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(100) NULL` |
| 8 | `loc_work` | `NVARCHAR(100)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(100) NULL` |
| 9 | `phone_work` | `NVARCHAR(100)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(100) NULL` |
| 10 | `type_proof` | `NVARCHAR(30)` | `NVARCHAR` | لا | لا | `` | `nvarchar(30) NOT NULL` |
| 11 | `num_proof` | `NVARCHAR(300)` | `NVARCHAR` | لا | لا | `` | `nvarchar(300) NOT NULL` |
| 12 | `release_date` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 13 | `end_date` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 14 | `loc_release` | `NVARCHAR(300)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(300) NULL` |
| 15 | `createat` | `DATETIME` | `DATETIME` | لا | لا | `` | `datetime NOT NULL` |
| 16 | `public_note` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 17 | `id_area` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 18 | `id_nationality` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `detials_bills_table`

المخطط: `dbo` · عدد الحقول: **11** · المفتاح الأساسي: `id` · سطر المصدر: 374

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `BIGINT` | `BIGINT` | لا | نعم | `` | `bigint IDENTITY(1,1) NOT NULL` |
| 2 | `qty` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 3 | `price_one` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 4 | `total` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 5 | `id_bill` | `BIGINT` | `BIGINT` | لا | لا | `` | `bigint NOT NULL` |
| 6 | `id_product` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 7 | `tax_price` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 8 | `tax_rate` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 9 | `baladi_tax_price` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 10 | `baladi_tax_rate` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 11 | `is_baladi_tax` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |

### `detials_hotel_table`

المخطط: `dbo` · عدد الحقول: **4** · المفتاح الأساسي: `id` · سطر المصدر: 393

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `count_room` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 3 | `count_floot` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 4 | `id_ho` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |

### `detials_status_table`

المخطط: `dbo` · عدد الحقول: **11** · المفتاح الأساسي: `id` · سطر المصدر: 405

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `BIGINT` | `BIGINT` | لا | نعم | `` | `bigint IDENTITY(1,1) NOT NULL` |
| 2 | `status` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 3 | `id_room` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 4 | `detials` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 5 | `start_date` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 6 | `end_date` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 7 | `id_reception` | `BIGINT` | `BIGINT` | نعم | لا | `` | `bigint NULL` |
| 8 | `id_emp` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 9 | `createat` | `DATETIME` | `DATETIME` | لا | لا | `` | `datetime NOT NULL` |
| 10 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 11 | `id_status_before` | `BIGINT` | `BIGINT` | نعم | لا | `` | `bigint NULL` |

### `emp_table`

المخطط: `dbo` · عدد الحقول: **8** · المفتاح الأساسي: `id` · سطر المصدر: 424

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 3 | `img` | `NVARCHAR(300)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(300) NULL` |
| 4 | `phone` | `NVARCHAR(15)` | `NVARCHAR` | لا | لا | `` | `nvarchar(15) NOT NULL` |
| 5 | `email` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 6 | `sex` | `NVARCHAR(10)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(10) NULL` |
| 7 | `num_identity` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 8 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `entries_acc_table`

المخطط: `dbo` · عدد الحقول: **12** · المفتاح الأساسي: `id` · سطر المصدر: 440

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `BIGINT` | `BIGINT` | لا | نعم | `` | `bigint IDENTITY(1,1) NOT NULL` |
| 2 | `debt_or_Credit` | `NVARCHAR(1)` | `NVARCHAR` | لا | لا | `` | `nvarchar(1) NOT NULL` |
| 3 | `amount` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 4 | `bill_or_band` | `NVARCHAR(10)` | `NVARCHAR` | لا | لا | `` | `nvarchar(10) NOT NULL` |
| 5 | `id_document_dand` | `BIGINT` | `BIGINT` | نعم | لا | `` | `bigint NULL` |
| 6 | `id_document_bill` | `BIGINT` | `BIGINT` | نعم | لا | `` | `bigint NULL` |
| 7 | `type_document` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 8 | `id_account` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 9 | `id_currancy` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 10 | `date` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 11 | `id_recetion` | `BIGINT` | `BIGINT` | نعم | لا | `` | `bigint NULL` |
| 12 | `note` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |

### `follower_reception_table`

المخطط: `dbo` · عدد الحقول: **8** · المفتاح الأساسي: `id` · سطر المصدر: 460

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `BIGINT` | `BIGINT` | لا | نعم | `` | `bigint IDENTITY(1,1) NOT NULL` |
| 2 | `cu_type` | `NVARCHAR(10)` | `NVARCHAR` | لا | لا | `` | `nvarchar(10) NOT NULL` |
| 3 | `relation` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 4 | `id_receptoin` | `BIGINT` | `BIGINT` | لا | لا | `` | `bigint NOT NULL` |
| 5 | `id_customer` | `BIGINT` | `BIGINT` | لا | لا | `` | `bigint NOT NULL` |
| 6 | `duration` | `NVARCHAR(1)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(1) NULL` |
| 7 | `duration_from` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 8 | `duration_to` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |

### `group_account_table`

المخطط: `dbo` · عدد الحقول: **6** · المفتاح الأساسي: `id` · سطر المصدر: 476

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 2 | `name` | `NVARCHAR(100)` | `NVARCHAR` | لا | لا | `` | `nvarchar(100) NOT NULL` |
| 3 | `id_main_group` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 4 | `is_root` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 5 | `is_private` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 6 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `group_services_table`

المخطط: `dbo` · عدد الحقول: **4** · المفتاح الأساسي: `id` · سطر المصدر: 490

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(MAX)` | `NVARCHAR` | لا | لا | `` | `nvarchar(max) NOT NULL` |
| 3 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 4 | `name_en` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |

### `hotels_branch_table`

المخطط: `dbo` · عدد الحقول: **16** · المفتاح الأساسي: `id` · سطر المصدر: 502

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name_h` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 3 | `num_en` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 4 | `country` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 5 | `city` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 6 | `regin` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 7 | `address` | `NVARCHAR(150)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(150) NULL` |
| 8 | `email` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 9 | `phone` | `NVARCHAR(MAX)` | `NVARCHAR` | لا | لا | `` | `nvarchar(max) NOT NULL` |
| 10 | `website` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 11 | `mail_box` | `NVARCHAR(150)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(150) NULL` |
| 12 | `logo` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 13 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 14 | `id_country` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 15 | `id_org` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 16 | `count_floot` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `items_expenses_table`

المخطط: `dbo` · عدد الحقول: **5** · المفتاح الأساسي: `id` · سطر المصدر: 526

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(300)` | `NVARCHAR` | لا | لا | `` | `nvarchar(300) NOT NULL` |
| 3 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 4 | `id_account` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 5 | `create_at` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |

### `jobs_name_table`

المخطط: `dbo` · عدد الحقول: **3** · المفتاح الأساسي: `id` · سطر المصدر: 539

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(300)` | `NVARCHAR` | لا | لا | `` | `nvarchar(300) NOT NULL` |
| 3 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `jop_emp_table`

المخطط: `dbo` · عدد الحقول: **3** · المفتاح الأساسي: `id` · سطر المصدر: 550

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `id_emp` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 3 | `id_job_name` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |

### `my_customers`

المخطط: `dbo` · عدد الحقول: **7** · المفتاح الأساسي: `id` · سطر المصدر: 561

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `BIGINT` | `BIGINT` | لا | نعم | `` | `bigint IDENTITY(1,1) NOT NULL` |
| 2 | `id_customer` | `BIGINT` | `BIGINT` | لا | لا | `` | `bigint NOT NULL` |
| 3 | `idsub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 4 | `private_note` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 5 | `id_account` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 6 | `createat` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 7 | `visit_end_date` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |

### `orgs_table`

المخطط: `dbo` · عدد الحقول: **15** · المفتاح الأساسي: `id` · سطر المصدر: 576

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name_h` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 3 | `num_en` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 4 | `country` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 5 | `city` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 6 | `regin` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 7 | `address` | `NVARCHAR(150)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(150) NULL` |
| 8 | `email` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 9 | `phone` | `NVARCHAR(MAX)` | `NVARCHAR` | لا | لا | `` | `nvarchar(max) NOT NULL` |
| 10 | `website` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 11 | `mail_box` | `NVARCHAR(150)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(150) NULL` |
| 12 | `logo` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 13 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 14 | `id_country` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 15 | `tax_num` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |

### `overtime_table`

المخطط: `dbo` · عدد الحقول: **8** · المفتاح الأساسي: `id` · سطر المصدر: 599

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `start_date` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 3 | `end_date` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 4 | `start_time` | `NVARCHAR(MAX)` | `NVARCHAR` | لا | لا | `` | `nvarchar(max) NOT NULL` |
| 5 | `end_time` | `NVARCHAR(MAX)` | `NVARCHAR` | لا | لا | `` | `nvarchar(max) NOT NULL` |
| 6 | `createat` | `DATETIME` | `DATETIME` | لا | لا | `` | `datetime NOT NULL` |
| 7 | `id_user` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 8 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `price_rooms_table`

المخطط: `dbo` · عدد الحقول: **7** · المفتاح الأساسي: `id` · سطر المصدر: 615

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `price` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 3 | `price_overtime` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 4 | `price_min` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 5 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 6 | `id_room` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 7 | `id_tax_group` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `product_table`

المخطط: `dbo` · عدد الحقول: **6** · المفتاح الأساسي: `id` · سطر المصدر: 630

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(MAX)` | `NVARCHAR` | لا | لا | `` | `nvarchar(max) NOT NULL` |
| 3 | `id_group` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 4 | `name_en` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 5 | `price` | `FLOAT` | `FLOAT` | لا | لا | `` | `float NOT NULL` |
| 6 | `id_tax_group` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `recetion_table`

المخطط: `dbo` · عدد الحقول: **19** · المفتاح الأساسي: `id` · سطر المصدر: 644

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `BIGINT` | `BIGINT` | لا | نعم | `` | `bigint IDENTITY(1,1) NOT NULL` |
| 2 | `source` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 3 | `price` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |
| 4 | `qty_time` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 5 | `unit` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 6 | `start_date` | `DATETIME` | `DATETIME` | لا | لا | `` | `datetime NOT NULL` |
| 7 | `end_date` | `DATETIME` | `DATETIME` | لا | لا | `` | `datetime NOT NULL` |
| 8 | `type_date` | `NVARCHAR(1)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(1) NULL` |
| 9 | `is_chechin` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 10 | `checkin_date` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 11 | `is_chechout` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 12 | `chechout_date` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 13 | `id_room` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 14 | `note` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 15 | `id_co` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 16 | `id_my_customer` | `BIGINT` | `BIGINT` | نعم | لا | `` | `bigint NULL` |
| 17 | `status` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 18 | `why_visit` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 19 | `area_from` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |

### `rooms_table`

المخطط: `dbo` · عدد الحقول: **15** · المفتاح الأساسي: `id` · سطر المصدر: 671

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name_r` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 3 | `num_floor` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 4 | `count_rooms` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 5 | `count_bed_single` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 6 | `count_bed_double` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 7 | `count_bathroom` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 8 | `count_tv` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 9 | `count_wallet` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 10 | `type_condition` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 11 | `public_features` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 12 | `private_features` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 13 | `note` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 14 | `id_ho` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 15 | `id_type` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |

### `setting_general_table`

المخطط: `dbo` · عدد الحقول: **3** · المفتاح الأساسي: `id` · سطر المصدر: 694

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 2 | `services_include_tax` | `BIT` | `BIT` | لا | لا | `` | `bit NOT NULL` |
| 3 | `enable_tax_num` | `BIT` | `BIT` | لا | لا | `` | `bit NOT NULL` |

### `setting_reception_table`

المخطط: `dbo` · عدد الحقول: **10** · المفتاح الأساسي: `id` · سطر المصدر: 705

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `is_checkin_time` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 3 | `time_checkin` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 4 | `is_checkout_time` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 5 | `time_checkout` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 6 | `is_interval_changeroom` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 7 | `interval_changeroom` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 8 | `is_mounth_reception_checkout` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |
| 9 | `id_sub` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 10 | `incude_price_tax` | `BIT` | `BIT` | نعم | لا | `` | `bit NULL` |

### `status_current_table`

المخطط: `dbo` · عدد الحقول: **9** · المفتاح الأساسي: `id` · سطر المصدر: 723

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `status` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 3 | `id_room` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 4 | `start_date` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 5 | `end_date` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |
| 6 | `id_reception` | `BIGINT` | `BIGINT` | نعم | لا | `` | `bigint NULL` |
| 7 | `id_emp` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 8 | `createat` | `DATETIME` | `DATETIME` | نعم | لا | `` | `datetime NULL` |
| 9 | `detials` | `NVARCHAR(MAX)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(max) NULL` |

### `tax_group_table`

المخطط: `dbo` · عدد الحقول: **7** · المفتاح الأساسي: `id` · سطر المصدر: 740

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name` | `NVARCHAR(MAX)` | `NVARCHAR` | لا | لا | `` | `nvarchar(max) NOT NULL` |
| 3 | `name_en` | `NVARCHAR(200)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(200) NULL` |
| 4 | `rate` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |
| 5 | `id_user` | `INT` | `INT` | نعم | لا | `` | `int NULL` |
| 6 | `is_baladi_tax` | `BIT` | `BIT` | لا | لا | `` | `bit NOT NULL` |
| 7 | `baladi_rate` | `FLOAT` | `FLOAT` | نعم | لا | `` | `float NULL` |

### `type_rooms_table`

المخطط: `dbo` · عدد الحقول: **4** · المفتاح الأساسي: `id` · سطر المصدر: 755

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `name_t` | `NVARCHAR(MAX)` | `NVARCHAR` | لا | لا | `` | `nvarchar(max) NOT NULL` |
| 3 | `color` | `NVARCHAR(50)` | `NVARCHAR` | نعم | لا | `` | `nvarchar(50) NULL` |
| 4 | `id_sub` | `INT` | `INT` | نعم | لا | `` | `int NULL` |

### `user_table`

المخطط: `dbo` · عدد الحقول: **5** · المفتاح الأساسي: `id` · سطر المصدر: 767

| # | الحقل | نوع SQL Server | النوع الأساسي | Null | Identity | Default | التعريف |
|---:|---|---|---|---|---|---|---|
| 1 | `id` | `INT` | `INT` | لا | نعم | `` | `int IDENTITY(1,1) NOT NULL` |
| 2 | `username` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 3 | `password` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 4 | `role` | `NVARCHAR(50)` | `NVARCHAR` | لا | لا | `` | `nvarchar(50) NOT NULL` |
| 5 | `id_sub` | `INT` | `INT` | لا | لا | `` | `int NOT NULL` |

## قرار المصدر قبل التنفيذ

لا يجوز توليد D1 النهائي من جداول Flutter الحالية قبل عمل mapping موثق من جداول الكمبيوتر. بعض الجداول لها تشابه اسمي أو وظيفي جزئي فقط؛ على سبيل المثال `rooms_table` في نسخة الكمبيوتر يحتوي خصائص نوع/تكوين الغرف، بينما `rooms` في Flutter يمثل سجل غرفة تشغيلياً. كذلك `recetion_table` يمثل سجلات استقبال/إقامة، ولا يجوز إسقاطه آلياً على `bookings` دون تحديد علاقة الحقول.

المطلوب في الخطوة التالية هو بناء mapping لكل جدول كمبيوتر إلى أحد التصنيفات: **يُزامن كما هو**، **يُدمج مع كيان Flutter**، **يُقسّم إلى أكثر من كيان**، أو **يبقى خاصاً بسطح المكتب**. بعد اعتماد هذا mapping فقط يُعاد توليد D1 وطبقة Worker ومحولات Flutter.

## حدود الدليل الحالي

الملف المرجعي يثبت مخطط SQL Server وتعريفات النماذج، لكنه لا يثبت وجود بيانات إنتاجية داخل ملفات المستودع؛ ملفات `HotelSys/Data/*.db` منفصلة ولا تُعامل تلقائياً على أنها قاعدة Oraxhotel الأساسية. كما أن تطبيق المزامنة للكمبيوتر لم يُثبت بعد من الكود الحالي، ولذلك لا ينبغي الادعاء بأن أحداث الكمبيوتر تصل إلى الهاتف قبل تنفيذ عميل/Bridge واضح.

## مرجع المصدر

[1]: `../HotelSys/database/Hotel_alkheer_init.sql` — SQL Server initialization script for the desktop Oraxhotel model.
[2]: `../HotelSys/Models/Hotel_alkheerContext.cs` — EF model and relationship configuration.
[3]: `../HotelSys/db/db.generated.cs` — generated Linq2DB table/column type definitions.
