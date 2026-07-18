using CanvasArt.API.Models.Common;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Models.Entities;
using CanvasArt.API.Models.Enums;

namespace CanvasArt.API.Services;

/// <summary>
/// Scoped promotion engine. Loads the currently-active single and combination promotions on
/// first use and reuses them for the rest of the request so a listing/cart prices consistently.
/// </summary>
public sealed class PromotionEvaluator : IPromotionEvaluator
{
    private readonly IPromotionRepository _promotions;
    private readonly IDateTimeProvider _clock;

    private IReadOnlyList<Promotion>? _activeSingle;
    private IReadOnlyList<CombinationPromotion>? _activeCombination;

    public PromotionEvaluator(IPromotionRepository promotions, IDateTimeProvider clock)
    {
        _promotions = promotions;
        _clock = clock;
    }

    public async Task<PriceBreakdown> ForPaintingAsync(decimal basePrice, int paintingId, int categoryId, CancellationToken cancellationToken = default)
    {
        var promos = await GetSingleAsync(cancellationToken);
        var best = BestFor(promos, basePrice, p =>
            p.PromotionType == PromotionType.Painting &&
            (p.TargetPaintingId == paintingId || (p.TargetCategoryId != null && p.TargetCategoryId == categoryId)));

        return ToBreakdown(basePrice, best);
    }

    public async Task<PriceBreakdown> ForFrameAsync(decimal basePrice, int frameId, CancellationToken cancellationToken = default)
    {
        var promos = await GetSingleAsync(cancellationToken);
        var best = BestFor(promos, basePrice, p =>
            p.PromotionType == PromotionType.Frame && p.TargetFrameId == frameId);

        return ToBreakdown(basePrice, best);
    }

    public async Task<CombinationBreakdown> ForCombinationAsync(
        decimal paintingBasePrice, int paintingId, int categoryId,
        decimal frameBasePrice, int frameId,
        CancellationToken cancellationToken = default)
    {
        var painting = await ForPaintingAsync(paintingBasePrice, paintingId, categoryId, cancellationToken);
        var frame = await ForFrameAsync(frameBasePrice, frameId, cancellationToken);

        var totalOriginal = paintingBasePrice + frameBasePrice;
        var individualDiscount = painting.DiscountAmount + frame.DiscountAmount;

        // Does a bundle promotion for this exact pairing beat the individual discounts?
        var combos = await GetCombinationAsync(cancellationToken);
        CombinationPromotion? bestCombo = null;
        var bestComboDiscount = 0m;
        foreach (var c in combos.Where(c => c.PaintingId == paintingId && c.FrameId == frameId))
        {
            var d = DiscountMath.ComputeDiscount(totalOriginal, c.DiscountType, c.DiscountValue);
            if (d > bestComboDiscount || (d == bestComboDiscount && bestCombo != null && c.Priority > bestCombo.Priority))
            {
                bestComboDiscount = d;
                bestCombo = c;
            }
        }

        if (bestCombo != null && bestComboDiscount >= individualDiscount)
        {
            return new CombinationBreakdown(
                paintingBasePrice, frameBasePrice, totalOriginal,
                bestComboDiscount, totalOriginal - bestComboDiscount,
                AppliedPromotionId: null,
                AppliedCombinationPromotionId: bestCombo.Id,
                PromotionName: bestCombo.Name);
        }

        return new CombinationBreakdown(
            paintingBasePrice, frameBasePrice, totalOriginal,
            individualDiscount, totalOriginal - individualDiscount,
            AppliedPromotionId: painting.PromotionId,
            AppliedCombinationPromotionId: null,
            PromotionName: painting.PromotionName ?? frame.PromotionName);
    }

    private static Promotion? BestFor(IEnumerable<Promotion> promos, decimal basePrice, Func<Promotion, bool> predicate)
    {
        Promotion? best = null;
        var bestDiscount = 0m;
        foreach (var p in promos.Where(predicate))
        {
            var d = DiscountMath.ComputeDiscount(basePrice, p.DiscountType, p.DiscountValue);
            if (d > bestDiscount || (d == bestDiscount && best != null && p.Priority > best.Priority))
            {
                bestDiscount = d;
                best = p;
            }
        }
        return best;
    }

    private static PriceBreakdown ToBreakdown(decimal basePrice, Promotion? promo)
    {
        if (promo is null)
            return PriceBreakdown.None(basePrice);

        var discount = DiscountMath.ComputeDiscount(basePrice, promo.DiscountType, promo.DiscountValue);
        return new PriceBreakdown(basePrice, discount, basePrice - discount, promo.Id, promo.Name);
    }

    private async Task<IReadOnlyList<Promotion>> GetSingleAsync(CancellationToken ct) =>
        _activeSingle ??= await _promotions.GetActiveAsync(_clock.UtcNow, ct);

    private async Task<IReadOnlyList<CombinationPromotion>> GetCombinationAsync(CancellationToken ct) =>
        _activeCombination ??= await _promotions.GetActiveCombinationsAsync(_clock.UtcNow, ct);
}
