namespace CanvasArt.API.Models.Enums;

/// <summary>Lifecycle of a customer order. Values are persisted as <c>int</c>.</summary>
public enum OrderStatus
{
    Pending = 0,
    Contacted = 1,
    Confirmed = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6
}

/// <summary>The kind of catalog entity a promotion targets.</summary>
public enum PromotionType
{
    Painting = 0,
    Frame = 1,
    Combination = 2
}

/// <summary>How a promotion's discount value is interpreted.</summary>
public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1
}

/// <summary>Well-known image variants generated from an uploaded original.</summary>
public enum ImageVariant
{
    Original = 0,
    Resized = 1,
    Thumbnail = 2,
    Watermark = 3
}
