using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Promotions;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Repository;

public interface IPromotionRepository
{
    // Single-target promotions
    Task<PagedResult<PromotionDto>> QueryAsync(PromotionQuery query, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Promotion promotion, CancellationToken cancellationToken = default);
    Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>All single-target promotions that are active at <paramref name="nowUtc"/>.</summary>
    Task<IReadOnlyList<Promotion>> GetActiveAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    // Combination promotions
    Task<PagedResult<CombinationPromotionDto>> QueryCombinationsAsync(PromotionQuery query, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<CombinationPromotion?> GetCombinationByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateCombinationAsync(CombinationPromotion promotion, CancellationToken cancellationToken = default);
    Task UpdateCombinationAsync(CombinationPromotion promotion, CancellationToken cancellationToken = default);
    Task DeleteCombinationAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CombinationPromotion>> GetActiveCombinationsAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
