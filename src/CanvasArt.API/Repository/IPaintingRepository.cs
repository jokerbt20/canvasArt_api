using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Paintings;
using CanvasArt.API.Models;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Repository;

public interface IPaintingRepository
{
    /// <summary>Paginated listing. When <paramref name="publishedOnly"/> is true, drafts are hidden.</summary>
    Task<PagedResult<PaintingListItemDto>> QueryAsync(PaintingQuery query, bool publishedOnly, CancellationToken cancellationToken = default);

    Task<PaintingAggregate?> GetAggregateByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PaintingAggregate?> GetAggregateBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Painting?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(
        Painting painting,
        IReadOnlyList<PaintingSize> sizes,
        IReadOnlyList<int> tagIds,
        IReadOnlyList<int> compatibleFrameIds,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Painting painting,
        IReadOnlyList<PaintingSize> sizes,
        IReadOnlyList<int> tagIds,
        IReadOnlyList<int> compatibleFrameIds,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task IncrementViewCountAsync(int id, CancellationToken cancellationToken = default);

    // Sizes lookups used for pricing/orders.
    Task<PaintingSize?> GetSizeAsync(int paintingSizeId, CancellationToken cancellationToken = default);
    Task<string?> GetPrimaryThumbnailAsync(int paintingId, CancellationToken cancellationToken = default);

    // Images
    Task<int> AddImageAsync(PaintingImage image, bool makePrimaryIfFirst, CancellationToken cancellationToken = default);
    Task<PaintingImage?> GetImageAsync(int imageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaintingImage>> GetImagesAsync(int paintingId, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(int imageId, CancellationToken cancellationToken = default);
    Task SetPrimaryImageAsync(int paintingId, int imageId, CancellationToken cancellationToken = default);
}
