/*
  Migrate_RemoveFrameSizesAndStock.sql

  Frames no longer have their own sizes — every frame is compatible with every painting size,
  so the FrameSizes table and its FK from OrderItems are dropped. Stock tracking is also
  removed entirely (Frames.Stock, PaintingSizes.Stock) since the storefront no longer enforces
  inventory limits.

  Run once against an already-deployed CanvasArt database. Idempotent: safe to run more than
  once.
*/

-- OrderItems: drop the FK to FrameSizes and its denormalised columns.
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItems_FrameSizes')
    ALTER TABLE dbo.OrderItems DROP CONSTRAINT FK_OrderItems_FrameSizes;

IF COL_LENGTH('dbo.OrderItems', 'FrameSizeId') IS NOT NULL
    ALTER TABLE dbo.OrderItems DROP COLUMN FrameSizeId;

IF COL_LENGTH('dbo.OrderItems', 'FrameSizeLabel') IS NOT NULL
    ALTER TABLE dbo.OrderItems DROP COLUMN FrameSizeLabel;

-- FrameSizes: no longer used, frames price and apply uniformly regardless of painting size.
IF OBJECT_ID(N'dbo.FrameSizes', N'U') IS NOT NULL
    DROP TABLE dbo.FrameSizes;

-- Frames: drop stock tracking.
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Frames_Stock')
    ALTER TABLE dbo.Frames DROP CONSTRAINT CK_Frames_Stock;

IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Frames_Stock')
    ALTER TABLE dbo.Frames DROP CONSTRAINT DF_Frames_Stock;

IF COL_LENGTH('dbo.Frames', 'Stock') IS NOT NULL
    ALTER TABLE dbo.Frames DROP COLUMN Stock;

-- PaintingSizes: drop stock tracking.
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PaintingSizes_Stock')
    ALTER TABLE dbo.PaintingSizes DROP CONSTRAINT CK_PaintingSizes_Stock;

IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_PaintingSizes_Stock')
    ALTER TABLE dbo.PaintingSizes DROP CONSTRAINT DF_PaintingSizes_Stock;

IF COL_LENGTH('dbo.PaintingSizes', 'Stock') IS NOT NULL
    ALTER TABLE dbo.PaintingSizes DROP COLUMN Stock;
