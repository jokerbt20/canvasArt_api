using CanvasArt.API.Models.Enums;

namespace CanvasArt.API.Models.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal SubTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal GrandTotal { get; set; }

    // Promo code / distributor attribution. Nullable FKs are SET NULL on delete; the snapshot
    // columns preserve attribution even if the code or distributor is later edited or removed.
    public int? PromoCodeId { get; set; }
    public int? DistributorId { get; set; }
    public string? PromoCode { get; set; }
    public string? DistributorName { get; set; }
    /// <summary>Extra discount granted by the promo code, on top of automatic promotions.</summary>
    public decimal PromoDiscount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
