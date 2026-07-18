using CanvasArt.API.Models.Common;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CanvasArt.API.Services;

/// <summary>
/// ImageSharp-backed image pipeline. Validates uploads, stores the original privately and
/// produces resized, thumbnail and watermarked variants under the public folder. Public
/// variants are stored in the DB as bare filenames; <see cref="UploadsSettings"/> is the single
/// source of truth for which physical folder each variant lives in and for the public URL
/// (<see cref="BuildImageUrl"/>/<see cref="BuildThumbUrl"/>/<see cref="BuildFrameUrl"/>) used to
/// expose it — identical logic in every environment, only config differs.
/// </summary>
public sealed class ImageService : IImageService, IDisposable
{
    private readonly ImageSettings _settings;
    private readonly UploadsSettings _uploads;
    private readonly ILogger<ImageService> _logger;
    private readonly string _publicStorageRoot;
    private readonly string _privateStorageRoot;
    private readonly Image<Rgba32>? _logoImage;

    public string ImagesFolder => _uploads.ImagesFolder;
    public string ThumbsFolder => _uploads.ThumbsFolder;
    public string FramesFolder => _uploads.FramesFolder;
    public string CustomersFolder => _uploads.CustomersFolder;

    public ImageService(
        IOptions<ImageSettings> options, IOptions<UploadsSettings> uploadsOptions, IHostEnvironment env, ILogger<ImageService> logger)
    {
        _settings = options.Value;
        _uploads = uploadsOptions.Value;
        _logger = logger;
        _publicStorageRoot = ResolveRoot(env.ContentRootPath, _uploads.PhysicalRoot);
        _privateStorageRoot = ResolveRoot(env.ContentRootPath, _settings.PrivateStorageRoot);
        Directory.CreateDirectory(_publicStorageRoot);
        Directory.CreateDirectory(_privateStorageRoot);
        _logoImage = LoadLogo(env.ContentRootPath);
    }

    public void Dispose() => _logoImage?.Dispose();

    private Image<Rgba32>? LoadLogo(string contentRootPath)
    {
        var logoPath = ResolveRoot(contentRootPath, _settings.WatermarkLogoPath);
        if (!File.Exists(logoPath))
        {
            _logger.LogWarning("Watermark logo not found at {Path}; falling back to text watermark.", logoPath);
            return null;
        }

        try
        {
            return Image.Load<Rgba32>(logoPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load watermark logo from {Path}; falling back to text watermark.", logoPath);
            return null;
        }
    }

    private static string ResolveRoot(string contentRootPath, string configuredPath) =>
        Path.GetFullPath(Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(contentRootPath, configuredPath));

    public async Task<ProcessedImageSet> ProcessPaintingImageAsync(
        Stream content, string originalFileName, string subFolder, CancellationToken cancellationToken = default)
    {
        var (bytes, ext) = await ReadAndValidateAsync(content, originalFileName, cancellationToken);

        using var image = LoadImage(bytes);
        var width = image.Width;
        var height = image.Height;

        var name = BuildFileName();
        var safeSub = SanitizeSubFolder(subFolder);

        // Private original (relative to the private root; never publicly exposed).
        var originalRel = $"{safeSub}/{name}{ext}";
        await WriteBytesAsync(_privateStorageRoot, originalRel, bytes, cancellationToken);

        // Public variants — bare filenames; the folder each lives in is implied by which field they populate.
        var resizedName = $"{name}_resized.jpg";
        var thumbName = $"{name}_thumb.jpg";
        var watermarkName = $"{name}_wm.jpg";

        SaveResized(image, _uploads.ImagesFolder, resizedName, _settings.ResizedMaxDimension);
        SaveResized(image, _uploads.ThumbsFolder, thumbName, _settings.ThumbnailMaxDimension);
        SaveWatermarked(image, _uploads.ImagesFolder, watermarkName, _settings.WatermarkMaxDimension);

        return new ProcessedImageSet(
            originalRel, resizedName, thumbName, watermarkName,
            Path.GetFileName(originalFileName), "image/jpeg", bytes.LongLength, width, height);
    }

    public async Task<SimpleImageSet> ProcessSimpleImageAsync(
        Stream content, string originalFileName, string imageFolder, string thumbFolder, CancellationToken cancellationToken = default)
    {
        var (bytes, _) = await ReadAndValidateAsync(content, originalFileName, cancellationToken);

        using var image = LoadImage(bytes);
        var width = image.Width;
        var height = image.Height;

        var name = BuildFileName();

        var imageName = $"{name}.jpg";
        var thumbName = $"{name}_thumb.jpg";

        SaveResized(image, imageFolder, imageName, _settings.ResizedMaxDimension);
        SaveResized(image, thumbFolder, thumbName, _settings.ThumbnailMaxDimension);

        return new SimpleImageSet(imageName, thumbName, Path.GetFileName(originalFileName), "image/jpeg", bytes.LongLength, width, height);
    }

    public void DeleteFiles(params string?[] relativePaths)
    {
        foreach (var path in relativePaths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;
            try
            {
                var physical = ResolvePhysicalPath(path);
                if (physical is not null && File.Exists(physical))
                    File.Delete(physical);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete image file {Path}", path);
            }
        }
    }

    public string? ResolvePhysicalPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var normalized = relativePath.Replace('\\', '/').TrimStart('/');

        // Bare filename (the current, canonical DB format): find which public sub-folder it
        // actually lives in by probing each known folder — filenames are timestamp+guid based,
        // so collisions across folders are not a practical concern.
        if (!normalized.Contains('/'))
        {
            foreach (var folder in PublicFolders())
            {
                var candidate = Path.GetFullPath(Path.Combine(_publicStorageRoot, folder, normalized));
                if (candidate.StartsWith(_publicStorageRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(candidate))
                    return candidate;
            }
            return null;
        }

        // Legacy / pre-migration formats: "/uploads/images/xxx.jpg" or "uploads/images/xxx.jpg".
        var legacyPrefix = "uploads/";
        if (normalized.StartsWith(legacyPrefix, StringComparison.OrdinalIgnoreCase))
            normalized = normalized[legacyPrefix.Length..];

        var firstSegmentEnd = normalized.IndexOf('/');
        var firstSegment = firstSegmentEnd >= 0 ? normalized[..firstSegmentEnd] : normalized;
        if (PublicFolders().Contains(firstSegment, StringComparer.OrdinalIgnoreCase))
        {
            var combined = Path.GetFullPath(Path.Combine(_publicStorageRoot, normalized));
            return combined.StartsWith(_publicStorageRoot, StringComparison.OrdinalIgnoreCase) ? combined : null;
        }

        // Otherwise it's a private original's relative path (e.g. "paintings/123/name.jpg").
        var privateCombined = Path.GetFullPath(Path.Combine(_privateStorageRoot, normalized));
        return privateCombined.StartsWith(_privateStorageRoot, StringComparison.OrdinalIgnoreCase) ? privateCombined : null;
    }

    public string? BuildImageUrl(string? fileName) => BuildPublicUrl(fileName, _uploads.ImagesFolder);

    public string? BuildThumbUrl(string? fileName) => BuildPublicUrl(fileName, _uploads.ThumbsFolder);

    public string? BuildFrameUrl(string? fileName) => BuildPublicUrl(fileName, _uploads.FramesFolder);

    public string? BuildCustomerUrl(string? fileName) => BuildPublicUrl(fileName, _uploads.CustomersFolder);

    // ----- helpers -----

    private IEnumerable<string> PublicFolders()
    {
        yield return _uploads.ImagesFolder;
        yield return _uploads.ThumbsFolder;
        yield return _uploads.FramesFolder;
        yield return _uploads.CustomersFolder;
    }

    private string? BuildPublicUrl(string? fileName, string folder)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        if (fileName.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return fileName;

        // Defensive: a not-yet-migrated row may still hold "/uploads/images/xxx.jpg" — reduce to
        // just the basename so the URL is still built correctly against the current convention.
        var name = fileName.Contains('/') || fileName.Contains('\\')
            ? Path.GetFileName(fileName.Replace('\\', '/').TrimEnd('/'))
            : fileName;

        return $"{_uploads.BaseUrl.TrimEnd('/')}/{folder}/{name}";
    }

    private async Task<(byte[] Bytes, string Ext)> ReadAndValidateAsync(Stream content, string fileName, CancellationToken ct)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !_settings.AllowedExtensions.Contains(ext))
            throw new ValidationException($"Unsupported file type. Allowed: {string.Join(", ", _settings.AllowedExtensions)}.");

        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        if (ms.Length == 0)
            throw new ValidationException("The uploaded file is empty.");
        if (ms.Length > _settings.MaxUploadBytes)
            throw new ValidationException($"File exceeds the maximum size of {_settings.MaxUploadBytes / (1024 * 1024)} MB.");

        return (ms.ToArray(), ext == ".jpeg" ? ".jpg" : ext);
    }

    private static Image<Rgba32> LoadImage(byte[] bytes)
    {
        try
        {
            return Image.Load<Rgba32>(bytes);
        }
        catch (Exception ex)
        {
            throw new ValidationException($"The uploaded file is not a valid image. {ex.Message}");
        }
    }

    private void SaveResized(Image<Rgba32> source, string folder, string fileName, int maxDimension)
    {
        using var clone = source.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maxDimension, maxDimension)
        }));
        SavePublic(clone, folder, fileName);
    }

    private void SaveWatermarked(Image<Rgba32> source, string folder, string fileName, int maxDimension)
    {
        using var clone = source.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maxDimension, maxDimension)
        }));

        ApplyWatermark(clone);
        SavePublic(clone, folder, fileName);
    }

    private void ApplyWatermark(Image<Rgba32> image)
    {
        if (_logoImage is not null)
        {
            ApplyLogoWatermark(image);
            return;
        }

        var alpha = (byte)Math.Clamp(_settings.WatermarkOpacity * 255f, 0f, 255f);
        var color = Color.FromRgba(255, 255, 255, alpha);

        if (TryCreateFont(image.Width, out var font))
        {
            var step = Math.Max(180, image.Width / 4);
            image.Mutate(ctx =>
            {
                for (var y = 0; y < image.Height + step; y += step)
                for (var x = -step; x < image.Width; x += step)
                    ctx.DrawText(_settings.WatermarkText, font!, color, new PointF(x, y));
            });
        }
        else
        {
            // Fallback: translucent diagonal band so the variant is still clearly marked.
            _logger.LogWarning("No system fonts available; applying a fontless watermark band.");
            var band = new SixLabors.ImageSharp.Drawing.RectangularPolygon(
                0, image.Height / 2f - 20, image.Width, 40);
            image.Mutate(ctx => ctx.Fill(Color.FromRgba(255, 255, 255, (byte)(alpha / 2)), band));
        }
    }

    private void ApplyLogoWatermark(Image<Rgba32> image)
    {
        const int marginFraction = 40; // margin ~= 2.5% of the shorter side
        const int maxLogoWidthFraction = 5; // logo width capped at ~20% of the image width

        var maxLogoWidth = Math.Max(1, image.Width / maxLogoWidthFraction);
        var scale = Math.Min(1f, (float)maxLogoWidth / _logoImage!.Width);
        var logoSize = new Size(
            Math.Max(1, (int)(_logoImage.Width * scale)),
            Math.Max(1, (int)(_logoImage.Height * scale)));

        var margin = Math.Max(8, Math.Min(image.Width, image.Height) / marginFraction);
        var location = new Point(
            Math.Max(0, image.Width - logoSize.Width - margin),
            Math.Max(0, image.Height - logoSize.Height - margin));

        using var logo = _logoImage.Clone(ctx => ctx.Resize(logoSize));
        image.Mutate(ctx => ctx.DrawImage(logo, location, _settings.WatermarkOpacity));
    }

    private bool TryCreateFont(int imageWidth, out Font? font)
    {
        font = null;
        try
        {
            if (!SystemFonts.Families.Any())
                return false;

            var family = SystemFonts.Families.FirstOrDefault(f =>
                             f.Name.Equals("Arial", StringComparison.OrdinalIgnoreCase) ||
                             f.Name.Equals("Segoe UI", StringComparison.OrdinalIgnoreCase) ||
                             f.Name.Equals("DejaVu Sans", StringComparison.OrdinalIgnoreCase));
            if (family == default)
                family = SystemFonts.Families.First();

            var size = Math.Max(18f, imageWidth / 16f);
            font = family.CreateFont(size, FontStyle.Bold);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve a system font for watermarking.");
            return false;
        }
    }

    private void SavePublic(Image<Rgba32> image, string folder, string fileName)
    {
        var physical = Path.GetFullPath(Path.Combine(_publicStorageRoot, folder, fileName));
        if (!physical.StartsWith(_publicStorageRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Invalid storage path for '{fileName}'.");
        Directory.CreateDirectory(Path.GetDirectoryName(physical)!);
        image.Save(physical, new JpegEncoder { Quality = _settings.JpegQuality });
    }

    private static async Task WriteBytesAsync(string root, string relativePath, byte[] bytes, CancellationToken ct)
    {
        var physical = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!physical.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Invalid storage path '{relativePath}'.");
        Directory.CreateDirectory(Path.GetDirectoryName(physical)!);
        await File.WriteAllBytesAsync(physical, bytes, ct);
    }

    private static string BuildFileName() =>
        $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..40];

    private static string SanitizeSubFolder(string subFolder)
    {
        var parts = subFolder.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => new string(p.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray()))
            .Where(p => p.Length > 0);
        return string.Join('/', parts);
    }
}
