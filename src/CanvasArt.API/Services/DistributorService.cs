using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Distributors;
using CanvasArt.API.Models.Entities;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.Interfaces;

namespace CanvasArt.API.Services;

public sealed class DistributorService : IDistributorService
{
    private readonly IDistributorRepository _distributors;
    private readonly IDateTimeProvider _clock;

    public DistributorService(IDistributorRepository distributors, IDateTimeProvider clock)
    {
        _distributors = distributors;
        _clock = clock;
    }

    // ----- Distributors -----

    public Task<PagedResult<DistributorDto>> QueryAsync(DistributorQuery query, CancellationToken cancellationToken = default) =>
        _distributors.QueryAsync(query, cancellationToken);

    public async Task<DistributorDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var distributor = await _distributors.GetByIdAsync(id, cancellationToken)
                          ?? throw new NotFoundException("Distributor", id);
        return ToDto(distributor);
    }

    public async Task<DistributorDto> CreateAsync(CreateDistributorRequest request, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var distributor = new Distributor
        {
            Name = request.Name.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };
        distributor.Id = await _distributors.CreateAsync(distributor, cancellationToken);
        return ToDto(distributor);
    }

    public async Task<DistributorDto> UpdateAsync(int id, UpdateDistributorRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _distributors.GetByIdAsync(id, cancellationToken)
                       ?? throw new NotFoundException("Distributor", id);

        existing.Name = request.Name.Trim();
        existing.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        existing.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        existing.IsActive = request.IsActive;
        existing.UpdatedAt = _clock.UtcNow;

        await _distributors.UpdateAsync(existing, cancellationToken);
        return ToDto(existing);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await _distributors.ExistsAsync(id, cancellationToken))
            throw new NotFoundException("Distributor", id);
        await _distributors.DeleteAsync(id, cancellationToken);
    }

    // ----- Promo codes -----

    public async Task<IReadOnlyList<PromoCodeDto>> GetPromoCodesAsync(int distributorId, CancellationToken cancellationToken = default)
    {
        if (!await _distributors.ExistsAsync(distributorId, cancellationToken))
            throw new NotFoundException("Distributor", distributorId);
        return await _distributors.GetPromoCodesByDistributorAsync(distributorId, cancellationToken);
    }

    public async Task<PromoCodeDto> CreatePromoCodeAsync(CreatePromoCodeRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _distributors.ExistsAsync(request.DistributorId, cancellationToken))
            throw new ValidationException($"Distributor {request.DistributorId} does not exist.");

        var code = NormalizeCode(request.Code);
        if (await _distributors.PromoCodeExistsAsync(code, null, cancellationToken))
            throw new ConflictException($"Promo code '{code}' is already in use.");

        var now = _clock.UtcNow;
        var promoCode = new PromoCode
        {
            DistributorId = request.DistributorId,
            Code = code,
            DiscountPercentage = request.DiscountPercentage,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };
        promoCode.Id = await _distributors.CreatePromoCodeAsync(promoCode, cancellationToken);
        return await GetPromoCodeDtoAsync(promoCode.Id, cancellationToken);
    }

    public async Task<PromoCodeDto> UpdatePromoCodeAsync(int id, UpdatePromoCodeRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _distributors.GetPromoCodeByIdAsync(id, cancellationToken)
                       ?? throw new NotFoundException("Promo code", id);

        var code = NormalizeCode(request.Code);
        if (await _distributors.PromoCodeExistsAsync(code, id, cancellationToken))
            throw new ConflictException($"Promo code '{code}' is already in use.");

        existing.Code = code;
        existing.DiscountPercentage = request.DiscountPercentage;
        existing.IsActive = request.IsActive;
        existing.UpdatedAt = _clock.UtcNow;

        await _distributors.UpdatePromoCodeAsync(existing, cancellationToken);
        return await GetPromoCodeDtoAsync(id, cancellationToken);
    }

    public async Task DeletePromoCodeAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _distributors.GetPromoCodeByIdAsync(id, cancellationToken) is null)
            throw new NotFoundException("Promo code", id);
        await _distributors.DeletePromoCodeAsync(id, cancellationToken);
    }

    // ----- Application / resolution -----

    public async Task<PromoCodeApplyResult> ApplyAsync(ApplyPromoCodeRequest request, CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveForOrderAsync(request.Code, cancellationToken)
                       ?? throw new ValidationException("A promo code is required.");
        return new PromoCodeApplyResult { Code = resolved.Code, DiscountPercentage = resolved.DiscountPercentage };
    }

    public async Task<ResolvedPromo?> ResolveForOrderAsync(string? code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var normalized = NormalizeCode(code);
        var promo = await _distributors.GetPromoCodeByCodeAsync(normalized, cancellationToken)
                    ?? throw new ValidationException($"Promo code '{normalized}' is not valid.");

        if (!promo.IsActive || !promo.DistributorIsActive)
            throw new ValidationException($"Promo code '{normalized}' is no longer active.");

        return new ResolvedPromo(promo.Id, promo.DistributorId, promo.DistributorName ?? string.Empty, promo.Code, promo.DiscountPercentage);
    }

    // ----- Dashboard -----

    public Task<IReadOnlyList<DistributorSalesRow>> GetDashboardAsync(DistributorDashboardQuery query, CancellationToken cancellationToken = default) =>
        _distributors.GetSalesDashboardAsync(query.FromDate, query.ToDate, cancellationToken);

    // ----- helpers -----

    private async Task<PromoCodeDto> GetPromoCodeDtoAsync(int id, CancellationToken ct)
    {
        var entity = await _distributors.GetPromoCodeByIdAsync(id, ct)
                     ?? throw new NotFoundException("Promo code", id);
        var codes = await _distributors.GetPromoCodesByDistributorAsync(entity.DistributorId, ct);
        return codes.First(c => c.Id == id);
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static DistributorDto ToDto(Distributor d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Email = d.Email,
        Phone = d.Phone,
        IsActive = d.IsActive,
        CreatedAt = d.CreatedAt
    };
}
