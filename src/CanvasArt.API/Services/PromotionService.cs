using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Promotions;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Services;

public sealed class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _promotions;
    private readonly IPaintingRepository _paintings;
    private readonly IFrameRepository _frames;
    private readonly ICategoryRepository _categories;
    private readonly IDateTimeProvider _clock;

    public PromotionService(
        IPromotionRepository promotions,
        IPaintingRepository paintings,
        IFrameRepository frames,
        ICategoryRepository categories,
        IDateTimeProvider clock)
    {
        _promotions = promotions;
        _paintings = paintings;
        _frames = frames;
        _categories = categories;
        _clock = clock;
    }

    public Task<PagedResult<PromotionDto>> QueryAsync(PromotionQuery query, CancellationToken cancellationToken = default) =>
        _promotions.QueryAsync(query, _clock.UtcNow, cancellationToken);

    public async Task<PromotionDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var promo = await _promotions.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException("Promotion", id);
        return ToDto(promo);
    }

    public async Task<PromotionDto> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateTargetsAsync(request, cancellationToken);
        var now = _clock.UtcNow;
        var promo = FromRequest(request);
        promo.CreatedAt = now;
        promo.UpdatedAt = now;
        promo.Id = await _promotions.CreateAsync(promo, cancellationToken);
        return ToDto(promo);
    }

    public async Task<PromotionDto> UpdateAsync(int id, UpdatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _promotions.GetByIdAsync(id, cancellationToken)
                       ?? throw new NotFoundException("Promotion", id);
        await ValidateTargetsAsync(request, cancellationToken);

        existing.Name = request.Name.Trim();
        existing.Description = request.Description?.Trim();
        existing.PromotionType = request.PromotionType;
        existing.DiscountType = request.DiscountType;
        existing.DiscountValue = request.DiscountValue;
        existing.TargetPaintingId = request.TargetPaintingId;
        existing.TargetFrameId = request.TargetFrameId;
        existing.TargetCategoryId = request.TargetCategoryId;
        existing.StartDate = request.StartDate;
        existing.EndDate = request.EndDate;
        existing.IsActive = request.IsActive;
        existing.Priority = request.Priority;
        existing.UpdatedAt = _clock.UtcNow;

        await _promotions.UpdateAsync(existing, cancellationToken);
        return ToDto(existing);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _promotions.GetByIdAsync(id, cancellationToken) is null)
            throw new NotFoundException("Promotion", id);
        await _promotions.DeleteAsync(id, cancellationToken);
    }

    public Task<PagedResult<CombinationPromotionDto>> QueryCombinationsAsync(PromotionQuery query, CancellationToken cancellationToken = default) =>
        _promotions.QueryCombinationsAsync(query, _clock.UtcNow, cancellationToken);

    public async Task<CombinationPromotionDto> GetCombinationByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var promo = await _promotions.GetCombinationByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException("Combination promotion", id);
        return ToDto(promo);
    }

    public async Task<CombinationPromotionDto> CreateCombinationAsync(CreateCombinationPromotionRequest request, CancellationToken cancellationToken = default)
    {
        await ValidatePairingAsync(request.PaintingId, request.FrameId, cancellationToken);
        var now = _clock.UtcNow;
        var promo = new CombinationPromotion
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            PaintingId = request.PaintingId,
            FrameId = request.FrameId,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.IsActive,
            Priority = request.Priority,
            CreatedAt = now,
            UpdatedAt = now
        };
        promo.Id = await _promotions.CreateCombinationAsync(promo, cancellationToken);
        return ToDto(promo);
    }

    public async Task<CombinationPromotionDto> UpdateCombinationAsync(int id, UpdateCombinationPromotionRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _promotions.GetCombinationByIdAsync(id, cancellationToken)
                       ?? throw new NotFoundException("Combination promotion", id);
        await ValidatePairingAsync(request.PaintingId, request.FrameId, cancellationToken);

        existing.Name = request.Name.Trim();
        existing.Description = request.Description?.Trim();
        existing.PaintingId = request.PaintingId;
        existing.FrameId = request.FrameId;
        existing.DiscountType = request.DiscountType;
        existing.DiscountValue = request.DiscountValue;
        existing.StartDate = request.StartDate;
        existing.EndDate = request.EndDate;
        existing.IsActive = request.IsActive;
        existing.Priority = request.Priority;
        existing.UpdatedAt = _clock.UtcNow;

        await _promotions.UpdateCombinationAsync(existing, cancellationToken);
        return ToDto(existing);
    }

    public async Task DeleteCombinationAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _promotions.GetCombinationByIdAsync(id, cancellationToken) is null)
            throw new NotFoundException("Combination promotion", id);
        await _promotions.DeleteCombinationAsync(id, cancellationToken);
    }

    private async Task ValidateTargetsAsync(CreatePromotionRequest r, CancellationToken ct)
    {
        if (r.TargetPaintingId is int pid && !await _paintings.ExistsAsync(pid, ct))
            throw new ValidationException($"Target painting {pid} does not exist.");
        if (r.TargetFrameId is int fid && !await _frames.ExistsAsync(fid, ct))
            throw new ValidationException($"Target frame {fid} does not exist.");
        if (r.TargetCategoryId is int cid && !await _categories.ExistsAsync(cid, ct))
            throw new ValidationException($"Target category {cid} does not exist.");
    }

    private async Task ValidatePairingAsync(int paintingId, int frameId, CancellationToken ct)
    {
        if (!await _paintings.ExistsAsync(paintingId, ct))
            throw new ValidationException($"Painting {paintingId} does not exist.");
        if (!await _frames.ExistsAsync(frameId, ct))
            throw new ValidationException($"Frame {frameId} does not exist.");
    }

    private static Promotion FromRequest(CreatePromotionRequest r) => new()
    {
        Name = r.Name.Trim(),
        Description = r.Description?.Trim(),
        PromotionType = r.PromotionType,
        DiscountType = r.DiscountType,
        DiscountValue = r.DiscountValue,
        TargetPaintingId = r.TargetPaintingId,
        TargetFrameId = r.TargetFrameId,
        TargetCategoryId = r.TargetCategoryId,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        IsActive = r.IsActive,
        Priority = r.Priority
    };

    private PromotionDto ToDto(Promotion p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        PromotionType = p.PromotionType,
        DiscountType = p.DiscountType,
        DiscountValue = p.DiscountValue,
        TargetPaintingId = p.TargetPaintingId,
        TargetFrameId = p.TargetFrameId,
        TargetCategoryId = p.TargetCategoryId,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        IsActive = p.IsActive,
        IsCurrentlyActive = p.IsActive && p.StartDate <= _clock.UtcNow && p.EndDate >= _clock.UtcNow,
        Priority = p.Priority,
        CreatedAt = p.CreatedAt
    };

    private CombinationPromotionDto ToDto(CombinationPromotion p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        PaintingId = p.PaintingId,
        FrameId = p.FrameId,
        DiscountType = p.DiscountType,
        DiscountValue = p.DiscountValue,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        IsActive = p.IsActive,
        IsCurrentlyActive = p.IsActive && p.StartDate <= _clock.UtcNow && p.EndDate >= _clock.UtcNow,
        Priority = p.Priority,
        CreatedAt = p.CreatedAt
    };
}
