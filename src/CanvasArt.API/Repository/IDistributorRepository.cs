using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Distributors;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Repository;

public interface IDistributorRepository
{
    // Distributors
    Task<PagedResult<DistributorDto>> QueryAsync(DistributorQuery query, CancellationToken cancellationToken = default);
    Task<Distributor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Distributor distributor, CancellationToken cancellationToken = default);
    Task UpdateAsync(Distributor distributor, CancellationToken cancellationToken = default);
    /// <summary>Deletes the distributor and all of its promo codes in one transaction.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // Promo codes
    Task<IReadOnlyList<PromoCodeDto>> GetPromoCodesByDistributorAsync(int distributorId, CancellationToken cancellationToken = default);
    Task<PromoCode?> GetPromoCodeByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Resolves a code (case-insensitive) joined with its distributor, or null if it does not exist.</summary>
    Task<PromoCode?> GetPromoCodeByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> PromoCodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken = default);
    Task<int> CreatePromoCodeAsync(PromoCode promoCode, CancellationToken cancellationToken = default);
    Task UpdatePromoCodeAsync(PromoCode promoCode, CancellationToken cancellationToken = default);
    Task DeletePromoCodeAsync(int id, CancellationToken cancellationToken = default);

    // Dashboard
    Task<IReadOnlyList<DistributorSalesRow>> GetSalesDashboardAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
}
