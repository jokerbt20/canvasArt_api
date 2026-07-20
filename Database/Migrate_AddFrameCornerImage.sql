/*
  Migrate_AddFrameCornerImage.sql

  Run once against an already-deployed CanvasArt database to add the frame-corner-image
  column used by the frame-room-preview feature (a transparent-background top-left corner
  PNG that FrameCompositor tiles/mirrors into a full frame around a painting).

  Idempotent: safe to run more than once.
*/

IF COL_LENGTH('dbo.Frames', 'CornerImagePath') IS NULL
    ALTER TABLE dbo.Frames ADD CornerImagePath NVARCHAR(400) NULL;
