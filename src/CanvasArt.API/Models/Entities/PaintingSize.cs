namespace CanvasArt.API.Models.Entities;

public class PaintingSize
{
    public int Id { get; set; }
    public int PaintingId { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public decimal Price { get; set; }
    public string? Sku { get; set; }
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
