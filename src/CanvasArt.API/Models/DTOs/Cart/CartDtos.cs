namespace CanvasArt.API.Models.DTOs.Cart;

/// <summary>A single line the guest wishes to price: painting + size, optional frame + size.</summary>
public record CartLineRequest
{
    public int PaintingId { get; init; }
    public int PaintingSizeId { get; init; }
    public int? FrameId { get; init; }
    public int? FrameSizeId { get; init; }
    public int Quantity { get; init; } = 1;
}

public record CartRequest
{
    public IReadOnlyList<CartLineRequest> Items { get; init; } = Array.Empty<CartLineRequest>();
}

public record CartLineResponse
{
    public int PaintingId { get; init; }
    public string PaintingName { get; init; } = string.Empty;
    public string? ThumbnailPath { get; init; }
    public int PaintingSizeId { get; init; }
    public string SizeLabel { get; init; } = string.Empty;
    public int? FrameId { get; init; }
    public string? FrameName { get; init; }
    public int? FrameSizeId { get; init; }
    public string? FrameSizeLabel { get; init; }

    public decimal PaintingUnitPrice { get; init; }
    public decimal FrameUnitPrice { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal UnitDiscount { get; init; }
    public decimal UnitFinalPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineSubTotal { get; init; }
    public decimal LineDiscount { get; init; }
    public decimal LineTotal { get; init; }
    public string? AppliedPromotion { get; init; }
}

public record CartResponse
{
    public IReadOnlyList<CartLineResponse> Items { get; init; } = Array.Empty<CartLineResponse>();
    public decimal SubTotal { get; init; }
    public decimal DiscountTotal { get; init; }
    public decimal GrandTotal { get; init; }
    public int TotalQuantity { get; init; }
}
