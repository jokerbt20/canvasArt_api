using CanvasArt.API.Models.Enums;

namespace CanvasArt.API.Models.Entities;

/// <summary>
/// A promotion that only applies when a specific painting and a specific frame
/// are purchased together (a bundle discount).
/// </summary>
public class CombinationPromotion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PaintingId { get; set; }
    public int FrameId { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
