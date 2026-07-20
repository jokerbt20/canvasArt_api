using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Models;

/// <summary>Everything needed to render a painting detail page, fetched in one round-trip.</summary>
public sealed record PaintingAggregate(
    Painting Painting,
    IReadOnlyList<PaintingSize> Sizes,
    IReadOnlyList<PaintingImage> Images,
    IReadOnlyList<Tag> Tags,
    IReadOnlyList<Frame> CompatibleFrames);

/// <summary>An order plus its line items and status history.</summary>
public sealed record OrderAggregate(
    Order Order,
    IReadOnlyList<OrderItem> Items,
    IReadOnlyList<OrderStatusHistory> History);
