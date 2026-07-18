namespace CanvasArt.API.Settings;

public sealed class ImageSettings
{
    public const string SectionName = "Images";

    /// <summary>
    /// Physical folder holding private originals; never served. Absolute, or relative to the
    /// application content root.
    /// </summary>
    public string PrivateStorageRoot { get; set; } = "storage/private";

    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024; // 10 MB
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".webp" };
    public string[] AllowedContentTypes { get; set; } = { "image/jpeg", "image/png", "image/webp" };

    public int ResizedMaxDimension { get; set; } = 1600;
    public int WatermarkMaxDimension { get; set; } = 1200;
    public int ThumbnailMaxDimension { get; set; } = 400;
    public int JpegQuality { get; set; } = 82;

    public string WatermarkText { get; set; } = "CanvasArt";
    public float WatermarkOpacity { get; set; } = 0.5f;

    /// <summary>
    /// Logo image drawn bottom-right on watermarked variants. Absolute, or relative to the
    /// application content root. Falls back to the tiled text watermark if the file is missing.
    /// </summary>
    public string WatermarkLogoPath { get; set; } = "Assets/logo.png";
}
