using CanvasArt.API.Models.Enums;

namespace CanvasArt.API.Models.Common;

public static class DiscountMath
{
    /// <summary>
    /// Computes the discount amount for a base price, never exceeding the base price itself.
    /// Percentage discounts are applied proportionally; fixed discounts are capped at the base.
    /// </summary>
    public static decimal ComputeDiscount(decimal basePrice, DiscountType type, decimal value)
    {
        if (basePrice <= 0m || value <= 0m)
            return 0m;

        var discount = type switch
        {
            DiscountType.Percentage => basePrice * (value / 100m),
            DiscountType.FixedAmount => value,
            _ => 0m
        };

        discount = Math.Round(discount, 2, MidpointRounding.AwayFromZero);
        return discount > basePrice ? basePrice : discount;
    }
}
