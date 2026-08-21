-- Migration 0001: exact desktop Oraxhotel SQL Server mirror.
-- Source: HotelSys/database/Hotel_alkheer_init.sql (47 tables, 378 columns).
-- SQL Server types are translated to SQLite/D1 affinities without renaming columns.
-- This mirror is the source-side preservation layer; mobile projections are in 0003.

CREATE TABLE IF NOT EXISTS "account_table" (
  "id" INTEGER NOT NULL PRIMARY KEY,
  "name" TEXT NOT NULL,
  "status" TEXT,
  "is_private" INTEGER,
  "createat" TEXT,
  "id_group" INTEGER NOT NULL,
  "code" INTEGER,
  FOREIGN KEY ("id_group") REFERENCES "group_account_table" ("id")
);

CREATE TABLE IF NOT EXISTS "admin_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "username" TEXT NOT NULL,
  "password" TEXT NOT NULL,
  "status" INTEGER,
  "lastdate_login" TEXT,
  "adminid" INTEGER
);

CREATE TABLE IF NOT EXISTS "area_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "id_city" INTEGER NOT NULL,
  "name_en" TEXT,
  "name_ar_tashkeel" TEXT,
  "name_ar_normalized" TEXT,
  "name_en_normalized" TEXT,
  FOREIGN KEY ("id_city") REFERENCES "city_table" ("id")
);

CREATE TABLE IF NOT EXISTS "AspNetRoles" (
  "Id" TEXT NOT NULL PRIMARY KEY,
  "Name" TEXT,
  "NormalizedName" TEXT,
  "ConcurrencyStamp" TEXT
);

CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
  "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "RoleId" TEXT NOT NULL,
  "ClaimType" TEXT,
  "ClaimValue" TEXT
);

CREATE TABLE IF NOT EXISTS "AspNetUsers" (
  "Id" TEXT NOT NULL PRIMARY KEY,
  "UserName" TEXT,
  "NormalizedUserName" TEXT,
  "Email" TEXT,
  "NormalizedEmail" TEXT,
  "EmailConfirmed" INTEGER NOT NULL,
  "PasswordHash" TEXT,
  "SecurityStamp" TEXT,
  "ConcurrencyStamp" TEXT,
  "PhoneNumber" TEXT,
  "PhoneNumberConfirmed" INTEGER NOT NULL,
  "TwoFactorEnabled" INTEGER NOT NULL,
  "LockoutEnd" TEXT,
  "LockoutEnabled" INTEGER NOT NULL,
  "AccessFailedCount" INTEGER NOT NULL,
  "FirstName" TEXT,
  "LastName" TEXT
);

CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
  "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "UserId" TEXT NOT NULL,
  "ClaimType" TEXT,
  "ClaimValue" TEXT
);

CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
  "LoginProvider" TEXT NOT NULL,
  "ProviderKey" TEXT NOT NULL,
  "ProviderDisplayName" TEXT,
  "UserId" TEXT NOT NULL,
  PRIMARY KEY ("LoginProvider", "ProviderKey")
);

CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
  "UserId" TEXT NOT NULL,
  "RoleId" TEXT NOT NULL,
  PRIMARY KEY ("UserId", "RoleId")
);

CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
  "UserId" TEXT NOT NULL,
  "LoginProvider" TEXT NOT NULL,
  "Name" TEXT NOT NULL,
  "Value" TEXT,
  PRIMARY KEY ("UserId", "LoginProvider", "Name")
);

CREATE TABLE IF NOT EXISTS "bank_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "is_default" INTEGER,
  "id_account" INTEGER NOT NULL,
  "id_sub" INTEGER,
  FOREIGN KEY ("id_account") REFERENCES "account_table" ("id")
);

CREATE TABLE IF NOT EXISTS "bills_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "type" TEXT NOT NULL,
  "type_pay" TEXT NOT NULL,
  "num_reference" TEXT,
  "date" TEXT NOT NULL,
  "total" REAL,
  "is_for_room" INTEGER NOT NULL,
  "deserve_amount" REAL,
  "type_discount" TEXT,
  "qty_discount" REAL,
  "pay_amount" REAL,
  "rest_amount" REAL,
  "num_check" TEXT,
  "num_card" TEXT,
  "note" TEXT,
  "createat" TEXT,
  "id_account" INTEGER NOT NULL,
  "id_reception" INTEGER,
  "id_bank" INTEGER,
  "customer_or_company" TEXT NOT NULL,
  "id_currancy" INTEGER,
  "total_tax_price" REAL,
  "total_tax_rate" REAL,
  "include_tax" INTEGER,
  "total_baladi_tax_price" REAL,
  "total_baladi_tax_rate" REAL,
  "is_baladi_tax" INTEGER,
  FOREIGN KEY ("id_account") REFERENCES "account_table" ("id"),
  FOREIGN KEY ("id_reception") REFERENCES "recetion_table" ("id")
);

CREATE TABLE IF NOT EXISTS "bond_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "type" TEXT NOT NULL,
  "type_pay" TEXT NOT NULL,
  "num_reference" TEXT,
  "date" TEXT NOT NULL,
  "amount" REAL NOT NULL,
  "loc_pay" TEXT,
  "worthy_date" TEXT,
  "why" TEXT,
  "hand" TEXT,
  "num_check" TEXT,
  "num_card" TEXT,
  "note" TEXT,
  "createat" TEXT,
  "is_done_pay" INTEGER,
  "id_bond_pay" INTEGER,
  "id_account" INTEGER NOT NULL,
  "id_reception" INTEGER,
  "id_item_expenses" INTEGER,
  "id_bank" INTEGER,
  "id_currancy" INTEGER,
  "time" TEXT,
  FOREIGN KEY ("id_account") REFERENCES "account_table" ("id"),
  FOREIGN KEY ("id_bond_pay") REFERENCES "bond_table" ("id"),
  FOREIGN KEY ("id_item_expenses") REFERENCES "items_expenses_table" ("id")
);

CREATE TABLE IF NOT EXISTS "boxs_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "is_default" INTEGER,
  "id_account" INTEGER NOT NULL,
  "id_sub" INTEGER,
  "is_private" INTEGER NOT NULL,
  FOREIGN KEY ("id_account") REFERENCES "account_table" ("id")
);

CREATE TABLE IF NOT EXISTS "boxs_user_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "id_box" INTEGER NOT NULL,
  "id_aspUser" TEXT NOT NULL,
  "is_defult" INTEGER,
  FOREIGN KEY ("id_aspUser") REFERENCES "AspNetUsers" ("Id"),
  FOREIGN KEY ("id_box") REFERENCES "boxs_table" ("id")
);

CREATE TABLE IF NOT EXISTS "change_room_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "id_room_from" INTEGER NOT NULL,
  "id_room_to" INTEGER NOT NULL,
  "why" TEXT NOT NULL,
  "date" TEXT NOT NULL,
  "price_old" REAL NOT NULL,
  "price_current" REAL NOT NULL,
  "id_receptoin" INTEGER NOT NULL,
  "id_sub" INTEGER
);

CREATE TABLE IF NOT EXISTS "city_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "id_country" INTEGER,
  "name_en" TEXT,
  FOREIGN KEY ("id_country") REFERENCES "country_table" ("id")
);

CREATE TABLE IF NOT EXISTS "company_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "id_account" INTEGER,
  "id_sub" INTEGER,
  FOREIGN KEY ("id_account") REFERENCES "account_table" ("id")
);

CREATE TABLE IF NOT EXISTS "condition_reception_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "num" INTEGER NOT NULL,
  "id_sub" INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS "country_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "name_en" TEXT
);

CREATE TABLE IF NOT EXISTS "currency_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "is_default" INTEGER,
  "code" TEXT,
  "rate_convert" REAL,
  "id_sub" INTEGER
);

CREATE TABLE IF NOT EXISTS "customer_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "type" TEXT NOT NULL,
  "sex" TEXT,
  "email" TEXT,
  "nationality" TEXT NOT NULL,
  "type_work" TEXT,
  "loc_work" TEXT,
  "phone_work" TEXT,
  "type_proof" TEXT NOT NULL,
  "num_proof" TEXT NOT NULL,
  "release_date" TEXT,
  "end_date" TEXT,
  "loc_release" TEXT,
  "createat" TEXT NOT NULL,
  "public_note" TEXT,
  "id_area" INTEGER,
  "id_nationality" INTEGER,
  FOREIGN KEY ("id_area") REFERENCES "area_table" ("id")
);

CREATE TABLE IF NOT EXISTS "detials_bills_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "qty" REAL,
  "price_one" REAL,
  "total" REAL,
  "id_bill" INTEGER NOT NULL,
  "id_product" INTEGER NOT NULL,
  "tax_price" REAL,
  "tax_rate" REAL,
  "baladi_tax_price" REAL,
  "baladi_tax_rate" REAL,
  "is_baladi_tax" INTEGER,
  FOREIGN KEY ("id_bill") REFERENCES "bills_table" ("id"),
  FOREIGN KEY ("id_product") REFERENCES "product_table" ("id")
);

CREATE TABLE IF NOT EXISTS "detials_hotel_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "count_room" REAL,
  "count_floot" REAL,
  "id_ho" INTEGER NOT NULL,
  FOREIGN KEY ("id_ho") REFERENCES "hotels_branch_table" ("id")
);

CREATE TABLE IF NOT EXISTS "detials_status_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "status" TEXT,
  "id_room" INTEGER NOT NULL,
  "detials" TEXT,
  "start_date" TEXT,
  "end_date" TEXT,
  "id_reception" INTEGER,
  "id_emp" INTEGER,
  "createat" TEXT NOT NULL,
  "id_sub" INTEGER,
  "id_status_before" INTEGER,
  FOREIGN KEY ("id_status_before") REFERENCES "detials_status_table" ("id")
);

CREATE TABLE IF NOT EXISTS "emp_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "img" TEXT,
  "phone" TEXT NOT NULL,
  "email" TEXT,
  "sex" TEXT,
  "num_identity" TEXT,
  "id_sub" INTEGER
);

CREATE TABLE IF NOT EXISTS "entries_acc_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "debt_or_Credit" TEXT NOT NULL,
  "amount" REAL,
  "bill_or_band" TEXT NOT NULL,
  "id_document_dand" INTEGER,
  "id_document_bill" INTEGER,
  "type_document" TEXT NOT NULL,
  "id_account" INTEGER NOT NULL,
  "id_currancy" INTEGER,
  "date" TEXT,
  "id_recetion" INTEGER,
  "note" TEXT,
  FOREIGN KEY ("id_account") REFERENCES "account_table" ("id")
);

CREATE TABLE IF NOT EXISTS "follower_reception_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "cu_type" TEXT NOT NULL,
  "relation" TEXT NOT NULL,
  "id_receptoin" INTEGER NOT NULL,
  "id_customer" INTEGER NOT NULL,
  "duration" TEXT,
  "duration_from" TEXT,
  "duration_to" TEXT,
  FOREIGN KEY ("id_customer") REFERENCES "customer_table" ("id"),
  FOREIGN KEY ("id_receptoin") REFERENCES "recetion_table" ("id")
);

CREATE TABLE IF NOT EXISTS "group_account_table" (
  "id" INTEGER NOT NULL PRIMARY KEY,
  "name" TEXT NOT NULL,
  "id_main_group" INTEGER,
  "is_root" INTEGER,
  "is_private" INTEGER,
  "id_sub" INTEGER,
  FOREIGN KEY ("id_main_group") REFERENCES "group_account_table" ("id")
);

CREATE TABLE IF NOT EXISTS "group_services_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "id_sub" INTEGER,
  "name_en" TEXT
);

CREATE TABLE IF NOT EXISTS "hotels_branch_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name_h" TEXT NOT NULL,
  "num_en" TEXT,
  "country" TEXT NOT NULL,
  "city" TEXT NOT NULL,
  "regin" TEXT NOT NULL,
  "address" TEXT,
  "email" TEXT,
  "phone" TEXT NOT NULL,
  "website" TEXT,
  "mail_box" TEXT,
  "logo" TEXT,
  "id_sub" INTEGER,
  "id_country" INTEGER,
  "id_org" INTEGER,
  "count_floot" INTEGER,
  FOREIGN KEY ("id_country") REFERENCES "country_table" ("id"),
  FOREIGN KEY ("id_org") REFERENCES "orgs_table" ("id")
);

CREATE TABLE IF NOT EXISTS "items_expenses_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "id_sub" INTEGER,
  "id_account" INTEGER NOT NULL,
  "create_at" TEXT,
  FOREIGN KEY ("id_account") REFERENCES "account_table" ("id")
);

CREATE TABLE IF NOT EXISTS "jobs_name_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "id_sub" INTEGER
);

CREATE TABLE IF NOT EXISTS "jop_emp_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "id_emp" INTEGER NOT NULL,
  "id_job_name" INTEGER NOT NULL,
  FOREIGN KEY ("id_emp") REFERENCES "emp_table" ("id")
);

CREATE TABLE IF NOT EXISTS "my_customers" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "id_customer" INTEGER NOT NULL,
  "idsub" INTEGER,
  "private_note" TEXT,
  "id_account" INTEGER,
  "createat" TEXT,
  "visit_end_date" TEXT,
  FOREIGN KEY ("id_account") REFERENCES "account_table" ("id"),
  FOREIGN KEY ("id_customer") REFERENCES "customer_table" ("id")
);

CREATE TABLE IF NOT EXISTS "orgs_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name_h" TEXT NOT NULL,
  "num_en" TEXT,
  "country" TEXT NOT NULL,
  "city" TEXT NOT NULL,
  "regin" TEXT NOT NULL,
  "address" TEXT,
  "email" TEXT,
  "phone" TEXT NOT NULL,
  "website" TEXT,
  "mail_box" TEXT,
  "logo" TEXT,
  "id_sub" INTEGER,
  "id_country" INTEGER,
  "tax_num" TEXT,
  FOREIGN KEY ("id_country") REFERENCES "country_table" ("id")
);

CREATE TABLE IF NOT EXISTS "overtime_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "start_date" TEXT,
  "end_date" TEXT,
  "start_time" TEXT NOT NULL,
  "end_time" TEXT NOT NULL,
  "createat" TEXT NOT NULL,
  "id_user" INTEGER,
  "id_sub" INTEGER
);

CREATE TABLE IF NOT EXISTS "price_rooms_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "price" REAL,
  "price_overtime" REAL,
  "price_min" REAL,
  "id_sub" INTEGER,
  "id_room" INTEGER NOT NULL,
  "id_tax_group" INTEGER,
  FOREIGN KEY ("id_room") REFERENCES "rooms_table" ("id"),
  FOREIGN KEY ("id_tax_group") REFERENCES "tax_group_table" ("id")
);

CREATE TABLE IF NOT EXISTS "product_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "id_group" INTEGER NOT NULL,
  "name_en" TEXT,
  "price" REAL NOT NULL,
  "id_tax_group" INTEGER,
  FOREIGN KEY ("id_group") REFERENCES "group_services_table" ("id"),
  FOREIGN KEY ("id_tax_group") REFERENCES "tax_group_table" ("id")
);

CREATE TABLE IF NOT EXISTS "recetion_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "source" TEXT NOT NULL,
  "price" REAL,
  "qty_time" INTEGER,
  "unit" TEXT NOT NULL,
  "start_date" TEXT NOT NULL,
  "end_date" TEXT NOT NULL,
  "type_date" TEXT,
  "is_chechin" INTEGER,
  "checkin_date" TEXT,
  "is_chechout" INTEGER,
  "chechout_date" TEXT,
  "id_room" INTEGER NOT NULL,
  "note" TEXT,
  "id_co" INTEGER,
  "id_my_customer" INTEGER,
  "status" INTEGER,
  "why_visit" TEXT,
  "area_from" TEXT,
  FOREIGN KEY ("id_co") REFERENCES "company_table" ("id"),
  FOREIGN KEY ("id_my_customer") REFERENCES "my_customers" ("id"),
  FOREIGN KEY ("id_room") REFERENCES "rooms_table" ("id")
);

CREATE TABLE IF NOT EXISTS "rooms_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name_r" TEXT NOT NULL,
  "num_floor" TEXT,
  "count_rooms" INTEGER,
  "count_bed_single" INTEGER,
  "count_bed_double" INTEGER,
  "count_bathroom" INTEGER,
  "count_tv" INTEGER,
  "count_wallet" INTEGER,
  "type_condition" TEXT,
  "public_features" TEXT,
  "private_features" TEXT,
  "note" TEXT,
  "id_ho" INTEGER NOT NULL,
  "id_type" INTEGER NOT NULL,
  FOREIGN KEY ("id_ho") REFERENCES "hotels_branch_table" ("id"),
  FOREIGN KEY ("id_type") REFERENCES "type_rooms_table" ("id")
);

CREATE TABLE IF NOT EXISTS "setting_general_table" (
  "id" INTEGER NOT NULL PRIMARY KEY,
  "services_include_tax" INTEGER NOT NULL,
  "enable_tax_num" INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS "setting_reception_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "is_checkin_time" INTEGER,
  "time_checkin" TEXT,
  "is_checkout_time" INTEGER,
  "time_checkout" TEXT,
  "is_interval_changeroom" INTEGER,
  "interval_changeroom" INTEGER,
  "is_mounth_reception_checkout" INTEGER,
  "id_sub" INTEGER NOT NULL,
  "incude_price_tax" INTEGER
);

CREATE TABLE IF NOT EXISTS "status_current_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "status" TEXT,
  "id_room" INTEGER NOT NULL,
  "start_date" TEXT,
  "end_date" TEXT,
  "id_reception" INTEGER,
  "id_emp" INTEGER,
  "createat" TEXT,
  "detials" TEXT,
  FOREIGN KEY ("id_room") REFERENCES "rooms_table" ("id")
);

CREATE TABLE IF NOT EXISTS "tax_group_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name" TEXT NOT NULL,
  "name_en" TEXT,
  "rate" INTEGER NOT NULL,
  "id_user" INTEGER,
  "is_baladi_tax" INTEGER NOT NULL,
  "baladi_rate" REAL
);

CREATE TABLE IF NOT EXISTS "type_rooms_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "name_t" TEXT NOT NULL,
  "color" TEXT,
  "id_sub" INTEGER
);

CREATE TABLE IF NOT EXISTS "user_table" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "username" TEXT NOT NULL,
  "password" TEXT NOT NULL,
  "role" TEXT NOT NULL,
  "id_sub" INTEGER NOT NULL
);
