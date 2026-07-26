namespace CanvasArt.API.Models.Entities;

/// <summary>
/// A distributor-owned promo code. When a customer enters it before checkout, the order receives
/// an extra <see cref="DiscountPercentage"/> off the already-discounted goods total, and the sale
/// is attributed to the owning distributor.
/// </summary>
public class PromoCode
{
    public int Id { get; set; }
    public int DistributorId { get; set; }
    public string Code { get; set; } = string.Empty;
    /// <summary>Percentage (0–100) taken off the discounted goods total.</summary>
    public decimal DiscountPercentage { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Read-only projections populated by joins.
    public string? DistributorName { get; set; }
    public bool DistributorIsActive { get; set; }
}
