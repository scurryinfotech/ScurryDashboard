BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS "AppSettings" (
	"Id"	INTEGER,
	"SettingKey"	TEXT,
	"SettingValue"	TEXT,
	"CreatedBy"	INTEGER,
	PRIMARY KEY("Id" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS Attendance (
    AttendanceId   INTEGER  PRIMARY KEY AUTOINCREMENT,
    StaffId        INTEGER  NOT NULL,
    AttendanceDate TEXT     NOT NULL,
    Status         TEXT     NOT NULL,
    CheckIn        TEXT,
    CheckOut       TEXT,
    OvertimeHours  REAL,
    Notes          TEXT,
    IsActive       INTEGER  DEFAULT 1,
    IsDeleted      INTEGER  DEFAULT 0,
    CreatedAt      TEXT     DEFAULT (datetime('now')),
    ModifiedAt     TEXT,
    ModifiedBy     TEXT,
    CreatedBy      INTEGER,
    UNIQUE (StaffId, AttendanceDate)
);
CREATE TABLE IF NOT EXISTS AttendanceLog (
    LogId        INTEGER  PRIMARY KEY AUTOINCREMENT,
    AttendanceId INTEGER,
    Action       TEXT,
    OldValues    TEXT,
    NewValues    TEXT,
    ChangedBy    TEXT,
    ChangedAt    TEXT     DEFAULT (datetime('now')),
    CreatedBy    INTEGER
);
CREATE TABLE IF NOT EXISTS CoffeeOrders (
    OrderId       INTEGER  PRIMARY KEY AUTOINCREMENT,
    OrderNumber   TEXT     UNIQUE,
    CustomerName  TEXT,
    FloorNo       TEXT,
    RoomNo        TEXT,
    TotalAmount   REAL,
    OrderStatus   INTEGER,
    CreatedDate   TEXT     DEFAULT (datetime('now')),
    CustomerPhone TEXT,
    IsActive      INTEGER  DEFAULT 1,
    CreatedBy     INTEGER,
    ModifiedDate  TEXT
);
CREATE TABLE IF NOT EXISTS Customers (
    Id           INTEGER  PRIMARY KEY AUTOINCREMENT,
    Name         TEXT     NOT NULL,
    Phone        TEXT     NOT NULL,
    CreatedDate  TEXT     DEFAULT (datetime('now')),
    CreatedBy    INTEGER  NOT NULL,
    ModifiedDate TEXT,
    ModifiedBy   INTEGER,
    IsActive     INTEGER  NOT NULL DEFAULT 1,
    Password     TEXT,
    loginame     TEXT,
    Address      TEXT
);
CREATE TABLE IF NOT EXISTS DailyExpenseLog (
    LogId           INTEGER  PRIMARY KEY AUTOINCREMENT,
    DailyExpenseId  INTEGER,
    Action          TEXT,
    OldValues       TEXT,
    NewValues       TEXT,
    ChangedBy       TEXT,
    ChangedAt       TEXT     DEFAULT (datetime('now')),
    CreatedBy       INTEGER
);
CREATE TABLE IF NOT EXISTS DailyExpenses (
    DailyExpenseId  INTEGER  PRIMARY KEY AUTOINCREMENT,
    Title           TEXT     NOT NULL,
    Category        TEXT,
    Amount          REAL     NOT NULL,
    ExpenseDate     TEXT     NOT NULL,
    PaidBy          TEXT,
    Notes           TEXT,
    IsActive        INTEGER  DEFAULT 1,
    IsDeleted       INTEGER  DEFAULT 0,
    CreatedAt       TEXT     DEFAULT (datetime('now')),
    ModifiedAt      TEXT,
    ModifiedBy      TEXT,
    PaymentMode     TEXT     NOT NULL DEFAULT 'Cash',
    CreatedBy       INTEGER
);
CREATE TABLE IF NOT EXISTS HotCoffeeVarieties (
    Id          INTEGER  PRIMARY KEY AUTOINCREMENT,
    CoffeeName  TEXT     NOT NULL,
    Description TEXT,
    CoffeeType  TEXT,
    Ingredients TEXT,
    Price       REAL,
    ImageUrl    TEXT,
    CreatedDate TEXT     DEFAULT (datetime('now')),
    IsActive    INTEGER  DEFAULT 1,
    CreatedBy   INTEGER,
    Image       TEXT
);
CREATE TABLE IF NOT EXISTS OrderItems (
    Id         INTEGER  PRIMARY KEY AUTOINCREMENT,
    OrderId    INTEGER,
    CoffeeId   INTEGER,
    Quantity   INTEGER  NOT NULL DEFAULT 1,
    Price      REAL,
    TotalPrice REAL,
    CreatedBy  INTEGER,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);
CREATE TABLE IF NOT EXISTS OrderStatusMaster (
    Id           INTEGER  PRIMARY KEY AUTOINCREMENT,
    Name         TEXT     NOT NULL UNIQUE,
    CreatedDate  TEXT     DEFAULT (datetime('now')),
    CreatedBy    INTEGER,
    ModifiedDate TEXT,
    ModifiedBy   INTEGER,
    IsActive     INTEGER  DEFAULT 1
);
CREATE TABLE IF NOT EXISTS OrderSummary (
    SummaryId          INTEGER  PRIMARY KEY AUTOINCREMENT,
    OrderId            TEXT     NOT NULL,
    CustomerName       TEXT,
    Phone              TEXT,
    TotalAmount        REAL     NOT NULL DEFAULT 0,
    DiscountAmount     REAL     NOT NULL DEFAULT 0,
    FinalAmount        REAL     NOT NULL DEFAULT 0,
    CreatedDate        TEXT     DEFAULT (datetime('now')),
    CompletedDate      TEXT,
    PaymentMode        TEXT,
    specialInstruction TEXT,
    CreatedBy          INTEGER
);
CREATE TABLE IF NOT EXISTS Orders (
    Id                  INTEGER  PRIMARY KEY AUTOINCREMENT,
    OrderId             TEXT,
    OrderStatus         INTEGER  NOT NULL DEFAULT 1,
    FullPortion         INTEGER,
    HalfPortion         INTEGER,
    TableNo             INTEGER,
    CreatedDate         TEXT     DEFAULT (datetime('now')),
    CreatedBy           INTEGER,
    ModifiedDate        TEXT,
    ModifiedBy          INTEGER,
    IsActive            INTEGER  DEFAULT 1,
    item_id             INTEGER,
    Price               REAL,
    customerName        TEXT,
    phone               TEXT,
    OrderType           TEXT,
    Address             TEXT,
    payment_mode        TEXT,
    DeliveryType        TEXT,
    specialInstructions TEXT,
    UserId              INTEGER,
    DeliveryStaffId     INTEGER,
    IsSynced            INTEGER  DEFAULT 0
);
CREATE TABLE IF NOT EXISTS Roles (
    RoleId      INTEGER  PRIMARY KEY AUTOINCREMENT,
    RoleName    TEXT     NOT NULL,
    Description TEXT,
    IsActive    INTEGER  DEFAULT 1,
    CreatedAt   TEXT     DEFAULT (datetime('now')),
    ModifiedAt  TEXT,
    ModifiedBy  TEXT,
    CreatedBy   INTEGER
);
CREATE TABLE IF NOT EXISTS SalaryPayments (
    PaymentId       INTEGER  PRIMARY KEY AUTOINCREMENT,
    StaffId         INTEGER  NOT NULL,
    PayrollId       INTEGER,
    Amount          REAL     NOT NULL,
    PaymentDate     TEXT     NOT NULL,
    PaymentMethod   TEXT     NOT NULL,
    PaymentType     TEXT     NOT NULL,
    Reason          TEXT,
    Description     TEXT,
    CreatedAt       TEXT     DEFAULT (datetime('now')),
    CreatedBy       INTEGER,
    CreatedByUserId INTEGER
);
CREATE TABLE IF NOT EXISTS ShopExpenseLog (
    LogId      INTEGER  PRIMARY KEY AUTOINCREMENT,
    ExpenseId  INTEGER,
    Action     TEXT,
    OldValues  TEXT,
    NewValues  TEXT,
    ChangedBy  TEXT,
    ChangedAt  TEXT     DEFAULT (datetime('now')),
    CreatedBy  INTEGER
);
CREATE TABLE IF NOT EXISTS ShopExpenses (
    ExpenseId    INTEGER  PRIMARY KEY AUTOINCREMENT,
    Title        TEXT     NOT NULL,
    Category     TEXT,
    Amount       REAL     NOT NULL,
    ExpenseDate  TEXT     NOT NULL,
    Description  TEXT,
    IsActive     INTEGER  DEFAULT 1,
    IsDeleted    INTEGER  DEFAULT 0,
    CreatedAt    TEXT     DEFAULT (datetime('now')),
    ModifiedAt   TEXT,
    ModifiedBy   TEXT,
    PaymentMode  TEXT     NOT NULL DEFAULT 'Cash',
    CreatedBy    INTEGER
);
CREATE TABLE IF NOT EXISTS Staff (
    StaffId         INTEGER  PRIMARY KEY AUTOINCREMENT,
    FullName        TEXT     NOT NULL,
    RoleId          INTEGER,
    Phone           TEXT,
    Email           TEXT,
    CNIC            TEXT,
    Department      TEXT,
    Salary          REAL,
    JoinDate        TEXT,
    IsActive        INTEGER  DEFAULT 1,
    IsDeleted       INTEGER  DEFAULT 0,
    CreatedAt       TEXT     DEFAULT (datetime('now')),
    ModifiedAt      TEXT,
    ModifiedBy      TEXT,
    AvalfDelivery   INTEGER  NOT NULL DEFAULT 0,
    CreatedBy       INTEGER
);
CREATE TABLE IF NOT EXISTS StaffLog (
    LogId     INTEGER  PRIMARY KEY AUTOINCREMENT,
    StaffId   INTEGER,
    Action    TEXT,
    OldValues TEXT,
    NewValues TEXT,
    ChangedBy TEXT,
    ChangedAt TEXT     DEFAULT (datetime('now')),
    CreatedBy INTEGER
);
CREATE TABLE IF NOT EXISTS SyncLog (
    Id         INTEGER  PRIMARY KEY AUTOINCREMENT,
    TableName  TEXT     NOT NULL,
    RecordId   INTEGER  NOT NULL,
    Action     TEXT     NOT NULL,
    IsSynced   INTEGER  DEFAULT 0,
    CreatedAt  TEXT     DEFAULT (datetime('now')),
    SyncedAt   TEXT
);
CREATE TABLE IF NOT EXISTS UserTableMaster (
    Id           INTEGER  PRIMARY KEY AUTOINCREMENT,
    UserId       INTEGER  NOT NULL,
    TableCount   INTEGER  NOT NULL DEFAULT 0,
    CreatedDate  TEXT     DEFAULT (datetime('now')),
    CreatedBy    INTEGER,
    ModifiedDate TEXT,
    ModifiedBy   INTEGER,
    IsActive     INTEGER  DEFAULT 1
);
CREATE TABLE IF NOT EXISTS Users (
    Id                 INTEGER  PRIMARY KEY AUTOINCREMENT,
    Username           TEXT     NOT NULL UNIQUE,
    Password           TEXT     NOT NULL,
    JwtToken           TEXT,
    JwtTokenExpiryDate TEXT,
    CreatedDate        TEXT     DEFAULT (datetime('now')),
    CreatedBy          INTEGER,
    ModifiedDate       TEXT,
    ModifiedBy         INTEGER,
    IsActive           INTEGER  DEFAULT 1
);
CREATE TABLE IF NOT EXISTS VendorPayments (
    PaymentId        INTEGER  PRIMARY KEY AUTOINCREMENT,
    PurchaseOrderId  INTEGER  NOT NULL,
    VendorId         INTEGER  NOT NULL,
    PaymentDate      TEXT     NOT NULL,
    Amount           REAL     NOT NULL,
    PaymentType      TEXT     NOT NULL,
    PaymentMethod    TEXT     NOT NULL,
    InvoiceNumber    TEXT,
    ReferenceNumber  TEXT,
    Notes            TEXT,
    CreatedAt        TEXT     DEFAULT (datetime('now')),
    CreatedByStaffId INTEGER,
    ModifiedBy       TEXT,
    CreatedBy        INTEGER,
    FOREIGN KEY (VendorId) REFERENCES Vendors(VendorId)
);
CREATE TABLE IF NOT EXISTS Vendors (
    VendorId         INTEGER  PRIMARY KEY AUTOINCREMENT,
    VendorName       TEXT     NOT NULL,
    ContactPerson    TEXT,
    Phone            TEXT,
    Email            TEXT,
    Address          TEXT,
    CNIC             TEXT,
    IsActive         INTEGER  NOT NULL DEFAULT 1,
    IsDeleted        INTEGER  NOT NULL DEFAULT 0,
    CreatedAt        TEXT     DEFAULT (datetime('now')),
    CreatedByStaffId INTEGER,
    ModifiedBy       TEXT,
    ModifiedAt       TEXT,
    CreatedBy        INTEGER
);
CREATE TABLE IF NOT EXISTS menu_categories (
    category_id   INTEGER  PRIMARY KEY AUTOINCREMENT,
    category_name TEXT     NOT NULL UNIQUE,
    description   TEXT,
    CreatedDate   TEXT     DEFAULT (datetime('now')),
    CreatedBy     INTEGER,
    ModifiedDate  TEXT,
    ModifiedBy    INTEGER,
    IsActive      INTEGER  DEFAULT 1
);
CREATE TABLE IF NOT EXISTS menu_items (
    item_id        INTEGER  PRIMARY KEY AUTOINCREMENT,
    subcategory_id INTEGER  NOT NULL,
    item_name      TEXT     NOT NULL,
    description    TEXT,
    image_src      TEXT,
    price1         REAL,
    price2         REAL,
    count1         INTEGER,
    count2         INTEGER,
    title          TEXT,
    CreatedDate    TEXT     DEFAULT (datetime('now')),
    CreatedBy      INTEGER,
    ModifiedDate   TEXT,
    ModifiedBy     INTEGER,
    IsActive       INTEGER  DEFAULT 1,
    image_data     TEXT,
    FOREIGN KEY (subcategory_id) REFERENCES menu_subcategories(subcategory_id)
);
CREATE TABLE IF NOT EXISTS menu_subcategories (
    subcategory_id   INTEGER  PRIMARY KEY AUTOINCREMENT,
    category_id      INTEGER  NOT NULL,
    subcategory_name TEXT     NOT NULL,
    description      TEXT,
    display_order    INTEGER,
    CreatedDate      TEXT     DEFAULT (datetime('now')),
    CreatedBy        INTEGER,
    ModifiedDate     TEXT,
    ModifiedBy       INTEGER,
    IsActive         INTEGER  DEFAULT 1,
    FOREIGN KEY (category_id) REFERENCES menu_categories(category_id)
);
INSERT INTO "AppSettings" ("Id","SettingKey","SettingValue","CreatedBy") VALUES (1,'AppVersion','1.0',NULL),
 (2,'LastSync','',NULL);
INSERT INTO "DailyExpenses" ("DailyExpenseId","Title","Category","Amount","ExpenseDate","PaidBy","Notes","IsActive","IsDeleted","CreatedAt","ModifiedAt","ModifiedBy","PaymentMode","CreatedBy") VALUES (1,'Pneer','Raw',1213.0,'2026-03-20',NULL,NULL,1,0,'2026-03-20 08:41:41','2026-03-23 05:29:12','Admin','Cash',NULL);
INSERT INTO "OrderStatusMaster" ("Id","Name","CreatedDate","CreatedBy","ModifiedDate","ModifiedBy","IsActive") VALUES (1,'Pending','2026-03-20 07:30:58',NULL,NULL,NULL,1),
 (2,'Preparing','2026-03-20 07:30:58',NULL,NULL,NULL,1),
 (3,'Served','2026-03-20 07:30:58',NULL,NULL,NULL,1),
 (4,'Cancelled','2026-03-20 07:30:58',NULL,NULL,NULL,1),
 (5,'Paid','2026-03-20 07:30:58',NULL,NULL,NULL,1);
INSERT INTO "Roles" ("RoleId","RoleName","Description","IsActive","CreatedAt","ModifiedAt","ModifiedBy","CreatedBy") VALUES (1,'Admin',NULL,1,'2026-03-20 07:30:58',NULL,NULL,NULL),
 (2,'Manager',NULL,1,'2026-03-20 07:30:58',NULL,NULL,NULL),
 (3,'Staff',NULL,1,'2026-03-20 07:30:58',NULL,NULL,NULL);
INSERT INTO "ShopExpenses" ("ExpenseId","Title","Category","Amount","ExpenseDate","Description","IsActive","IsDeleted","CreatedAt","ModifiedAt","ModifiedBy","PaymentMode","CreatedBy") VALUES (1,'Electricity','Utitlities',12323.0,'2026-03-10','sdf',0,1,'2026-03-20 10:18:16','2026-03-20 10:55:13','Admin','Cheque',NULL);
INSERT INTO "Staff" ("StaffId","FullName","RoleId","Phone","Email","CNIC","Department","Salary","JoinDate","IsActive","IsDeleted","CreatedAt","ModifiedAt","ModifiedBy","AvalfDelivery","CreatedBy") VALUES (1,'Scurry',3,'6392363256','scurryinfotech@gmail.com','03974029387420938749','Kitchen',23423423.0,NULL,1,0,'2026-03-20 11:03:03',NULL,'Admin',0,NULL),
 (2,'Vinod Rathod',1,'90909090923','rohit@gmail.com','03974029387420938749','Kitchen',23234.0,'2026-03-28',0,1,'2026-03-21 09:01:08','2026-03-21 09:10:03','Admin',0,NULL),
 (3,'Som',1,'29374937',NULL,NULL,NULL,0.0,'2026-03-26',1,0,'2026-03-21 09:22:38',NULL,'Admin',0,NULL);
INSERT INTO "SyncLog" ("Id","TableName","RecordId","Action","IsSynced","CreatedAt","SyncedAt") VALUES (1,'DailyExpenses',1,'INSERT',1,'2026-03-20 08:41:41','2026-03-20 08:41:56'),
 (2,'ShopExpenses',1,'INSERT',1,'2026-03-20 10:18:17',NULL),
 (3,'ShopExpenses',1,'DELETE',1,'2026-03-20 10:55:14',NULL),
 (4,'Staff',1,'INSERT',1,'2026-03-20 11:03:03',NULL),
 (5,'Staff',2,'INSERT',1,'2026-03-21 09:01:08',NULL),
 (6,'Staff',2,'DELETE',1,'2026-03-21 09:10:03',NULL),
 (7,'Staff',3,'INSERT',1,'2026-03-21 09:22:38',NULL),
 (8,'DailyExpenses',1,'UPDATE',1,'2026-03-23 05:29:12','2026-03-23 05:29:15');
INSERT INTO "Users" ("Id","Username","Password","JwtToken","JwtTokenExpiryDate","CreatedDate","CreatedBy","ModifiedDate","ModifiedBy","IsActive") VALUES (1,'admin','admin123','eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...','2026-03-31 08:29:09','2026-03-31 08:29:09',1,NULL,NULL,1),
 (2,'Grill_N_Shakes','jemit','eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6IkdyaWxsX05fU2hha2VzIiwibmJmIjoxNzY3OTg3Njg4LCJleHAiOjE3NzU3NjM2ODgsImlhdCI6MTc2Nzk4NzY4OH0.OOuRacj4hnMRNRQ30D2p2IToZSdYdLdDtRm157K96ck','2026-04-09 19:41:28','2025-06-29 18:21:51',2,NULL,NULL,1);
COMMIT;
