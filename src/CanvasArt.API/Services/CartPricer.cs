using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Cart;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.Interfaces;

namespace CanvasArt.API.Services;

/// <summary>A fully-resolved and priced cart line, shared by cart previews and order creation.</summary>
public sealed record PricedLine
{
    public required int PaintingId { get; init; }
    public required string PaintingCode { get; init; }
    public required string PaintingName { get; init; }
    public string? ThumbnailPath { get; init; }
    public required int PaintingSizeId { get; init; }
    public required string SizeLabel { get; init; }
    public required decimal PaintingBasePrice { get; init; }

    public int? FrameId { get; init; }
    public string? FrameName { get; init; }
    public decimal FrameBasePrice { get; init; }

    public required int Quantity { get; init; }

    public decimal UnitOriginal { get; init; }
    public decimal UnitDiscount { get; init; }
    public decimal UnitFinal { get; init; }

    public int? AppliedPromotionId { get; init; }
    public int? AppliedCombinationPromotionId { get; init; }
    public string? AppliedPromotionName { get; init; }

    public decimal LineSubTotal => Math.Round(UnitOriginal * Quantity, 2, MidpointRounding.AwayFromZero);
    public decimal LineDiscount => Math.Round(UnitDiscount * Quantity, 2, MidpointRounding.AwayFromZero);
    public decimal LineTotal => Math.Round(UnitFinal * Quantity, 2, MidpointRounding.AwayFromZero);
}

/// <summary>
/// Resolves cart line requests into fully-priced lines: validates the referenced painting,
/// size, and optional frame, enforces compatibility, and applies promotions. Frames have no
/// sizes of their own — every frame is compatible with every painting size.
/// </summary>
public sealed class CartPricer
{
    private readonly IPaintingRepository _paintings;
    private readonly IFrameRepository _frames;
    private readonly IPromotionEvaluator _pricing;

    public CartPricer(IPaintingRepository paintings, IFrameRepository frames, IPromotionEvaluator pricing)
    {
        _paintings = paintings;
        _frames = frames;
        _pricing = pricing;
    }

    public async Task<IReadOnlyList<PricedLine>> PriceAsync(
        IReadOnlyList<CartLineRequest> items, CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            throw new ValidationException("The cart is empty.");

        var result = new List<PricedLine>(items.Count);

        foreach (var line in items)
        {
            var painting = await _paintings.GetByIdAsync(line.PaintingId, cancellationToken)
                           ?? throw new ValidationException($"Painting {line.PaintingId} does not exist.");
            if (!painting.IsPublished)
                throw new ValidationException($"Painting '{painting.Name}' is not available.");

            var size = await _paintings.GetSizeAsync(line.PaintingSizeId, cancellationToken);
            if (size is null || size.PaintingId != painting.Id || !size.IsActive)
                throw new ValidationException($"Size {line.PaintingSizeId} is not valid for painting '{painting.Name}'.");

            var thumbnail = await _paintings.GetPrimaryThumbnailAsync(painting.Id, cancellationToken);

            // Optional frame. Frames have no sizes of their own, so any active, compatible frame applies.
            int? frameId = null;
            string? frameName = null;
            var frameBasePrice = 0m;

            if (line.FrameId is int fid)
            {
                var frame = await _frames.GetByIdAsync(fid, cancellationToken)
                            ?? throw new ValidationException($"Frame {fid} does not exist.");
                if (!frame.IsActive)
                    throw new ValidationException($"Frame '{frame.Name}' is not available.");
                if (!await _frames.IsCompatibleAsync(painting.Id, fid, cancellationToken))
                    throw new ValidationException($"Frame '{frame.Name}' is not compatible with painting '{painting.Name}'.");

                frameId = fid;
                frameName = frame.Name;
                frameBasePrice = frame.BasePrice;
            }

            PricedLine priced;
            if (frameId is null)
            {
                var b = await _pricing.ForPaintingAsync(size.Price, painting.Id, painting.CategoryId, cancellationToken);
                priced = new PricedLine
                {
                    PaintingId = painting.Id,
                    PaintingCode = painting.Code,
                    PaintingName = painting.Name,
                    ThumbnailPath = thumbnail,
                    PaintingSizeId = size.Id,
                    SizeLabel = size.Label,
                    PaintingBasePrice = size.Price,
                    Quantity = line.Quantity,
                    UnitOriginal = b.OriginalPrice,
                    UnitDiscount = b.DiscountAmount,
                    UnitFinal = b.FinalPrice,
                    AppliedPromotionId = b.PromotionId,
                    AppliedPromotionName = b.PromotionName
                };
            }
            else
            {
                var b = await _pricing.ForCombinationAsync(
                    size.Price, painting.Id, painting.CategoryId,
                    frameBasePrice, frameId.Value, cancellationToken);
                priced = new PricedLine
                {
                    PaintingId = painting.Id,
                    PaintingCode = painting.Code,
                    PaintingName = painting.Name,
                    ThumbnailPath = thumbnail,
                    PaintingSizeId = size.Id,
                    SizeLabel = size.Label,
                    PaintingBasePrice = size.Price,
                    FrameId = frameId,
                    FrameName = frameName,
                    FrameBasePrice = frameBasePrice,
                    Quantity = line.Quantity,
                    UnitOriginal = b.TotalOriginal,
                    UnitDiscount = b.DiscountAmount,
                    UnitFinal = b.FinalPrice,
                    AppliedPromotionId = b.AppliedPromotionId,
                    AppliedCombinationPromotionId = b.AppliedCombinationPromotionId,
                    AppliedPromotionName = b.PromotionName
                };
            }

            result.Add(priced);
        }

        return result;
    }

    public static CartResponse ToCartResponse(IReadOnlyList<PricedLine> lines)
    {
        var items = lines.Select(l => new CartLineResponse
        {
            PaintingId = l.PaintingId,
            PaintingName = l.PaintingName,
            ThumbnailPath = l.ThumbnailPath,
            PaintingSizeId = l.PaintingSizeId,
            SizeLabel = l.SizeLabel,
            FrameId = l.FrameId,
            FrameName = l.FrameName,
            PaintingUnitPrice = l.PaintingBasePrice,
            FrameUnitPrice = l.FrameBasePrice,
            UnitPrice = l.UnitOriginal,
            UnitDiscount = l.UnitDiscount,
            UnitFinalPrice = l.UnitFinal,
            Quantity = l.Quantity,
            LineSubTotal = l.LineSubTotal,
            LineDiscount = l.LineDiscount,
            LineTotal = l.LineTotal,
            AppliedPromotion = l.AppliedPromotionName
        }).ToList();

        return new CartResponse
        {
            Items = items,
            SubTotal = Math.Round(items.Sum(i => i.LineSubTotal), 2, MidpointRounding.AwayFromZero),
            DiscountTotal = Math.Round(items.Sum(i => i.LineDiscount), 2, MidpointRounding.AwayFromZero),
            GrandTotal = Math.Round(items.Sum(i => i.LineTotal), 2, MidpointRounding.AwayFromZero),
            TotalQuantity = items.Sum(i => i.Quantity)
        };
    }
}
