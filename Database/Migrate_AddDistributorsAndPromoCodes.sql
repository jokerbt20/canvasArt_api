/*
  Migrate_AddDistributorsAndPromoCodes.sql

  Adds distributor + promo-code support:
    - dbo.Distributors            : sales distributors.
    - dbo.PromoCodes              : distributor-owned percentage codes (active flag per code).
    - dbo.Orders promo columns    : attribution snapshot (distributor + code + extra discount).

  A customer may order with or without a promo code. When a code is used, the order records the
  owning distributor and the extra discount so sales attribution survives later edits/deletes.

  Run once against an already-deployed CanvasArt database. Idempotent: safe to run more than once.
*/

-- ----- Distributors -----
IF OBJECT_ID(N'dbo.Distributors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Distributors
    (
        Id        INT           IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(200) NOT NULL,
        Email     NVARCHAR(256) NULL,
        Phone     NVARCHAR(40)  NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_Distributors_IsActive DEFAULT (1),
        CreatedAt DATETIME2(3)  NOT NULL CONSTRAINT DF_Distributors_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(3)  NOT NULL CONSTRAINT DF_Distributors_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Distributors PRIMARY KEY CLUSTERED (Id)
    );
    CREATE INDEX IX_Distributors_IsActive ON dbo.Distributors (IsActive) INCLUDE (Name);
END
GO

-- ----- Promo codes -----
IF OBJECT_ID(N'dbo.PromoCodes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PromoCodes
    (
        Id                 INT           IDENTITY(1,1) NOT NULL,
        DistributorId      INT           NOT NULL,
        Code               NVARCHAR(50)  NOT NULL,
        DiscountPercentage DECIMAL(5,2)  NOT NULL,
        IsActive           BIT           NOT NULL CONSTRAINT DF_PromoCodes_IsActive DEFAULT (1),
        CreatedAt          DATETIME2(3)  NOT NULL CONSTRAINT DF_PromoCodes_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt          DATETIME2(3)  NOT NULL CONSTRAINT DF_PromoCodes_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_PromoCodes PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_PromoCodes_Code UNIQUE (Code),
        CONSTRAINT CK_PromoCodes_Percentage CHECK (DiscountPercentage > 0 AND DiscountPercentage <= 100),
        -- NO ACTION (not CASCADE): a CASCADE here plus the SET NULL FKs on Orders would give SQL
        -- Server multiple cascade paths to dbo.Orders. DistributorService deletes codes first.
        CONSTRAINT FK_PromoCodes_Distributors FOREIGN KEY (DistributorId)
            REFERENCES dbo.Distributors (Id)
    );
    CREATE INDEX IX_PromoCodes_DistributorId ON dbo.PromoCodes (DistributorId);
END
GO

-- ----- Orders: attribution snapshot columns -----
IF COL_LENGTH('dbo.Orders', 'PromoCodeId') IS NULL
    ALTER TABLE dbo.Orders ADD PromoCodeId INT NULL;
GO
IF COL_LENGTH('dbo.Orders', 'DistributorId') IS NULL
    ALTER TABLE dbo.Orders ADD DistributorId INT NULL;
GO
IF COL_LENGTH('dbo.Orders', 'PromoCode') IS NULL
    ALTER TABLE dbo.Orders ADD PromoCode NVARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.Orders', 'DistributorName') IS NULL
    ALTER TABLE dbo.Orders ADD DistributorName NVARCHAR(200) NULL;
GO
IF COL_LENGTH('dbo.Orders', 'PromoDiscount') IS NULL
    ALTER TABLE dbo.Orders ADD PromoDiscount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_PromoDiscount DEFAULT (0);
GO

-- FKs use ON DELETE SET NULL so removing a code/distributor keeps the order's snapshot intact.
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Orders_PromoCodes')
    ALTER TABLE dbo.Orders ADD CONSTRAINT FK_Orders_PromoCodes
        FOREIGN KEY (PromoCodeId) REFERENCES dbo.PromoCodes (Id) ON DELETE SET NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Orders_Distributors')
    ALTER TABLE dbo.Orders ADD CONSTRAINT FK_Orders_Distributors
        FOREIGN KEY (DistributorId) REFERENCES dbo.Distributors (Id) ON DELETE SET NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_DistributorId' AND object_id = OBJECT_ID('dbo.Orders'))
    CREATE INDEX IX_Orders_DistributorId ON dbo.Orders (DistributorId) INCLUDE (Status, GrandTotal, PromoDiscount, CreatedAt);
GO
