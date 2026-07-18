using CanvasArt.API.Models.Common;

namespace CanvasArt.API.Models.DTOs.Categories;

public record CategoryDto
{
    public int Id { get; init; }
    public int? ParentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImagePath { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public int PaintingCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateCategoryRequest
{
    public int? ParentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateCategoryRequest
{
    public int? ParentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public class CategoryQuery : PagedQuery
{
    public bool? IsActive { get; set; }
    public int? ParentId { get; set; }
}
