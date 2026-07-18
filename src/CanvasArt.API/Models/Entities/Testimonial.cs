namespace CanvasArt.API.Models.Entities;

public class Testimonial
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public byte? Rating { get; set; }
    public string? ImagePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
