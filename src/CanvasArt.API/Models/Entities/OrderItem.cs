namespace CanvasArt.API.Models.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int PaintingId { get; set; }
    public int PaintingSizeId { get; set; }
    public int? FrameId { get; set; }

    // Denormalised snapshot captured at purchase time.
    public string PaintingCode { get; set; } = string.Empty;
    public string PaintingName { get; set; } = string.Empty;
    public string SizeLabel { get; set; } = string.Empty;
    public string? FrameName { get; set; }
    public string? ThumbnailPath { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal FramePrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public int? AppliedPromotionId { get; set; }
    public int? AppliedCombinationPromotionId { get; set; }
}
