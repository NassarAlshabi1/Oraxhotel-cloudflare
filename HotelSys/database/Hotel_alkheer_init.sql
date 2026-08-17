/*
  Orax Hotel - SQL Server initialization script
  Generated from the checked-in Linq2DB model definitions.

  Safety notes:
  - This script creates the Hotel_alkheer database if it does not exist.
  - It contains no passwords and no production/customer data.
  - Run it with a SQL Server account allowed to create a database and tables.
  - Review the generated schema against the source backup before production use.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_ID(N'Hotel_alkheer') IS NULL
BEGIN
    CREATE DATABASE [Hotel_alkheer];
END;
GO

USE [Hotel_alkheer];
GO


IF OBJECT_ID(N'dbo.account_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[account_table] (
        [id] int NOT NULL,
        [name] nvarchar(100) NOT NULL,
        [status] nvarchar(50) NULL,
        [is_private] bit NULL,
        [createat] datetime NULL,
        [id_group] int NOT NULL,
        [code] int NULL,
        CONSTRAINT [PK_account_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.admin_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[admin_table] (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [name] nvarchar(70) NOT NULL,
        [username] nvarchar(50) NOT NULL,
        [password] nvarchar(200) NOT NULL,
        [status] bit NULL,
        [lastdate_login] date NULL,
        [adminid] bigint NULL,
        CONSTRAINT [PK_admin_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.area_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[area_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(200) NOT NULL,
        [id_city] int NOT NULL,
        [name_en] nvarchar(200) NULL,
        [name_ar_tashkeel] nvarchar(max) NULL,
        [name_ar_normalized] nvarchar(max) NULL,
        [name_en_normalized] nvarchar(max) NULL,
        CONSTRAINT [PK_area_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetRoleClaims] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id])
    );
END;
GO

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] nvarchar(max) NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        [FirstName] nvarchar(100) NULL,
        [LastName] nvarchar(100) NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserClaims] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id])
    );
END;
GO

IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey])
    );
END;
GO

IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId])
    );
END;
GO

IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name])
    );
END;
GO

IF OBJECT_ID(N'dbo.bank_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[bank_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(300) NOT NULL,
        [is_default] bit NULL,
        [id_account] int NOT NULL,
        [id_sub] int NULL,
        CONSTRAINT [PK_bank_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.bills_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[bills_table] (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [type] nvarchar(20) NOT NULL,
        [type_pay] nvarchar(20) NOT NULL,
        [num_reference] nvarchar(100) NULL,
        [date] datetime NOT NULL,
        [total] float NULL,
        [is_for_room] bit NOT NULL,
        [deserve_amount] float NULL,
        [type_discount] nvarchar(50) NULL,
        [qty_discount] float NULL,
        [pay_amount] float NULL,
        [rest_amount] float NULL,
        [num_check] nvarchar(50) NULL,
        [num_card] nvarchar(50) NULL,
        [note] nvarchar(max) NULL,
        [createat] datetime NULL,
        [id_account] int NOT NULL,
        [id_reception] bigint NULL,
        [id_bank] int NULL,
        [customer_or_company] nvarchar(50) NOT NULL,
        [id_currancy] int NULL,
        [total_tax_price] float NULL,
        [total_tax_rate] float NULL,
        [include_tax] bit NULL,
        [total_baladi_tax_price] float NULL,
        [total_baladi_tax_rate] float NULL,
        [is_baladi_tax] bit NULL,
        CONSTRAINT [PK_bills_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.bond_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[bond_table] (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [type] nvarchar(20) NOT NULL,
        [type_pay] nvarchar(20) NOT NULL,
        [num_reference] nvarchar(100) NULL,
        [date] datetime NOT NULL,
        [amount] float NOT NULL,
        [loc_pay] nvarchar(300) NULL,
        [worthy_date] datetime NULL,
        [why] nvarchar(max) NULL,
        [hand] nvarchar(max) NULL,
        [num_check] nvarchar(50) NULL,
        [num_card] nvarchar(50) NULL,
        [note] nvarchar(max) NULL,
        [createat] datetime NULL,
        [is_done_pay] bit NULL,
        [id_bond_pay] bigint NULL,
        [id_account] int NOT NULL,
        [id_reception] bigint NULL,
        [id_item_expenses] int NULL,
        [id_bank] int NULL,
        [id_currancy] int NULL,
        [time] nvarchar(max) NULL,
        CONSTRAINT [PK_bond_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.boxs_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[boxs_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(300) NOT NULL,
        [is_default] bit NULL,
        [id_account] int NOT NULL,
        [id_sub] int NULL,
        [is_private] bit NOT NULL,
        CONSTRAINT [PK_boxs_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.boxs_user_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[boxs_user_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [id_box] int NOT NULL,
        [id_aspUser] nvarchar(450) NOT NULL,
        [is_defult] bit NULL,
        CONSTRAINT [PK_boxs_user_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.change_room_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[change_room_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [id_room_from] int NOT NULL,
        [id_room_to] int NOT NULL,
        [why] nvarchar(max) NOT NULL,
        [date] datetime NOT NULL,
        [price_old] float NOT NULL,
        [price_current] float NOT NULL,
        [id_receptoin] bigint NOT NULL,
        [id_sub] int NULL,
        CONSTRAINT [PK_change_room_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.city_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[city_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(max) NOT NULL,
        [id_country] int NULL,
        [name_en] nvarchar(max) NULL,
        CONSTRAINT [PK_city_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.company_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[company_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(50) NOT NULL,
        [id_account] int NULL,
        [id_sub] int NULL,
        CONSTRAINT [PK_company_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.condition_reception_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[condition_reception_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(max) NOT NULL,
        [num] int NOT NULL,
        [id_sub] int NOT NULL,
        CONSTRAINT [PK_condition_reception_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.country_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[country_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(200) NOT NULL,
        [name_en] nvarchar(200) NULL,
        CONSTRAINT [PK_country_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.currency_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[currency_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(300) NOT NULL,
        [is_default] bit NULL,
        [code] nvarchar(5) NULL,
        [rate_convert] float NULL,
        [id_sub] int NULL,
        CONSTRAINT [PK_currency_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.customer_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[customer_table] (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [name] nvarchar(max) NOT NULL,
        [type] nvarchar(100) NOT NULL,
        [sex] nvarchar(10) NULL,
        [email] nvarchar(max) NULL,
        [nationality] nvarchar(300) NOT NULL,
        [type_work] nvarchar(100) NULL,
        [loc_work] nvarchar(100) NULL,
        [phone_work] nvarchar(100) NULL,
        [type_proof] nvarchar(30) NOT NULL,
        [num_proof] nvarchar(300) NOT NULL,
        [release_date] datetime NULL,
        [end_date] datetime NULL,
        [loc_release] nvarchar(300) NULL,
        [createat] datetime NOT NULL,
        [public_note] nvarchar(max) NULL,
        [id_area] int NULL,
        [id_nationality] int NULL,
        CONSTRAINT [PK_customer_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.detials_bills_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[detials_bills_table] (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [qty] float NULL,
        [price_one] float NULL,
        [total] float NULL,
        [id_bill] bigint NOT NULL,
        [id_product] int NOT NULL,
        [tax_price] float NULL,
        [tax_rate] float NULL,
        [baladi_tax_price] float NULL,
        [baladi_tax_rate] float NULL,
        [is_baladi_tax] bit NULL,
        CONSTRAINT [PK_detials_bills_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.detials_hotel_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[detials_hotel_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [count_room] float NULL,
        [count_floot] float NULL,
        [id_ho] int NOT NULL,
        CONSTRAINT [PK_detials_hotel_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.detials_status_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[detials_status_table] (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [status] nvarchar(50) NULL,
        [id_room] int NOT NULL,
        [detials] nvarchar(max) NULL,
        [start_date] nvarchar(max) NULL,
        [end_date] nvarchar(max) NULL,
        [id_reception] bigint NULL,
        [id_emp] int NULL,
        [createat] datetime NOT NULL,
        [id_sub] int NULL,
        [id_status_before] bigint NULL,
        CONSTRAINT [PK_detials_status_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.emp_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[emp_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(50) NOT NULL,
        [img] nvarchar(300) NULL,
        [phone] nvarchar(15) NOT NULL,
        [email] nvarchar(50) NULL,
        [sex] nvarchar(10) NULL,
        [num_identity] nvarchar(50) NULL,
        [id_sub] int NULL,
        CONSTRAINT [PK_emp_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.entries_acc_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[entries_acc_table] (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [debt_or_Credit] nvarchar(1) NOT NULL,
        [amount] float NULL,
        [bill_or_band] nvarchar(10) NOT NULL,
        [id_document_dand] bigint NULL,
        [id_document_bill] bigint NULL,
        [type_document] nvarchar(50) NOT NULL,
        [id_account] int NOT NULL,
        [id_currancy] int NULL,
        [date] datetime NULL,
        [id_recetion] bigint NULL,
        [note] nvarchar(max) NULL,
        CONSTRAINT [PK_entries_acc_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.follower_reception_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[follower_reception_table] (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [cu_type] nvarchar(10) NOT NULL,
        [relation] nvarchar(50) NOT NULL,
        [id_receptoin] bigint NOT NULL,
        [id_customer] bigint NOT NULL,
        [duration] nvarchar(1) NULL,
        [duration_from] datetime NULL,
        [duration_to] datetime NULL,
        CONSTRAINT [PK_follower_reception_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.group_account_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[group_account_table] (
        [id] int NOT NULL,
        [name] nvarchar(100) NOT NULL,
        [id_main_group] int NULL,
        [is_root] bit NULL,
        [is_private] bit NULL,
        [id_sub] int NULL,
        CONSTRAINT [PK_group_account_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.group_services_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[group_services_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(max) NOT NULL,
        [id_sub] int NULL,
        [name_en] nvarchar(max) NULL,
        CONSTRAINT [PK_group_services_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.hotels_branch_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[hotels_branch_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name_h] nvarchar(50) NOT NULL,
        [num_en] nvarchar(50) NULL,
        [country] nvarchar(50) NOT NULL,
        [city] nvarchar(50) NOT NULL,
        [regin] nvarchar(50) NOT NULL,
        [address] nvarchar(150) NULL,
        [email] nvarchar(max) NULL,
        [phone] nvarchar(max) NOT NULL,
        [website] nvarchar(max) NULL,
        [mail_box] nvarchar(150) NULL,
        [logo] nvarchar(max) NULL,
        [id_sub] int NULL,
        [id_country] int NULL,
        [id_org] int NULL,
        [count_floot] int NULL,
        CONSTRAINT [PK_hotels_branch_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.items_expenses_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[items_expenses_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(300) NOT NULL,
        [id_sub] int NULL,
        [id_account] int NOT NULL,
        [create_at] datetime NULL,
        CONSTRAINT [PK_items_expenses_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.jobs_name_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[jobs_name_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(300) NOT NULL,
        [id_sub] int NULL,
        CONSTRAINT [PK_jobs_name_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.jop_emp_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[jop_emp_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [id_emp] int NOT NULL,
        [id_job_name] int NOT NULL,
        CONSTRAINT [PK_jop_emp_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.my_customers', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[my_customers] (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [id_customer] bigint NOT NULL,
        [idsub] int NULL,
        [private_note] nvarchar(max) NULL,
        [id_account] int NULL,
        [createat] datetime NULL,
        [visit_end_date] datetime NULL,
        CONSTRAINT [PK_my_customers] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.orgs_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[orgs_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name_h] nvarchar(50) NOT NULL,
        [num_en] nvarchar(50) NULL,
        [country] nvarchar(50) NOT NULL,
        [city] nvarchar(50) NOT NULL,
        [regin] nvarchar(50) NOT NULL,
        [address] nvarchar(150) NULL,
        [email] nvarchar(max) NULL,
        [phone] nvarchar(max) NOT NULL,
        [website] nvarchar(max) NULL,
        [mail_box] nvarchar(150) NULL,
        [logo] nvarchar(max) NULL,
        [id_sub] int NULL,
        [id_country] int NULL,
        [tax_num] nvarchar(max) NULL,
        CONSTRAINT [PK_orgs_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.overtime_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[overtime_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [start_date] datetime NULL,
        [end_date] datetime NULL,
        [start_time] nvarchar(max) NOT NULL,
        [end_time] nvarchar(max) NOT NULL,
        [createat] datetime NOT NULL,
        [id_user] int NULL,
        [id_sub] int NULL,
        CONSTRAINT [PK_overtime_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.price_rooms_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[price_rooms_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [price] float NULL,
        [price_overtime] float NULL,
        [price_min] float NULL,
        [id_sub] int NULL,
        [id_room] int NOT NULL,
        [id_tax_group] int NULL,
        CONSTRAINT [PK_price_rooms_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.product_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[product_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(max) NOT NULL,
        [id_group] int NOT NULL,
        [name_en] nvarchar(max) NULL,
        [price] float NOT NULL,
        [id_tax_group] int NULL,
        CONSTRAINT [PK_product_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.recetion_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[recetion_table] (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [source] nvarchar(50) NOT NULL,
        [price] float NULL,
        [qty_time] int NULL,
        [unit] nvarchar(50) NOT NULL,
        [start_date] datetime NOT NULL,
        [end_date] datetime NOT NULL,
        [type_date] nvarchar(1) NULL,
        [is_chechin] bit NULL,
        [checkin_date] datetime NULL,
        [is_chechout] bit NULL,
        [chechout_date] datetime NULL,
        [id_room] int NOT NULL,
        [note] nvarchar(max) NULL,
        [id_co] int NULL,
        [id_my_customer] bigint NULL,
        [status] int NULL,
        [why_visit] nvarchar(max) NULL,
        [area_from] nvarchar(max) NULL,
        CONSTRAINT [PK_recetion_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.rooms_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[rooms_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name_r] nvarchar(50) NOT NULL,
        [num_floor] nvarchar(50) NULL,
        [count_rooms] int NULL,
        [count_bed_single] int NULL,
        [count_bed_double] int NULL,
        [count_bathroom] int NULL,
        [count_tv] int NULL,
        [count_wallet] int NULL,
        [type_condition] nvarchar(50) NULL,
        [public_features] nvarchar(max) NULL,
        [private_features] nvarchar(max) NULL,
        [note] nvarchar(max) NULL,
        [id_ho] int NOT NULL,
        [id_type] int NOT NULL,
        CONSTRAINT [PK_rooms_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.setting_general_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[setting_general_table] (
        [id] int NOT NULL,
        [services_include_tax] bit NOT NULL,
        [enable_tax_num] bit NOT NULL,
        CONSTRAINT [PK_setting_general_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.setting_reception_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[setting_reception_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [is_checkin_time] bit NULL,
        [time_checkin] nvarchar(max) NULL,
        [is_checkout_time] bit NULL,
        [time_checkout] nvarchar(max) NULL,
        [is_interval_changeroom] bit NULL,
        [interval_changeroom] int NULL,
        [is_mounth_reception_checkout] bit NULL,
        [id_sub] int NOT NULL,
        [incude_price_tax] bit NULL,
        CONSTRAINT [PK_setting_reception_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.status_current_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[status_current_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [status] nvarchar(50) NULL,
        [id_room] int NOT NULL,
        [start_date] nvarchar(max) NULL,
        [end_date] nvarchar(max) NULL,
        [id_reception] bigint NULL,
        [id_emp] int NULL,
        [createat] datetime NULL,
        [detials] nvarchar(max) NULL,
        CONSTRAINT [PK_status_current_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.tax_group_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tax_group_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name] nvarchar(max) NOT NULL,
        [name_en] nvarchar(200) NULL,
        [rate] int NOT NULL,
        [id_user] int NULL,
        [is_baladi_tax] bit NOT NULL,
        [baladi_rate] float NULL,
        CONSTRAINT [PK_tax_group_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.type_rooms_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[type_rooms_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [name_t] nvarchar(max) NOT NULL,
        [color] nvarchar(50) NULL,
        [id_sub] int NULL,
        CONSTRAINT [PK_type_rooms_table] PRIMARY KEY ([id])
    );
END;
GO

IF OBJECT_ID(N'dbo.user_table', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[user_table] (
        [id] int IDENTITY(1,1) NOT NULL,
        [username] nvarchar(50) NOT NULL,
        [password] nvarchar(50) NOT NULL,
        [role] nvarchar(50) NOT NULL,
        [id_sub] int NOT NULL,
        CONSTRAINT [PK_user_table] PRIMARY KEY ([id])
    );
END;
GO

/* Minimal, non-sensitive initial data. All inserts are idempotent. */
IF OBJECT_ID(N'dbo.group_account_table', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.group_account_table)
BEGIN
    INSERT INTO dbo.group_account_table (id, name, is_root) VALUES (1, N'الأصول', 1);
END;
GO

IF OBJECT_ID(N'dbo.country_table', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.country_table)
BEGIN
    INSERT INTO dbo.country_table (name, name_en) VALUES (N'غير محدد', N'Undefined');
END;
GO

IF OBJECT_ID(N'dbo.currency_table', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.currency_table)
BEGIN
    INSERT INTO dbo.currency_table (name, is_default, code, rate_convert) VALUES (N'العملة المحلية', 1, N'LOC', 1);
END;
GO

IF OBJECT_ID(N'dbo.group_services_table', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.group_services_table)
BEGIN
    INSERT INTO dbo.group_services_table (name, name_en) VALUES (N'خدمات عامة', N'General Services');
END;
GO

IF OBJECT_ID(N'dbo.orgs_table', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.orgs_table)
BEGIN
    INSERT INTO dbo.orgs_table (name_h, country, city, regin, phone) VALUES (N'أوراكس هوتيل', N'غير محدد', N'غير محدد', N'غير محدد', N'غير محدد');
END;
GO

IF OBJECT_ID(N'dbo.hotels_branch_table', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.hotels_branch_table)
BEGIN
    DECLARE @OrgId int = (SELECT TOP (1) id FROM dbo.orgs_table ORDER BY id);
    INSERT INTO dbo.hotels_branch_table (name_h, country, city, regin, phone, id_org) VALUES (N'الفرع الرئيسي', N'غير محدد', N'غير محدد', N'غير محدد', N'غير محدد', @OrgId);
END;
GO

IF OBJECT_ID(N'dbo.setting_general_table', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.setting_general_table)
BEGIN
    INSERT INTO dbo.setting_general_table (id, services_include_tax, enable_tax_num) VALUES (1, 0, 0);
END;
GO

IF OBJECT_ID(N'dbo.setting_reception_table', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.setting_reception_table)
BEGIN
    INSERT INTO dbo.setting_reception_table (id_sub, incude_price_tax) VALUES (1, 0);
END;
GO

IF OBJECT_ID(N'dbo.tax_group_table', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.tax_group_table)
BEGIN
    INSERT INTO dbo.tax_group_table (name, rate, is_baladi_tax, baladi_rate) VALUES (N'بدون ضريبة', 0, 0, 0);
END;
GO

IF OBJECT_ID(N'dbo.type_rooms_table', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.type_rooms_table)
BEGIN
    INSERT INTO dbo.type_rooms_table (name_t, color) VALUES (N'غرفة عامة', N'#808080');
END;
GO

IF OBJECT_ID(N'dbo.rooms_table', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.rooms_table)
BEGIN
    DECLARE @BranchId int = (SELECT TOP (1) id FROM dbo.hotels_branch_table ORDER BY id);
    DECLARE @RoomTypeId int = (SELECT TOP (1) id FROM dbo.type_rooms_table ORDER BY id);
    INSERT INTO dbo.rooms_table (name_r, id_ho, id_type) VALUES (N'غرفة 001', @BranchId, @RoomTypeId);
END;
GO

-- Do not insert an administrator, legacy user, password, or PasswordHash here.
-- Create the first account through ASP.NET Identity after the database is initialized.
