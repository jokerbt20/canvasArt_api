using CanvasArt.API.Models.Common;

namespace CanvasArt.API.Models.DTOs.Distributors;

// ----- Distributor -----

public record DistributorDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
    public int PromoCodeCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateDistributorRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateDistributorRequest : CreateDistributorRequest;

public class DistributorQuery : PagedQuery
{
    public bool? IsActive { get; set; }
}

// ----- Promo codes -----

public record PromoCodeDto
{
    public int Id { get; init; }
    public int DistributorId { get; init; }
    public string? DistributorName { get; init; }
    public string Code { get; init; } = string.Empty;
    public decimal DiscountPercentage { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreatePromoCodeRequest
{
    public int DistributorId { get; init; }
    public string Code { get; init; } = string.Empty;
    public decimal DiscountPercentage { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdatePromoCodeRequest
{
    public string Code { get; init; } = string.Empty;
    public decimal DiscountPercentage { get; init; }
    public bool IsActive { get; init; } = true;
}

// ----- Public code application (storefront preview) -----

public record ApplyPromoCodeRequest
{
    public string Code { get; init; } = string.Empty;
}

/// <summary>Storefront-safe result of validating a promo code. Never exposes the distributor.</summary>
public record PromoCodeApplyResult
{
    public string Code { get; init; } = string.Empty;
    public decimal DiscountPercentage { get; init; }
}

// ----- Dashboard -----

public class DistributorDashboardQuery
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public record DistributorSalesRow
{
    public int DistributorId { get; init; }
    public string DistributorName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int OrderCount { get; init; }
    /// <summary>Sum of order grand totals attributed to this distributor (excludes cancelled orders).</summary>
    public decimal TotalSales { get; init; }
    /// <summary>Sum of the extra promo-code discount this distributor's codes granted customers.</summary>
    public decimal TotalPromoDiscount { get; init; }
}
