namespace CanvasArt.API.Models.Entities;

public class Frame
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public decimal BasePrice { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
