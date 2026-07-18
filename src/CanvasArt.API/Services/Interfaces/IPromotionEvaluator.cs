namespace CanvasArt.API.Services.Interfaces;

/// <summary>The outcome of applying the best applicable promotion to a base price.</summary>
public sealed record PriceBreakdown(
    decimal OriginalPrice,
    decimal DiscountAmount,
    decimal FinalPrice,
    int? PromotionId,
    string? PromotionName)
{
    public static PriceBreakdown None(decimal price) => new(price, 0m, price, null, null);
}

/// <summary>
/// Scoped service that loads active promotions once per request and computes discounted
/// prices for paintings, frames and painting+frame combinations. Keeps promotion logic in
/// one place so listings, cart and orders all price identically.
/// </summary>
public interface IPromotionEvaluator
{
    Task<PriceBreakdown> ForPaintingAsync(decimal basePrice, int paintingId, int categoryId, CancellationToken cancellationToken = default);
    Task<PriceBreakdown> ForFrameAsync(decimal basePrice, int frameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prices a painting+frame line. Applies per-item painting/frame promotions first, then any
    /// combination promotion for that exact pairing if it beats the sum of individual discounts.
    /// </summary>
    Task<CombinationBreakdown> ForCombinationAsync(
        decimal paintingBasePrice, int paintingId, int categoryId,
        decimal frameBasePrice, int frameId,
        CancellationToken cancellationToken = default);
}

public sealed record CombinationBreakdown(
    decimal PaintingOriginal,
    decimal FrameOriginal,
    decimal TotalOriginal,
    decimal DiscountAmount,
    decimal FinalPrice,
    int? AppliedPromotionId,
    int? AppliedCombinationPromotionId,
    string? PromotionName);
