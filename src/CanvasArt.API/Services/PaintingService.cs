using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Paintings;
using CanvasArt.API.Models.DTOs.Tags;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Models;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Services;

public sealed class PaintingService : IPaintingService
{
    private readonly IPaintingRepository _paintings;
    private readonly ICategoryRepository _categories;
    private readonly IFrameRepository _frames;
    private readonly ITagRepository _tags;
    private readonly IPromotionEvaluator _pricing;
    private readonly IImageService _images;
    private readonly IDateTimeProvider _clock;

    public PaintingService(
        IPaintingRepository paintings,
        ICategoryRepository categories,
        IFrameRepository frames,
        ITagRepository tags,
        IPromotionEvaluator pricing,
        IImageService images,
        IDateTimeProvider clock)
    {
        _paintings = paintings;
        _categories = categories;
        _frames = frames;
        _tags = tags;
        _pricing = pricing;
        _images = images;
        _clock = clock;
    }

    public async Task<PagedResult<PaintingListItemDto>> QueryAsync(PaintingQuery query, bool publishedOnly, CancellationToken cancellationToken = default)
    {
        var page = await _paintings.QueryAsync(query, publishedOnly, cancellationToken);

        var priced = new List<PaintingListItemDto>(page.Items.Count);
        foreach (var item in page.Items)
        {
            var breakdown = await _pricing.ForPaintingAsync(item.FromPrice, item.Id, item.CategoryId, cancellationToken);
            priced.Add(item with { FromFinalPrice = breakdown.FinalPrice, ThumbnailPath = _images.BuildThumbUrl(item.ThumbnailPath) });
        }

        return new PagedResult<PaintingListItemDto>(priced, page.TotalCount, page.Page, page.PageSize);
    }

    public async Task<PaintingDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var aggregate = await _paintings.GetAggregateByIdAsync(id, cancellationToken)
                        ?? throw new NotFoundException("Painting", id);
        return await BuildDetailAsync(aggregate, cancellationToken);
    }

    public async Task<PaintingDetailDto> GetBySlugAsync(string slug, bool incrementView, CancellationToken cancellationToken = default)
    {
        var aggregate = await _paintings.GetAggregateBySlugAsync(slug, cancellationToken)
                        ?? throw new NotFoundException($"Painting '{slug}' was not found.");
        if (incrementView)
            await _paintings.IncrementViewCountAsync(aggregate.Painting.Id, cancellationToken);
        return await BuildDetailAsync(aggregate, cancellationToken);
    }

    public async Task<PaintingDetailDto> CreateAsync(CreatePaintingRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _categories.ExistsAsync(request.CategoryId, cancellationToken))
            throw new ValidationException($"Category {request.CategoryId} does not exist.");

        var frameIds = await ValidateFramesAsync(request.CompatibleFrameIds, cancellationToken);
        var tagIds = await ValidateTagsAsync(request.TagIds, cancellationToken);

        var code = await ResolveCodeAsync(request.Code, cancellationToken);
        var slug = await EnsureUniqueSlugAsync(request.Slug, request.Name, null, cancellationToken);

        var now = _clock.UtcNow;
        var painting = new Painting
        {
            Code = code,
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description?.Trim(),
            Context = request.Context?.Trim(),
            Colors = SerializeColors(request.Colors),
            CategoryId = request.CategoryId,
            IsPublished = request.IsPublished,
            IsFeatured = request.IsFeatured,
            CreatedAt = now,
            UpdatedAt = now
        };

        var sizes = MapSizes(request.Sizes);
        painting.Id = await _paintings.CreateAsync(painting, sizes, tagIds, frameIds, cancellationToken);

        return await GetByIdAsync(painting.Id, cancellationToken);
    }

    public async Task<PaintingDetailDto> UpdateAsync(int id, UpdatePaintingRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _paintings.GetByIdAsync(id, cancellationToken)
                       ?? throw new NotFoundException("Painting", id);

        if (!await _categories.ExistsAsync(request.CategoryId, cancellationToken))
            throw new ValidationException($"Category {request.CategoryId} does not exist.");

        var frameIds = await ValidateFramesAsync(request.CompatibleFrameIds, cancellationToken);
        var tagIds = await ValidateTagsAsync(request.TagIds, cancellationToken);

        existing.Name = request.Name.Trim();
        existing.Slug = await EnsureUniqueSlugAsync(request.Slug, request.Name, id, cancellationToken);
        existing.Description = request.Description?.Trim();
        existing.Context = request.Context?.Trim();
        existing.Colors = SerializeColors(request.Colors);
        existing.CategoryId = request.CategoryId;
        existing.IsPublished = request.IsPublished;
        existing.IsFeatured = request.IsFeatured;
        existing.UpdatedAt = _clock.UtcNow;

        var sizes = MapSizes(request.Sizes);
        await _paintings.UpdateAsync(existing, sizes, tagIds, frameIds, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var images = await _paintings.GetImagesAsync(id, cancellationToken);
        if (!await _paintings.ExistsAsync(id, cancellationToken))
            throw new NotFoundException("Painting", id);

        await _paintings.DeleteAsync(id, cancellationToken);

        foreach (var img in images)
            _images.DeleteFiles(img.OriginalPath, img.ResizedPath, img.ThumbnailPath, img.WatermarkPath);
    }

    public async Task<PaintingImageDto> UploadImageAsync(int paintingId, Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        if (!await _paintings.ExistsAsync(paintingId, cancellationToken))
            throw new NotFoundException("Painting", paintingId);

        var set = await _images.ProcessPaintingImageAsync(content, fileName, $"paintings/{paintingId}", cancellationToken);

        var image = new PaintingImage
        {
            PaintingId = paintingId,
            OriginalPath = set.OriginalPath,
            ResizedPath = set.ResizedPath,
            ThumbnailPath = set.ThumbnailPath,
            WatermarkPath = set.WatermarkPath,
            FileName = set.FileName,
            ContentType = set.ContentType,
            FileSizeBytes = set.FileSizeBytes,
            Width = set.Width,
            Height = set.Height,
            CreatedAt = _clock.UtcNow
        };
        image.Id = await _paintings.AddImageAsync(image, makePrimaryIfFirst: true, cancellationToken);

        return new PaintingImageDto
        {
            Id = image.Id,
            ThumbnailPath = _images.BuildThumbUrl(image.ThumbnailPath)!,
            WatermarkPath = _images.BuildImageUrl(image.WatermarkPath)!,
            ResizedPath = _images.BuildImageUrl(image.ResizedPath)!,
            IsPrimary = image.IsPrimary,
            DisplayOrder = image.DisplayOrder,
            Width = image.Width,
            Height = image.Height
        };
    }

    public async Task DeleteImageAsync(int paintingId, int imageId, CancellationToken cancellationToken = default)
    {
        var image = await _paintings.GetImageAsync(imageId, cancellationToken);
        if (image is null || image.PaintingId != paintingId)
            throw new NotFoundException("Image", imageId);

        await _paintings.DeleteImageAsync(imageId, cancellationToken);
        _images.DeleteFiles(image.OriginalPath, image.ResizedPath, image.ThumbnailPath, image.WatermarkPath);
    }

    public async Task SetPrimaryImageAsync(int paintingId, int imageId, CancellationToken cancellationToken = default)
    {
        var image = await _paintings.GetImageAsync(imageId, cancellationToken);
        if (image is null || image.PaintingId != paintingId)
            throw new NotFoundException("Image", imageId);
        await _paintings.SetPrimaryImageAsync(paintingId, imageId, cancellationToken);
    }

    // ----- helpers -----

    private async Task<PaintingDetailDto> BuildDetailAsync(PaintingAggregate a, CancellationToken ct)
    {
        var p = a.Painting;

        var sizes = new List<PaintingSizeDto>(a.Sizes.Count);
        foreach (var s in a.Sizes)
        {
            var b = await _pricing.ForPaintingAsync(s.Price, p.Id, p.CategoryId, ct);
            sizes.Add(new PaintingSizeDto
            {
                Id = s.Id,
                Label = s.Label,
                WidthCm = s.WidthCm,
                HeightCm = s.HeightCm,
                Price = s.Price,
                FinalPrice = b.FinalPrice,
                DiscountAmount = b.DiscountAmount,
                Sku = s.Sku,
                IsDefault = s.IsDefault,
                DisplayOrder = s.DisplayOrder,
                IsActive = s.IsActive
            });
        }

        var frames = new List<CompatibleFrameDto>(a.CompatibleFrames.Count);
        foreach (var f in a.CompatibleFrames)
        {
            var b = await _pricing.ForFrameAsync(f.BasePrice, f.Id, ct);
            frames.Add(new CompatibleFrameDto
            {
                Id = f.Id,
                Name = f.Name,
                Material = f.Material,
                Color = f.Color,
                ThumbnailPath = _images.BuildFrameUrl(f.ThumbnailPath),
                BasePrice = f.BasePrice,
                FinalPrice = b.FinalPrice
            });
        }

        return new PaintingDetailDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Slug = p.Slug,
            Description = p.Description,
            Context = p.Context,
            Colors = DeserializeColors(p.Colors),
            CategoryId = p.CategoryId,
            CategoryName = p.CategoryName,
            CategorySlug = p.CategorySlug,
            IsPublished = p.IsPublished,
            IsFeatured = p.IsFeatured,
            ViewCount = p.ViewCount,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Sizes = sizes,
            Images = a.Images.Select(i => new PaintingImageDto
            {
                Id = i.Id,
                ThumbnailPath = _images.BuildThumbUrl(i.ThumbnailPath)!,
                WatermarkPath = _images.BuildImageUrl(i.WatermarkPath)!,
                ResizedPath = _images.BuildImageUrl(i.ResizedPath)!,
                IsPrimary = i.IsPrimary,
                DisplayOrder = i.DisplayOrder,
                Width = i.Width,
                Height = i.Height
            }).ToList(),
            Tags = a.Tags.Select(t => new TagDto { Id = t.Id, Name = t.Name, Slug = t.Slug }).ToList(),
            CompatibleFrames = frames
        };
    }

    private async Task<IReadOnlyList<int>> ValidateFramesAsync(IReadOnlyList<int>? ids, CancellationToken ct)
    {
        if (ids is null || ids.Count == 0)
            return Array.Empty<int>();
        var distinct = ids.Distinct().ToList();
        if (!await _frames.AllExistAsync(distinct, ct))
            throw new ValidationException("One or more compatible frames do not exist.");
        return distinct;
    }

    private async Task<IReadOnlyList<int>> ValidateTagsAsync(IReadOnlyList<int>? ids, CancellationToken ct)
    {
        if (ids is null || ids.Count == 0)
            return Array.Empty<int>();
        var distinct = ids.Distinct().ToList();
        foreach (var tagId in distinct)
        {
            if (!await _tags.ExistsAsync(tagId, ct))
                throw new ValidationException($"Tag {tagId} does not exist.");
        }
        return distinct;
    }

    private async Task<string> ResolveCodeAsync(string? requested, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            var code = requested.Trim().ToUpperInvariant();
            if (await _paintings.CodeExistsAsync(code, null, ct))
                throw new ConflictException($"Painting code '{code}' is already in use.");
            return code;
        }

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = $"PTG-{DateTime.UtcNow:yyMM}-{Random.Shared.Next(0, 1_000_000):D6}";
            if (!await _paintings.CodeExistsAsync(code, null, ct))
                return code;
        }
        throw new ConflictException("Unable to generate a unique painting code.");
    }

    private async Task<string> EnsureUniqueSlugAsync(string? requested, string name, int? excludeId, CancellationToken ct)
    {
        var slug = SlugHelper.Generate(string.IsNullOrWhiteSpace(requested) ? name : requested!);
        if (string.IsNullOrEmpty(slug))
            throw new ValidationException("Unable to derive a slug from the supplied name.");
        if (await _paintings.SlugExistsAsync(slug, excludeId, ct))
            throw new ConflictException($"A painting with slug '{slug}' already exists.");
        return slug;
    }

    private static List<PaintingSize> MapSizes(IReadOnlyList<PaintingSizeInput> inputs) =>
        inputs.Select(s => new PaintingSize
        {
            Id = s.Id ?? 0,
            Label = s.Label.Trim(),
            WidthCm = s.WidthCm,
            HeightCm = s.HeightCm,
            Price = s.Price,
            Sku = s.Sku?.Trim(),
            IsDefault = s.IsDefault,
            DisplayOrder = s.DisplayOrder,
            IsActive = s.IsActive
        }).ToList();

    private static string? SerializeColors(IReadOnlyList<string>? colors)
    {
        if (colors is null || colors.Count == 0)
            return null;
        var cleaned = colors.Select(c => c.Trim()).Where(c => c.Length > 0);
        return string.Join(',', cleaned);
    }

    private static IReadOnlyList<string> DeserializeColors(string? colors) =>
        string.IsNullOrWhiteSpace(colors)
            ? Array.Empty<string>()
            : colors.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
