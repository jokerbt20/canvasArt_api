using CanvasArt.API.Models.Enums;

namespace CanvasArt.API.Models.Entities;

/// <summary>
/// A single-target promotion applying to a painting, a frame, or an entire category.
/// Combination (painting+frame) promotions are modelled separately by <see cref="CombinationPromotion"/>.
/// </summary>
public class Promotion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PromotionType PromotionType { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public int? TargetPaintingId { get; set; }
    public int? TargetFrameId { get; set; }
    public int? TargetCategoryId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
