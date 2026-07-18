using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.Enums;

namespace CanvasArt.API.Models.DTOs.Promotions;

public record PromotionDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public PromotionType PromotionType { get; init; }
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public int? TargetPaintingId { get; init; }
    public int? TargetFrameId { get; init; }
    public int? TargetCategoryId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsActive { get; init; }
    public bool IsCurrentlyActive { get; init; }
    public int Priority { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreatePromotionRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public PromotionType PromotionType { get; init; }
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public int? TargetPaintingId { get; init; }
    public int? TargetFrameId { get; init; }
    public int? TargetCategoryId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsActive { get; init; } = true;
    public int Priority { get; init; }
}

public record UpdatePromotionRequest : CreatePromotionRequest;

public record CombinationPromotionDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int PaintingId { get; init; }
    public string? PaintingName { get; init; }
    public int FrameId { get; init; }
    public string? FrameName { get; init; }
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsActive { get; init; }
    public bool IsCurrentlyActive { get; init; }
    public int Priority { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateCombinationPromotionRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int PaintingId { get; init; }
    public int FrameId { get; init; }
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsActive { get; init; } = true;
    public int Priority { get; init; }
}

public record UpdateCombinationPromotionRequest : CreateCombinationPromotionRequest;

public class PromotionQuery : PagedQuery
{
    public PromotionType? PromotionType { get; set; }
    public bool? IsActive { get; set; }
    public bool? OnlyCurrentlyActive { get; set; }
}
