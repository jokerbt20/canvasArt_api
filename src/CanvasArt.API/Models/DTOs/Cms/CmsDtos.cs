using CanvasArt.API.Models.Common;

namespace CanvasArt.API.Models.DTOs.Cms;

public record SlideDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string ImagePath { get; init; } = string.Empty;
    public string? LinkUrl { get; init; }
    public string? ButtonText { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}

public record CreateSlideRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string? LinkUrl { get; init; }
    public string? ButtonText { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}

public record UpdateSlideRequest : CreateSlideRequest;

/// <summary>Creates a slide by reusing an existing painting image (server-side copy), avoiding a client-side re-upload.</summary>
public record CreateSlideFromPaintingImageRequest : CreateSlideRequest
{
    public int PaintingImageId { get; init; }
}

public record TestimonialDto
{
    public int Id { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
    public byte? Rating { get; init; }
    public string? ImagePath { get; init; }
    public string? ThumbnailPath { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}

public record CreateTestimonialRequest
{
    public string CustomerName { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
    public byte? Rating { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateTestimonialRequest : CreateTestimonialRequest;

public record SettingDto
{
    public string Key { get; init; } = string.Empty;
    public string? Value { get; init; }
    public string Group { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record UpsertSettingRequest
{
    public string Key { get; init; } = string.Empty;
    public string? Value { get; init; }
    public string Group { get; init; } = "General";
    public string? Description { get; init; }
}

public record UpsertSettingsRequest
{
    public IReadOnlyList<UpsertSettingRequest> Settings { get; init; } = Array.Empty<UpsertSettingRequest>();
}
