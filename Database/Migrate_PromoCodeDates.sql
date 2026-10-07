/*
  Migrate_PromoCodeDates.sql

  Adds an optional validity window to promo codes:
    - StartsAt : first day the code works (NULL = immediately).
    - EndsAt   : last day the code works, inclusive (NULL = no end date).

  Both are calendar dates; the application treats EndsAt as valid through the end of that day.

  Run once against an already-deployed CanvasArt database. Idempotent: safe to run more than once.
*/

IF COL_LENGTH('dbo.PromoCodes', 'StartsAt') IS NULL
    ALTER TABLE dbo.PromoCodes ADD StartsAt DATETIME2(3) NULL;
GO

IF COL_LENGTH('dbo.PromoCodes', 'EndsAt') IS NULL
    ALTER TABLE dbo.PromoCodes ADD EndsAt DATETIME2(3) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PromoCodes_Window')
    ALTER TABLE dbo.PromoCodes ADD CONSTRAINT CK_PromoCodes_Window
        CHECK (StartsAt IS NULL OR EndsAt IS NULL OR EndsAt >= StartsAt);
GO
