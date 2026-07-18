namespace CanvasArt.API.Services.Interfaces;

/// <summary>The full set of variants produced from an uploaded painting image.</summary>
public sealed record ProcessedImageSet(
    string OriginalPath,
    string ResizedPath,
    string ThumbnailPath,
    string WatermarkPath,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    int Width,
    int Height);

/// <summary>A lightweight image + thumbnail pair (frames, slides, categories).</summary>
public sealed record SimpleImageSet(
    string ImagePath,
    string ThumbnailPath,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    int Width,
    int Height);

public interface IImageService
{
    /// <summary>Configured folder name for full-size image variants (paintings, slides).</summary>
    string ImagesFolder { get; }

    /// <summary>Configured folder name for thumbnail variants.</summary>
    string ThumbsFolder { get; }

    /// <summary>Configured folder name for frame images/thumbnails.</summary>
    string FramesFolder { get; }

    /// <summary>Configured folder name for customer testimonial photos/thumbnails.</summary>
    string CustomersFolder { get; }

    /// <summary>
    /// Validates the upload, stores the original privately, and generates resized,
    /// thumbnail and watermarked variants under the given sub-folder.
    /// </summary>
    Task<ProcessedImageSet> ProcessPaintingImageAsync(
        Stream content, string originalFileName, string subFolder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores an image plus a thumbnail (no watermark, no private original) into the given
    /// public sub-folders. Shared by slides, categories and frames so upload logic is never
    /// duplicated — callers only choose which folder pair the variants land in.
    /// </summary>
    Task<SimpleImageSet> ProcessSimpleImageAsync(
        Stream content, string originalFileName, string imageFolder, string thumbFolder, CancellationToken cancellationToken = default);

    /// <summary>Deletes the given stored filenames/relative paths if they exist. Never throws.</summary>
    void DeleteFiles(params string?[] relativePaths);

    /// <summary>Resolves a stored filename/relative path to an absolute file-system path, or null if invalid.</summary>
    string? ResolvePhysicalPath(string relativePath);

    /// <summary>Builds the public URL for a filename stored under the images folder, or null if empty.</summary>
    string? BuildImageUrl(string? fileName);

    /// <summary>Builds the public URL for a filename stored under the thumbs folder, or null if empty.</summary>
    string? BuildThumbUrl(string? fileName);

    /// <summary>Builds the public URL for a filename stored under the frames folder, or null if empty.</summary>
    string? BuildFrameUrl(string? fileName);

    /// <summary>Builds the public URL for a filename stored under the customers folder, or null if empty.</summary>
    string? BuildCustomerUrl(string? fileName);
}
