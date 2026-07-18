/* ============================================================================
   CanvasArt — Online Art Gallery & Painting Store
   Database creation script (SQL Server)

   Creates the database (if permitted), all tables, keys, constraints,
   indexes and seed data (roles, administrator, example categories, settings).

   The application is ready to run immediately after executing this script.

   Default administrator:
       Email    : admin@canvasarts.mk
       Password : Admin@Canvas2026        <-- change after first login
   ============================================================================ */

/* --- Create the database if it does not yet exist -------------------------
   On shared hosting the database usually already exists; the guard makes the
   script safe to run either way. If your account cannot CREATE DATABASE, this
   block is simply skipped because the database is already present. */
IF DB_ID(N'canvasar_database') IS NULL
BEGIN
    CREATE DATABASE [canvasar_database];
END
GO

USE [canvasar_database];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* ============================================================================
   TABLES
   ============================================================================ */

/* ----------------------------- Roles ------------------------------------- */
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        Id             INT            IDENTITY(1,1) NOT NULL,
        Name           NVARCHAR(50)   NOT NULL,
        NormalizedName NVARCHAR(50)   NOT NULL,
        Description    NVARCHAR(200)  NULL,
        CreatedAt      DATETIME2(3)   NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Roles_NormalizedName UNIQUE (NormalizedName)
    );
END
GO

/* ----------------------------- Users ------------------------------------- */
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id              INT            IDENTITY(1,1) NOT NULL,
        RoleId          INT            NOT NULL,
        Email           NVARCHAR(256)  NOT NULL,
        NormalizedEmail NVARCHAR(256)  NOT NULL,
        PasswordHash    NVARCHAR(256)  NOT NULL,
        FirstName       NVARCHAR(100)  NOT NULL,
        LastName        NVARCHAR(100)  NOT NULL,
        PhoneNumber     NVARCHAR(32)   NULL,
        IsActive        BIT            NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAt       DATETIME2(3)   NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2(3)   NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        LastLoginAt     DATETIME2(3)   NULL,
        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Users_NormalizedEmail UNIQUE (NormalizedEmail),
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id)
    );
    CREATE INDEX IX_Users_RoleId ON dbo.Users (RoleId);
END
GO

/* -------------------------- RefreshTokens -------------------------------- */
IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens
    (
        Id              BIGINT         IDENTITY(1,1) NOT NULL,
        UserId          INT            NOT NULL,
        Token           NVARCHAR(200)  NOT NULL,
        JwtId           NVARCHAR(64)   NOT NULL,
        CreatedAt       DATETIME2(3)   NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ExpiresAt       DATETIME2(3)   NOT NULL,
        IsUsed          BIT            NOT NULL CONSTRAINT DF_RefreshTokens_IsUsed DEFAULT (0),
        IsRevoked       BIT            NOT NULL CONSTRAINT DF_RefreshTokens_IsRevoked DEFAULT (0),
        RevokedAt       DATETIME2(3)   NULL,
        ReplacedByToken NVARCHAR(200)  NULL,
        CreatedByIp     NVARCHAR(64)   NULL,
        CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_RefreshTokens_Token UNIQUE (Token),
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens (UserId);
END
GO

/* --------------------------- Categories ---------------------------------- */
IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        Id           INT            IDENTITY(1,1) NOT NULL,
        ParentId     INT            NULL,
        Name         NVARCHAR(150)  NOT NULL,
        Slug         NVARCHAR(180)  NOT NULL,
        Description  NVARCHAR(1000) NULL,
        ImagePath    NVARCHAR(400)  NULL,
        DisplayOrder INT            NOT NULL CONSTRAINT DF_Categories_DisplayOrder DEFAULT (0),
        IsActive     BIT            NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1),
        CreatedAt    DATETIME2(3)   NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt    DATETIME2(3)   NOT NULL CONSTRAINT DF_Categories_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Categories_Slug UNIQUE (Slug),
        CONSTRAINT FK_Categories_Parent FOREIGN KEY (ParentId) REFERENCES dbo.Categories (Id)
    );
    CREATE INDEX IX_Categories_ParentId ON dbo.Categories (ParentId);
END
GO

/* ------------------------------ Tags ------------------------------------- */
IF OBJECT_ID(N'dbo.Tags', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tags
    (
        Id        INT           IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(80)  NOT NULL,
        Slug      NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME2(3)  NOT NULL CONSTRAINT DF_Tags_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Tags PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Tags_Slug UNIQUE (Slug),
        CONSTRAINT UQ_Tags_Name UNIQUE (Name)
    );
END
GO

/* ---------------------------- Paintings ---------------------------------- */
IF OBJECT_ID(N'dbo.Paintings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Paintings
    (
        Id          INT            IDENTITY(1,1) NOT NULL,
        Code        NVARCHAR(50)   NOT NULL,
        Name        NVARCHAR(200)  NOT NULL,
        Slug        NVARCHAR(220)  NOT NULL,
        Description NVARCHAR(MAX)  NULL,
        Context     NVARCHAR(MAX)  NULL,
        Colors      NVARCHAR(500)  NULL,
        CategoryId  INT            NOT NULL,
        IsPublished BIT            NOT NULL CONSTRAINT DF_Paintings_IsPublished DEFAULT (0),
        IsFeatured  BIT            NOT NULL CONSTRAINT DF_Paintings_IsFeatured DEFAULT (0),
        ViewCount   BIGINT         NOT NULL CONSTRAINT DF_Paintings_ViewCount DEFAULT (0),
        CreatedAt   DATETIME2(3)   NOT NULL CONSTRAINT DF_Paintings_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt   DATETIME2(3)   NOT NULL CONSTRAINT DF_Paintings_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Paintings PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Paintings_Code UNIQUE (Code),
        CONSTRAINT UQ_Paintings_Slug UNIQUE (Slug),
        CONSTRAINT FK_Paintings_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (Id)
    );
    CREATE INDEX IX_Paintings_CategoryId ON dbo.Paintings (CategoryId);
    CREATE INDEX IX_Paintings_Published ON dbo.Paintings (IsPublished, IsFeatured) INCLUDE (Name, CreatedAt);
    CREATE INDEX IX_Paintings_CreatedAt ON dbo.Paintings (CreatedAt DESC);
END
GO

/* -------------------------- PaintingSizes -------------------------------- */
IF OBJECT_ID(N'dbo.PaintingSizes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaintingSizes
    (
        Id           INT            IDENTITY(1,1) NOT NULL,
        PaintingId   INT            NOT NULL,
        Label        NVARCHAR(50)   NOT NULL,
        WidthCm      DECIMAL(9,2)   NOT NULL,
        HeightCm     DECIMAL(9,2)   NOT NULL,
        Price        DECIMAL(18,2)  NOT NULL,
        Stock        INT            NOT NULL CONSTRAINT DF_PaintingSizes_Stock DEFAULT (0),
        Sku          NVARCHAR(64)   NULL,
        IsDefault    BIT            NOT NULL CONSTRAINT DF_PaintingSizes_IsDefault DEFAULT (0),
        DisplayOrder INT            NOT NULL CONSTRAINT DF_PaintingSizes_DisplayOrder DEFAULT (0),
        IsActive     BIT            NOT NULL CONSTRAINT DF_PaintingSizes_IsActive DEFAULT (1),
        CONSTRAINT PK_PaintingSizes PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_PaintingSizes_Paintings FOREIGN KEY (PaintingId) REFERENCES dbo.Paintings (Id) ON DELETE CASCADE,
        CONSTRAINT CK_PaintingSizes_Price CHECK (Price >= 0),
        CONSTRAINT CK_PaintingSizes_Stock CHECK (Stock >= 0),
        CONSTRAINT CK_PaintingSizes_Dimensions CHECK (WidthCm > 0 AND HeightCm > 0)
    );
    CREATE INDEX IX_PaintingSizes_PaintingId ON dbo.PaintingSizes (PaintingId);
END
GO

/* -------------------------- PaintingImages ------------------------------- */
IF OBJECT_ID(N'dbo.PaintingImages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaintingImages
    (
        Id            INT            IDENTITY(1,1) NOT NULL,
        PaintingId    INT            NOT NULL,
        OriginalPath  NVARCHAR(400)  NOT NULL,
        ResizedPath   NVARCHAR(400)  NOT NULL,
        ThumbnailPath NVARCHAR(400)  NOT NULL,
        WatermarkPath NVARCHAR(400)  NOT NULL,
        FileName      NVARCHAR(260)  NOT NULL,
        ContentType   NVARCHAR(100)  NOT NULL,
        FileSizeBytes BIGINT         NOT NULL CONSTRAINT DF_PaintingImages_FileSizeBytes DEFAULT (0),
        Width         INT            NOT NULL CONSTRAINT DF_PaintingImages_Width DEFAULT (0),
        Height        INT            NOT NULL CONSTRAINT DF_PaintingImages_Height DEFAULT (0),
        IsPrimary     BIT            NOT NULL CONSTRAINT DF_PaintingImages_IsPrimary DEFAULT (0),
        DisplayOrder  INT            NOT NULL CONSTRAINT DF_PaintingImages_DisplayOrder DEFAULT (0),
        CreatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_PaintingImages_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_PaintingImages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_PaintingImages_Paintings FOREIGN KEY (PaintingId) REFERENCES dbo.Paintings (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_PaintingImages_PaintingId ON dbo.PaintingImages (PaintingId, IsPrimary DESC, DisplayOrder);
END
GO

/* --------------------------- PaintingTags -------------------------------- */
IF OBJECT_ID(N'dbo.PaintingTags', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaintingTags
    (
        PaintingId INT NOT NULL,
        TagId      INT NOT NULL,
        CONSTRAINT PK_PaintingTags PRIMARY KEY CLUSTERED (PaintingId, TagId),
        CONSTRAINT FK_PaintingTags_Paintings FOREIGN KEY (PaintingId) REFERENCES dbo.Paintings (Id) ON DELETE CASCADE,
        CONSTRAINT FK_PaintingTags_Tags FOREIGN KEY (TagId) REFERENCES dbo.Tags (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_PaintingTags_TagId ON dbo.PaintingTags (TagId);
END
GO

/* ------------------------------ Frames ----------------------------------- */
IF OBJECT_ID(N'dbo.Frames', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Frames
    (
        Id            INT            IDENTITY(1,1) NOT NULL,
        Code          NVARCHAR(50)   NOT NULL,
        Name          NVARCHAR(150)  NOT NULL,
        Material      NVARCHAR(80)   NOT NULL,
        Color         NVARCHAR(60)   NOT NULL,
        Description   NVARCHAR(1000) NULL,
        ImagePath     NVARCHAR(400)  NULL,
        ThumbnailPath NVARCHAR(400)  NULL,
        BasePrice     DECIMAL(18,2)  NOT NULL CONSTRAINT DF_Frames_BasePrice DEFAULT (0),
        Stock         INT            NOT NULL CONSTRAINT DF_Frames_Stock DEFAULT (0),
        IsActive      BIT            NOT NULL CONSTRAINT DF_Frames_IsActive DEFAULT (1),
        CreatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Frames_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Frames_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Frames PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Frames_Code UNIQUE (Code),
        CONSTRAINT CK_Frames_BasePrice CHECK (BasePrice >= 0),
        CONSTRAINT CK_Frames_Stock CHECK (Stock >= 0)
    );
    CREATE INDEX IX_Frames_IsActive ON dbo.Frames (IsActive);
END
GO

/* ---------------------------- FrameSizes --------------------------------- */
IF OBJECT_ID(N'dbo.FrameSizes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FrameSizes
    (
        Id           INT            IDENTITY(1,1) NOT NULL,
        FrameId      INT            NOT NULL,
        Label        NVARCHAR(50)   NOT NULL,
        WidthCm      DECIMAL(9,2)   NOT NULL,
        HeightCm     DECIMAL(9,2)   NOT NULL,
        Price        DECIMAL(18,2)  NOT NULL,
        Stock        INT            NOT NULL CONSTRAINT DF_FrameSizes_Stock DEFAULT (0),
        Sku          NVARCHAR(64)   NULL,
        DisplayOrder INT            NOT NULL CONSTRAINT DF_FrameSizes_DisplayOrder DEFAULT (0),
        IsActive     BIT            NOT NULL CONSTRAINT DF_FrameSizes_IsActive DEFAULT (1),
        CONSTRAINT PK_FrameSizes PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_FrameSizes_Frames FOREIGN KEY (FrameId) REFERENCES dbo.Frames (Id) ON DELETE CASCADE,
        CONSTRAINT CK_FrameSizes_Price CHECK (Price >= 0),
        CONSTRAINT CK_FrameSizes_Stock CHECK (Stock >= 0),
        CONSTRAINT CK_FrameSizes_Dimensions CHECK (WidthCm > 0 AND HeightCm > 0)
    );
    CREATE INDEX IX_FrameSizes_FrameId ON dbo.FrameSizes (FrameId);
END
GO

/* ----------------------- FrameCompatibilities ---------------------------- */
IF OBJECT_ID(N'dbo.FrameCompatibilities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FrameCompatibilities
    (
        Id         INT          IDENTITY(1,1) NOT NULL,
        PaintingId INT          NOT NULL,
        FrameId    INT          NOT NULL,
        CreatedAt  DATETIME2(3) NOT NULL CONSTRAINT DF_FrameCompat_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_FrameCompatibilities PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_FrameCompatibilities UNIQUE (PaintingId, FrameId),
        CONSTRAINT FK_FrameCompat_Paintings FOREIGN KEY (PaintingId) REFERENCES dbo.Paintings (Id) ON DELETE CASCADE,
        CONSTRAINT FK_FrameCompat_Frames FOREIGN KEY (FrameId) REFERENCES dbo.Frames (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_FrameCompat_FrameId ON dbo.FrameCompatibilities (FrameId);
END
GO

/* ---------------------------- Promotions --------------------------------- */
IF OBJECT_ID(N'dbo.Promotions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Promotions
    (
        Id               INT            IDENTITY(1,1) NOT NULL,
        Name             NVARCHAR(150)  NOT NULL,
        Description      NVARCHAR(1000) NULL,
        PromotionType    INT            NOT NULL,   -- 0 Painting, 1 Frame, 2 Combination
        DiscountType     INT            NOT NULL,   -- 0 Percentage, 1 FixedAmount
        DiscountValue    DECIMAL(18,2)  NOT NULL,
        TargetPaintingId INT            NULL,
        TargetFrameId    INT            NULL,
        TargetCategoryId INT            NULL,
        StartDate        DATETIME2(3)   NOT NULL,
        EndDate          DATETIME2(3)   NOT NULL,
        IsActive         BIT            NOT NULL CONSTRAINT DF_Promotions_IsActive DEFAULT (1),
        Priority         INT            NOT NULL CONSTRAINT DF_Promotions_Priority DEFAULT (0),
        CreatedAt        DATETIME2(3)   NOT NULL CONSTRAINT DF_Promotions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt        DATETIME2(3)   NOT NULL CONSTRAINT DF_Promotions_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Promotions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Promotions_Type CHECK (PromotionType IN (0,1,2)),
        CONSTRAINT CK_Promotions_DiscountType CHECK (DiscountType IN (0,1)),
        CONSTRAINT CK_Promotions_DiscountValue CHECK (DiscountValue >= 0),
        CONSTRAINT CK_Promotions_Dates CHECK (EndDate >= StartDate),
        CONSTRAINT FK_Promotions_Paintings FOREIGN KEY (TargetPaintingId) REFERENCES dbo.Paintings (Id) ON DELETE SET NULL,
        CONSTRAINT FK_Promotions_Frames FOREIGN KEY (TargetFrameId) REFERENCES dbo.Frames (Id) ON DELETE SET NULL,
        CONSTRAINT FK_Promotions_Categories FOREIGN KEY (TargetCategoryId) REFERENCES dbo.Categories (Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_Promotions_Active ON dbo.Promotions (IsActive, StartDate, EndDate) INCLUDE (PromotionType, DiscountType, DiscountValue, Priority);
    CREATE INDEX IX_Promotions_TargetPainting ON dbo.Promotions (TargetPaintingId);
    CREATE INDEX IX_Promotions_TargetFrame ON dbo.Promotions (TargetFrameId);
    CREATE INDEX IX_Promotions_TargetCategory ON dbo.Promotions (TargetCategoryId);
END
GO

/* ----------------------- CombinationPromotions --------------------------- */
IF OBJECT_ID(N'dbo.CombinationPromotions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CombinationPromotions
    (
        Id            INT            IDENTITY(1,1) NOT NULL,
        Name          NVARCHAR(150)  NOT NULL,
        Description   NVARCHAR(1000) NULL,
        PaintingId    INT            NOT NULL,
        FrameId       INT            NOT NULL,
        DiscountType  INT            NOT NULL,
        DiscountValue DECIMAL(18,2)  NOT NULL,
        StartDate     DATETIME2(3)   NOT NULL,
        EndDate       DATETIME2(3)   NOT NULL,
        IsActive      BIT            NOT NULL CONSTRAINT DF_ComboPromotions_IsActive DEFAULT (1),
        Priority      INT            NOT NULL CONSTRAINT DF_ComboPromotions_Priority DEFAULT (0),
        CreatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_ComboPromotions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_ComboPromotions_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_CombinationPromotions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ComboPromotions_DiscountType CHECK (DiscountType IN (0,1)),
        CONSTRAINT CK_ComboPromotions_DiscountValue CHECK (DiscountValue >= 0),
        CONSTRAINT CK_ComboPromotions_Dates CHECK (EndDate >= StartDate),
        CONSTRAINT FK_ComboPromotions_Paintings FOREIGN KEY (PaintingId) REFERENCES dbo.Paintings (Id) ON DELETE CASCADE,
        CONSTRAINT FK_ComboPromotions_Frames FOREIGN KEY (FrameId) REFERENCES dbo.Frames (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_ComboPromotions_Pairing ON dbo.CombinationPromotions (PaintingId, FrameId, IsActive, StartDate, EndDate);
    CREATE INDEX IX_ComboPromotions_FrameId ON dbo.CombinationPromotions (FrameId);
END
GO

/* ------------------------------ Slides ----------------------------------- */
IF OBJECT_ID(N'dbo.Slides', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Slides
    (
        Id           INT            IDENTITY(1,1) NOT NULL,
        Title        NVARCHAR(150)  NOT NULL,
        Subtitle     NVARCHAR(300)  NULL,
        ImagePath    NVARCHAR(400)  NOT NULL,
        LinkUrl      NVARCHAR(500)  NULL,
        ButtonText   NVARCHAR(80)   NULL,
        DisplayOrder INT            NOT NULL CONSTRAINT DF_Slides_DisplayOrder DEFAULT (0),
        IsActive     BIT            NOT NULL CONSTRAINT DF_Slides_IsActive DEFAULT (1),
        StartDate    DATETIME2(3)   NULL,
        EndDate      DATETIME2(3)   NULL,
        CreatedAt    DATETIME2(3)   NOT NULL CONSTRAINT DF_Slides_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt    DATETIME2(3)   NOT NULL CONSTRAINT DF_Slides_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Slides PRIMARY KEY CLUSTERED (Id)
    );
    CREATE INDEX IX_Slides_Active ON dbo.Slides (IsActive, DisplayOrder);
END
GO

/* ----------------------------- Settings ---------------------------------- */
IF OBJECT_ID(N'dbo.Settings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Settings
    (
        Id          INT            IDENTITY(1,1) NOT NULL,
        [Key]       NVARCHAR(120)  NOT NULL,
        [Value]     NVARCHAR(MAX)  NULL,
        [Group]     NVARCHAR(80)   NOT NULL CONSTRAINT DF_Settings_Group DEFAULT (N'General'),
        Description NVARCHAR(400)  NULL,
        UpdatedAt   DATETIME2(3)   NOT NULL CONSTRAINT DF_Settings_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Settings PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Settings_Key UNIQUE ([Key])
    );
    CREATE INDEX IX_Settings_Group ON dbo.Settings ([Group]);
END
GO

/* ------------------------------ Orders ----------------------------------- */
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        Id            INT            IDENTITY(1,1) NOT NULL,
        OrderNumber   NVARCHAR(40)   NOT NULL,
        FirstName     NVARCHAR(100)  NOT NULL,
        LastName      NVARCHAR(100)  NOT NULL,
        Email         NVARCHAR(256)  NOT NULL,
        Phone         NVARCHAR(40)   NOT NULL,
        AddressLine   NVARCHAR(300)  NOT NULL,
        City          NVARCHAR(120)  NOT NULL,
        Country       NVARCHAR(120)  NOT NULL,
        PostalCode    NVARCHAR(20)   NOT NULL,
        Notes         NVARCHAR(1000) NULL,
        Status        INT            NOT NULL CONSTRAINT DF_Orders_Status DEFAULT (0),
        SubTotal      DECIMAL(18,2)  NOT NULL CONSTRAINT DF_Orders_SubTotal DEFAULT (0),
        DiscountTotal DECIMAL(18,2)  NOT NULL CONSTRAINT DF_Orders_DiscountTotal DEFAULT (0),
        ShippingCost  DECIMAL(18,2)  NOT NULL CONSTRAINT DF_Orders_ShippingCost DEFAULT (0),
        GrandTotal    DECIMAL(18,2)  NOT NULL CONSTRAINT DF_Orders_GrandTotal DEFAULT (0),
        CreatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Orders_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Orders_OrderNumber UNIQUE (OrderNumber),
        CONSTRAINT CK_Orders_Status CHECK (Status BETWEEN 0 AND 6)
    );
    CREATE INDEX IX_Orders_Status ON dbo.Orders (Status);
    CREATE INDEX IX_Orders_CreatedAt ON dbo.Orders (CreatedAt DESC);
    CREATE INDEX IX_Orders_Email ON dbo.Orders (Email);
END
GO

/* ---------------------------- OrderItems --------------------------------- */
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems
    (
        Id                            INT            IDENTITY(1,1) NOT NULL,
        OrderId                       INT            NOT NULL,
        PaintingId                    INT            NOT NULL,
        PaintingSizeId                INT            NOT NULL,
        FrameId                       INT            NULL,
        FrameSizeId                   INT            NULL,
        PaintingCode                  NVARCHAR(50)   NOT NULL,
        PaintingName                  NVARCHAR(200)  NOT NULL,
        SizeLabel                     NVARCHAR(50)   NOT NULL,
        FrameName                     NVARCHAR(150)  NULL,
        FrameSizeLabel                NVARCHAR(50)   NULL,
        ThumbnailPath                 NVARCHAR(400)  NULL,
        UnitPrice                     DECIMAL(18,2)  NOT NULL,
        FramePrice                    DECIMAL(18,2)  NOT NULL CONSTRAINT DF_OrderItems_FramePrice DEFAULT (0),
        DiscountAmount                DECIMAL(18,2)  NOT NULL CONSTRAINT DF_OrderItems_DiscountAmount DEFAULT (0),
        Quantity                      INT            NOT NULL,
        LineTotal                     DECIMAL(18,2)  NOT NULL,
        AppliedPromotionId            INT            NULL,
        AppliedCombinationPromotionId INT            NULL,
        CONSTRAINT PK_OrderItems PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id) ON DELETE CASCADE,
        CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_OrderItems_Amounts CHECK (UnitPrice >= 0 AND FramePrice >= 0 AND DiscountAmount >= 0 AND LineTotal >= 0)
    );
    CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems (OrderId);
END
GO

/* ----------------------- OrderStatusHistories ---------------------------- */
IF OBJECT_ID(N'dbo.OrderStatusHistories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderStatusHistories
    (
        Id              INT            IDENTITY(1,1) NOT NULL,
        OrderId         INT            NOT NULL,
        FromStatus      INT            NULL,
        ToStatus        INT            NOT NULL,
        Note            NVARCHAR(1000) NULL,
        ChangedByUserId INT            NULL,
        CreatedAt       DATETIME2(3)   NOT NULL CONSTRAINT DF_OrderStatusHistories_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_OrderStatusHistories PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_OrderStatusHistories_ToStatus CHECK (ToStatus BETWEEN 0 AND 6),
        CONSTRAINT FK_OrderStatusHistories_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id) ON DELETE CASCADE,
        CONSTRAINT FK_OrderStatusHistories_Users FOREIGN KEY (ChangedByUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_OrderStatusHistories_OrderId ON dbo.OrderStatusHistories (OrderId);
END
GO

/* --------------------------- Testimonials --------------------------------- */
IF OBJECT_ID(N'dbo.Testimonials', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Testimonials
    (
        Id            INT            IDENTITY(1,1) NOT NULL,
        CustomerName  NVARCHAR(150)  NOT NULL,
        Comment       NVARCHAR(2000) NOT NULL,
        Rating        TINYINT        NULL,
        ImagePath     NVARCHAR(400)  NULL,
        ThumbnailPath NVARCHAR(400)  NULL,
        DisplayOrder  INT            NOT NULL CONSTRAINT DF_Testimonials_DisplayOrder DEFAULT (0),
        IsActive      BIT            NOT NULL CONSTRAINT DF_Testimonials_IsActive DEFAULT (1),
        CreatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Testimonials_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Testimonials_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Testimonials PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Testimonials_Rating CHECK (Rating IS NULL OR Rating BETWEEN 1 AND 5)
    );
    CREATE INDEX IX_Testimonials_Active ON dbo.Testimonials (IsActive, DisplayOrder);
END
GO

/* --------------------------- ContactMessages ------------------------------ */
IF OBJECT_ID(N'dbo.ContactMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContactMessages
    (
        Id         INT            IDENTITY(1,1) NOT NULL,
        Name       NVARCHAR(150)  NOT NULL,
        Email      NVARCHAR(256)  NOT NULL,
        Subject    NVARCHAR(200)  NULL,
        Message    NVARCHAR(2000) NOT NULL,
        IsRead     BIT            NOT NULL CONSTRAINT DF_ContactMessages_IsRead DEFAULT (0),
        CreatedAt  DATETIME2(3)   NOT NULL CONSTRAINT DF_ContactMessages_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ContactMessages PRIMARY KEY CLUSTERED (Id)
    );
    CREATE INDEX IX_ContactMessages_CreatedAt ON dbo.ContactMessages (CreatedAt DESC);
END
GO

/* ============================================================================
   SEED DATA
   ============================================================================ */

/* ----------------------------- Roles ------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NormalizedName = N'ADMINISTRATOR')
    INSERT INTO dbo.Roles (Name, NormalizedName, Description)
    VALUES (N'Administrator', N'ADMINISTRATOR', N'Full management access to the gallery back office.');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NormalizedName = N'GUEST')
    INSERT INTO dbo.Roles (Name, NormalizedName, Description)
    VALUES (N'Guest', N'GUEST', N'Standard storefront customer.');
GO

/* ------------------------ Administrator user -----------------------------
   Password: Admin@Canvas2026  (BCrypt, work factor 12). Change after login. */
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE NormalizedEmail = N'ADMIN@CANVASARTS.MK')
BEGIN
    DECLARE @AdminRoleId INT = (SELECT Id FROM dbo.Roles WHERE NormalizedName = N'ADMINISTRATOR');
    INSERT INTO dbo.Users (RoleId, Email, NormalizedEmail, PasswordHash, FirstName, LastName, IsActive)
    VALUES
    (
        @AdminRoleId,
        N'admin@canvasarts.mk',
        N'ADMIN@CANVASARTS.MK',
        N'$2a$12$EvDTCkFr2zmwXz/Dbt6XEedZh24.6DLBw5X2bIF.LyIutHJP0NCxO',
        N'Site',
        N'Administrator',
        1
    );
END
GO

/* -------------------------- Example categories --------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
BEGIN
    INSERT INTO dbo.Categories (ParentId, Name, Slug, Description, DisplayOrder, IsActive) VALUES
        (NULL, N'Abstract',      N'abstract',      N'Bold, non-representational works.',      1, 1),
        (NULL, N'Landscapes',    N'landscapes',    N'Natural and urban scenery.',             2, 1),
        (NULL, N'Portraits',     N'portraits',     N'Studies of people and character.',       3, 1),
        (NULL, N'Still Life',    N'still-life',    N'Arranged objects and compositions.',     4, 1),
        (NULL, N'Modern',        N'modern',        N'Contemporary styles and techniques.',    5, 1),
        (NULL, N'Nature',        N'nature',        N'Flora, fauna and the great outdoors.',   6, 1);
END
GO

/* ---------------------------- Example tags ------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Tags)
BEGIN
    INSERT INTO dbo.Tags (Name, Slug) VALUES
        (N'Colorful', N'colorful'),
        (N'Minimal',  N'minimal'),
        (N'Vintage',  N'vintage'),
        (N'Large',    N'large'),
        (N'Bestseller', N'bestseller');
END
GO

/* ------------------------ CMS / storefront settings ---------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Settings)
BEGIN
    INSERT INTO dbo.Settings ([Key], [Value], [Group], Description) VALUES
        (N'site.name',           N'CanvasArt',                       N'General', N'Public site name.'),
        (N'site.tagline',        N'Original art for every wall',     N'General', N'Homepage tagline.'),
        (N'contact.email',       N'hello@canvasarts.mk',             N'Contact', N'Public contact email.'),
        (N'contact.phone',       N'+389 70 000 000',                 N'Contact', N'Public contact phone.'),
        (N'contact.address',     N'Skopje, North Macedonia',         N'Contact', N'Public contact address.'),
        (N'social.facebook',     N'https://facebook.com/canvasart',  N'Social',  N'Facebook page URL.'),
        (N'social.instagram',    N'https://instagram.com/canvasart', N'Social',  N'Instagram profile URL.'),
        (N'about.title',         N'About CanvasArt',                 N'About',   N'About-section heading.'),
        (N'about.body',          N'We curate original paintings from independent artists.', N'About', N'About-section body.'),
        (N'footer.copyright',    N'© CanvasArt. All rights reserved.', N'Footer', N'Footer copyright line.'),
        (N'shipping.flat_rate',  N'0',                               N'Shipping', N'Flat shipping fee applied to every order.');
END
GO

PRINT N'CanvasArt database schema and seed data are ready.';
GO
