using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Frames;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Models;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Services;

public sealed class FrameService : IFrameService
{
    private readonly IFrameRepository _frames;
    private readonly IPromotionEvaluator _pricing;
    private readonly IImageService _images;
    private readonly IDateTimeProvider _clock;

    public FrameService(IFrameRepository frames, IPromotionEvaluator pricing, IImageService images, IDateTimeProvider clock)
    {
        _frames = frames;
        _pricing = pricing;
        _images = images;
        _clock = clock;
    }

    public async Task<PagedResult<FrameListItemDto>> QueryAsync(FrameQuery query, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var page = await _frames.QueryAsync(query, activeOnly, cancellationToken);
        var priced = new List<FrameListItemDto>(page.Items.Count);
        foreach (var item in page.Items)
        {
            var b = await _pricing.ForFrameAsync(item.BasePrice, item.Id, cancellationToken);
            priced.Add(item with { FinalPrice = b.FinalPrice, ThumbnailPath = _images.BuildFrameUrl(item.ThumbnailPath) });
        }
        return new PagedResult<FrameListItemDto>(priced, page.TotalCount, page.Page, page.PageSize);
    }

    public async Task<FrameDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var frame = await _frames.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException("Frame", id);
        return await BuildDetailAsync(frame, cancellationToken);
    }

    public async Task<FrameDetailDto> CreateAsync(CreateFrameRequest request, CancellationToken cancellationToken = default)
    {
        var code = await ResolveCodeAsync(request.Code, cancellationToken);
        var now = _clock.UtcNow;
        var frame = new Frame
        {
            Code = code,
            Name = request.Name.Trim(),
            Material = request.Material.Trim(),
            Color = request.Color.Trim(),
            Description = request.Description?.Trim(),
            BasePrice = request.BasePrice,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };
        frame.Id = await _frames.CreateAsync(frame, cancellationToken);
        return await GetByIdAsync(frame.Id, cancellationToken);
    }

    public async Task<FrameDetailDto> UpdateAsync(int id, UpdateFrameRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _frames.GetByIdAsync(id, cancellationToken)
                       ?? throw new NotFoundException("Frame", id);

        existing.Name = request.Name.Trim();
        existing.Material = request.Material.Trim();
        existing.Color = request.Color.Trim();
        existing.Description = request.Description?.Trim();
        existing.BasePrice = request.BasePrice;
        existing.IsActive = request.IsActive;
        existing.UpdatedAt = _clock.UtcNow;

        await _frames.UpdateAsync(existing, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var frame = await _frames.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException("Frame", id);
        await _frames.DeleteAsync(id, cancellationToken);
        _images.DeleteFiles(frame.ImagePath, frame.ThumbnailPath);
    }

    public async Task<FrameDetailDto> UploadImageAsync(int frameId, Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var frame = await _frames.GetByIdAsync(frameId, cancellationToken)
                    ?? throw new NotFoundException("Frame", frameId);

        // Saved with alpha preserved (PNG) rather than re-encoded as JPEG: this is the same
        // image FrameCompositor uses to build room previews, and it needs a transparent
        // background to detect the moulding via its alpha channel.
        var set = await _images.ProcessSimpleImageAsync(content, fileName, _images.FramesFolder, _images.FramesFolder, cancellationToken, preserveAlpha: true);

        // Remove previous files, then persist the new paths.
        _images.DeleteFiles(frame.ImagePath, frame.ThumbnailPath);
        await _frames.UpdateImagesAsync(frameId, set.ImagePath, set.ThumbnailPath, cancellationToken);

        return await GetByIdAsync(frameId, cancellationToken);
    }

    private async Task<FrameDetailDto> BuildDetailAsync(Frame f, CancellationToken ct)
    {
        var b = await _pricing.ForFrameAsync(f.BasePrice, f.Id, ct);

        return new FrameDetailDto
        {
            Id = f.Id,
            Code = f.Code,
            Name = f.Name,
            Material = f.Material,
            Color = f.Color,
            Description = f.Description,
            ImagePath = _images.BuildFrameUrl(f.ImagePath),
            ThumbnailPath = _images.BuildFrameUrl(f.ThumbnailPath),
            BasePrice = f.BasePrice,
            FinalPrice = b.FinalPrice,
            DiscountAmount = b.DiscountAmount,
            IsActive = f.IsActive,
            CreatedAt = f.CreatedAt
        };
    }

    private async Task<string> ResolveCodeAsync(string? requested, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            var code = requested.Trim().ToUpperInvariant();
            if (await _frames.CodeExistsAsync(code, null, ct))
                throw new ConflictException($"Frame code '{code}' is already in use.");
            return code;
        }

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = $"FRM-{DateTime.UtcNow:yyMM}-{Random.Shared.Next(0, 1_000_000):D6}";
            if (!await _frames.CodeExistsAsync(code, null, ct))
                return code;
        }
        throw new ConflictException("Unable to generate a unique frame code.");
    }
}
