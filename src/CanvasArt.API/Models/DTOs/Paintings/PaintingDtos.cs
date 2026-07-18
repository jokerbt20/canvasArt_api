using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Tags;

namespace CanvasArt.API.Models.DTOs.Paintings;

public record PaintingSizeDto
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
    public bool IsDefault { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}

public record PaintingImageDto
{
    public int Id { get; init; }
    public string ThumbnailPath { get; init; } = string.Empty;
    /// <summary>Watermarked variant — the only full-size image exposed publicly.</summary>
    public string WatermarkPath { get; init; } = string.Empty;
    /// <summary>Resized, non-watermarked variant. Only meant for admin use (e.g. slideshow selection), not for public product display.</summary>
    public string ResizedPath { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

/// <summary>Compact projection used by listing endpoints (thumbnail only).</summary>
public record PaintingListItemDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? ThumbnailPath { get; init; }
    public decimal FromPrice { get; init; }
    public decimal FromFinalPrice { get; init; }
    public bool IsPublished { get; init; }
    public bool IsFeatured { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record PaintingDetailDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Context { get; init; }
    public IReadOnlyList<string> Colors { get; init; } = Array.Empty<string>();
    public int CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? CategorySlug { get; init; }
    public bool IsPublished { get; init; }
    public bool IsFeatured { get; init; }
    public long ViewCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<PaintingSizeDto> Sizes { get; init; } = Array.Empty<PaintingSizeDto>();
    public IReadOnlyList<PaintingImageDto> Images { get; init; } = Array.Empty<PaintingImageDto>();
    public IReadOnlyList<TagDto> Tags { get; init; } = Array.Empty<TagDto>();
    public IReadOnlyList<CompatibleFrameDto> CompatibleFrames { get; init; } = Array.Empty<CompatibleFrameDto>();
}

public record CompatibleFrameDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Material { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public string? ThumbnailPath { get; init; }
    public decimal BasePrice { get; init; }
    public decimal FinalPrice { get; init; }
}

// ----- Write models -----

public record PaintingSizeInput
{
    public int? Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public string? Sku { get; init; }
    public bool IsDefault { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public record CreatePaintingRequest
{
    public string? Code { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public string? Context { get; init; }
    public IReadOnlyList<string>? Colors { get; init; }
    public int CategoryId { get; init; }
    public bool IsPublished { get; init; }
    public bool IsFeatured { get; init; }
    public IReadOnlyList<PaintingSizeInput> Sizes { get; init; } = Array.Empty<PaintingSizeInput>();
    public IReadOnlyList<int>? TagIds { get; init; }
    public IReadOnlyList<int>? CompatibleFrameIds { get; init; }
}

public record UpdatePaintingRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public string? Context { get; init; }
    public IReadOnlyList<string>? Colors { get; init; }
    public int CategoryId { get; init; }
    public bool IsPublished { get; init; }
    public bool IsFeatured { get; init; }
    public IReadOnlyList<PaintingSizeInput> Sizes { get; init; } = Array.Empty<PaintingSizeInput>();
    public IReadOnlyList<int>? TagIds { get; init; }
    public IReadOnlyList<int>? CompatibleFrameIds { get; init; }
}

public class PaintingQuery : PagedQuery
{
    public int? CategoryId { get; set; }
    public string? CategorySlug { get; set; }
    public int? TagId { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsFeatured { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Color { get; set; }
}
