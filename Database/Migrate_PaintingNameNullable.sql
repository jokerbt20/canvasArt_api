/*
  Migrate_PaintingNameNullable.sql

  Makes dbo.Paintings.Name optional. A painting with no name shows no title on the site;
  its slug and the name snapshotted onto cart/order lines fall back to the painting Code.

  IX_Paintings_Published INCLUDEs Name, which blocks ALTER COLUMN, so it is dropped and
  recreated around the change.

  Run once against an already-deployed CanvasArt database. Idempotent: safe to run more than
  once.
*/

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.Paintings') AND name = 'Name' AND is_nullable = 0)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Paintings_Published' AND object_id = OBJECT_ID('dbo.Paintings'))
        DROP INDEX IX_Paintings_Published ON dbo.Paintings;

    ALTER TABLE dbo.Paintings ALTER COLUMN Name NVARCHAR(200) NULL;

    CREATE INDEX IX_Paintings_Published ON dbo.Paintings (IsPublished, IsFeatured) INCLUDE (Name, CreatedAt);
END
GO
