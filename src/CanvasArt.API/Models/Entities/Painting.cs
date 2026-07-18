namespace CanvasArt.API.Models.Entities;

public class Painting
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Context { get; set; }
    /// <summary>Comma-separated dominant colours (e.g. "#1A2B3C,#FFFFFF").</summary>
    public string? Colors { get; set; }
    public int CategoryId { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
    public long ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Read-only projections populated by joins.
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }
}
