/*
  Migrate_BareImageFilenames.sql

  Run once, after deploying the centralized image storage refactor, against the CanvasArt
  database. Existing rows store public image paths prefixed with the old static-file request
  path (e.g. "/uploads/images/xxx_resized.jpg" or "uploads/images/xxx_resized.jpg"). The
  application now stores just the bare filename in these columns and builds the public URL
  dynamically (see IImageService.BuildImageUrl/BuildThumbUrl/BuildFrameUrl), so every affected
  column is rewritten here to its basename (the substring after the last '/').

  Idempotent: rows that are already bare filenames (no '/') are left untouched, so this script
  is safe to run more than once.
*/

UPDATE dbo.PaintingImages
SET ResizedPath = RIGHT(ResizedPath, CHARINDEX('/', REVERSE(ResizedPath)) - 1)
WHERE ResizedPath LIKE '%/%';

UPDATE dbo.PaintingImages
SET ThumbnailPath = RIGHT(ThumbnailPath, CHARINDEX('/', REVERSE(ThumbnailPath)) - 1)
WHERE ThumbnailPath LIKE '%/%';

UPDATE dbo.PaintingImages
SET WatermarkPath = RIGHT(WatermarkPath, CHARINDEX('/', REVERSE(WatermarkPath)) - 1)
WHERE WatermarkPath LIKE '%/%';

UPDATE dbo.Frames
SET ImagePath = RIGHT(ImagePath, CHARINDEX('/', REVERSE(ImagePath)) - 1)
WHERE ImagePath LIKE '%/%';

UPDATE dbo.Frames
SET ThumbnailPath = RIGHT(ThumbnailPath, CHARINDEX('/', REVERSE(ThumbnailPath)) - 1)
WHERE ThumbnailPath LIKE '%/%';

UPDATE dbo.Slides
SET ImagePath = RIGHT(ImagePath, CHARINDEX('/', REVERSE(ImagePath)) - 1)
WHERE ImagePath LIKE '%/%';

-- Note: PaintingImages.OriginalPath is intentionally left untouched — it's the private original
-- and keeps its "paintings/{id}/name.ext" relative form, never exposed as a public URL.
-- Note: Categories.ImagePath is intentionally left untouched — categories are out of scope for
-- this refactor (CategoryService does not go through IImageService).
