using CanvasArt.API.Models.Common;

namespace CanvasArt.API.Models.DTOs.Frames;

public record FrameSizeDto
{
    public int Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public decimal Price { get; init; }
    public decimal FinalPrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public int Stock { get; init; }
    public string? Sku { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}

public record FrameListItemDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Material { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public string? ThumbnailPath { get; init; }
    public decimal BasePrice { get; init; }
    public decimal FinalPrice { get; init; }
    public int Stock { get; init; }
    public bool IsActive { get; init; }
}

public record FrameDetailDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Material { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImagePath { get; init; }
    public string? ThumbnailPath { get; init; }
    public decimal BasePrice { get; init; }
    public decimal FinalPrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public int Stock { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<FrameSizeDto> Sizes { get; init; } = Array.Empty<FrameSizeDto>();
}

public record FrameSizeInput
{
    public int? Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public string? Sku { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public record CreateFrameRequest
{
    public string? Code { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Material { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal BasePrice { get; init; }
    public int Stock { get; init; }
    public bool IsActive { get; init; } = true;
    public IReadOnlyList<FrameSizeInput> Sizes { get; init; } = Array.Empty<FrameSizeInput>();
}

public record UpdateFrameRequest
{
    public string Name { get; init; } = string.Empty;
    public string Material { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal BasePrice { get; init; }
    public int Stock { get; init; }
    public bool IsActive { get; init; } = true;
    public IReadOnlyList<FrameSizeInput> Sizes { get; init; } = Array.Empty<FrameSizeInput>();
}

public class FrameQuery : PagedQuery
{
    public string? Material { get; set; }
    public string? Color { get; set; }
    public bool? IsActive { get; set; }
    public int? CompatibleWithPaintingId { get; set; }
}
