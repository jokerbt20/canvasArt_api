namespace CanvasArt.API.Models.Entities;

public class PaintingImage
{
    public int Id { get; set; }
    public int PaintingId { get; set; }
    public string OriginalPath { get; set; } = string.Empty;
    public string ResizedPath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public string WatermarkPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
