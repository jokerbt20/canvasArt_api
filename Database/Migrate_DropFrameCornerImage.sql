/*
  Migrate_DropFrameCornerImage.sql

  Reverts Migrate_AddFrameCornerImage.sql. The room-preview compositor now builds the frame
  border directly from the frame's main ImagePath (uploaded with its alpha channel preserved)
  instead of a separately-uploaded corner asset, so the CornerImagePath column is no longer
  used by the application.

  Run once against an already-deployed CanvasArt database. Idempotent: safe to run more than
  once.
*/

IF COL_LENGTH('dbo.Frames', 'CornerImagePath') IS NOT NULL
    ALTER TABLE dbo.Frames DROP COLUMN CornerImagePath;
