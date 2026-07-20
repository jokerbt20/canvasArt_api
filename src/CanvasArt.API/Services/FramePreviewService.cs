using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.FramePreviews;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.FrameCompositing;
using CanvasArt.API.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CanvasArt.API.Services;

public sealed class FramePreviewService : IFramePreviewService
{
    // Mirrors ImageSettings.ResizedMaxDimension — this is a browser-canvas preview, not a print
    // asset, so private originals (which can be arbitrarily large) are capped before compositing.
    private const int MaxWorkingDimension = 1600;

    private readonly IPaintingRepository _paintings;
    private readonly IFrameRepository _frames;
    private readonly IImageService _images;

    public FramePreviewService(IPaintingRepository paintings, IFrameRepository frames, IImageService images)
    {
        _paintings = paintings;
        _frames = frames;
        _images = images;
    }

    public async Task<FramePreviewDto> GetOrBuildAsync(int paintingId, int frameId, int? paintingImageId, CancellationToken cancellationToken = default)
    {
        var painting = await _paintings.GetByIdAsync(paintingId, cancellationToken);
        if (painting is null || !painting.IsPublished)
            throw new NotFoundException("Painting", paintingId);

        var frame = await _frames.GetByIdAsync(frameId, cancellationToken);
        if (frame is null || !frame.IsActive)
            throw new NotFoundException("Frame", frameId);
        if (string.IsNullOrEmpty(frame.ImagePath))
            throw new ValidationException("This frame doesn't support room previews yet.");

        if (!await _frames.IsCompatibleAsync(paintingId, frameId, cancellationToken))
            throw new ValidationException("This frame is not available for this painting.");

        var image = await ResolvePaintingImageAsync(paintingId, paintingImageId, cancellationToken);

        var fileName = $"pv-{image.Id}-{frame.Id}-{frame.UpdatedAt.Ticks}.png";

        if (_images.PublicFileExists(_images.FramePreviewsFolder, fileName))
        {
            var cachedPath = _images.ResolvePhysicalPath(fileName)
                              ?? throw new InvalidOperationException("Cached frame preview file could not be resolved.");
            var info = await Image.IdentifyAsync(cachedPath, cancellationToken);
            return new FramePreviewDto
            {
                PreviewUrl = _images.BuildFramePreviewUrl(fileName)!,
                Width = info.Width,
                Height = info.Height
            };
        }

        var paintingPhysicalPath = _images.ResolvePhysicalPath(image.OriginalPath)
                                    ?? throw new NotFoundException("Painting image file", image.Id);
        var framePhysicalPath = _images.ResolvePhysicalPath(frame.ImagePath)
                                 ?? throw new NotFoundException("Frame image file", frame.Id);

        using var paintingImage = await Image.LoadAsync<Rgba32>(paintingPhysicalPath, cancellationToken);
        using var cornerImage = await Image.LoadAsync<Rgba32>(framePhysicalPath, cancellationToken);

        if (paintingImage.Width > MaxWorkingDimension || paintingImage.Height > MaxWorkingDimension)
        {
            paintingImage.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(MaxWorkingDimension, MaxWorkingDimension),
                Sampler = KnownResamplers.Lanczos3
            }));
        }

        using var framed = FrameCompositor.BuildFrame(paintingImage, cornerImage);
        await _images.SaveGeneratedPngAsync(framed, _images.FramePreviewsFolder, fileName, cancellationToken);

        return new FramePreviewDto
        {
            PreviewUrl = _images.BuildFramePreviewUrl(fileName)!,
            Width = framed.Width,
            Height = framed.Height
        };
    }

    private async Task<Models.Entities.PaintingImage> ResolvePaintingImageAsync(int paintingId, int? paintingImageId, CancellationToken ct)
    {
        if (paintingImageId is int id)
        {
            var image = await _paintings.GetImageAsync(id, ct);
            if (image is null || image.PaintingId != paintingId)
                throw new NotFoundException("Painting image", id);
            return image;
        }

        var images = await _paintings.GetImagesAsync(paintingId, ct);
        var primary = images.FirstOrDefault(i => i.IsPrimary) ?? images.FirstOrDefault();
        return primary ?? throw new NotFoundException($"Painting {paintingId} has no images.");
    }
}
