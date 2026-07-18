using AutoMapper;
using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Cms;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Services;

public sealed class CmsService : ICmsService
{
    private readonly ISlideRepository _slides;
    private readonly ISettingRepository _settings;
    private readonly ITestimonialRepository _testimonials;
    private readonly IImageService _images;
    private readonly IPaintingRepository _paintings;
    private readonly IMapper _mapper;
    private readonly IDateTimeProvider _clock;

    public CmsService(
        ISlideRepository slides,
        ISettingRepository settings,
        ITestimonialRepository testimonials,
        IImageService images,
        IPaintingRepository paintings,
        IMapper mapper,
        IDateTimeProvider clock)
    {
        _slides = slides;
        _settings = settings;
        _testimonials = testimonials;
        _images = images;
        _paintings = paintings;
        _mapper = mapper;
        _clock = clock;
    }

    public async Task<IReadOnlyList<SlideDto>> GetSlidesAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var slides = await _slides.GetAllAsync(activeOnly, _clock.UtcNow, cancellationToken);
        return slides.Select(MapSlide).ToList();
    }

    public async Task<SlideDto> GetSlideAsync(int id, CancellationToken cancellationToken = default)
    {
        var slide = await _slides.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException("Slide", id);
        return MapSlide(slide);
    }

    public async Task<SlideDto> CreateSlideAsync(CreateSlideRequest request, Stream imageContent, string fileName, CancellationToken cancellationToken = default)
    {
        var image = await _images.ProcessSimpleImageAsync(imageContent, fileName, _images.ImagesFolder, _images.ThumbsFolder, cancellationToken);
        var now = _clock.UtcNow;
        var slide = new Slide
        {
            Title = request.Title.Trim(),
            Subtitle = request.Subtitle?.Trim(),
            ImagePath = image.ImagePath,
            LinkUrl = request.LinkUrl?.Trim(),
            ButtonText = request.ButtonText?.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = now,
            UpdatedAt = now
        };
        slide.Id = await _slides.CreateAsync(slide, cancellationToken);
        return MapSlide(slide);
    }

    public async Task<SlideDto> CreateSlideFromPaintingImageAsync(CreateSlideFromPaintingImageRequest request, CancellationToken cancellationToken = default)
    {
        var image = await CopyPaintingImageAsync(request.PaintingImageId, cancellationToken);

        var now = _clock.UtcNow;
        var slide = new Slide
        {
            Title = request.Title.Trim(),
            Subtitle = request.Subtitle?.Trim(),
            ImagePath = image.ImagePath,
            LinkUrl = request.LinkUrl?.Trim(),
            ButtonText = request.ButtonText?.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = now,
            UpdatedAt = now
        };
        slide.Id = await _slides.CreateAsync(slide, cancellationToken);
        return MapSlide(slide);
    }

    public async Task<SlideDto> UpdateSlideFromPaintingImageAsync(int id, CreateSlideFromPaintingImageRequest request, CancellationToken cancellationToken = default)
    {
        var slide = await _slides.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException("Slide", id);

        var image = await CopyPaintingImageAsync(request.PaintingImageId, cancellationToken);

        slide.Title = request.Title.Trim();
        slide.Subtitle = request.Subtitle?.Trim();
        slide.LinkUrl = request.LinkUrl?.Trim();
        slide.ButtonText = request.ButtonText?.Trim();
        slide.DisplayOrder = request.DisplayOrder;
        slide.IsActive = request.IsActive;
        slide.StartDate = request.StartDate;
        slide.EndDate = request.EndDate;
        slide.UpdatedAt = _clock.UtcNow;

        _images.DeleteFiles(slide.ImagePath);
        slide.ImagePath = image.ImagePath;

        await _slides.UpdateAsync(slide, cancellationToken);
        return MapSlide(slide);
    }

    private async Task<SimpleImageSet> CopyPaintingImageAsync(int paintingImageId, CancellationToken cancellationToken)
    {
        var sourceImage = await _paintings.GetImageAsync(paintingImageId, cancellationToken)
                           ?? throw new NotFoundException("Painting image", paintingImageId);

        var sourceRelativePath = string.IsNullOrEmpty(sourceImage.ResizedPath) ? sourceImage.ThumbnailPath : sourceImage.ResizedPath;
        var physicalPath = _images.ResolvePhysicalPath(sourceRelativePath)
                            ?? throw new NotFoundException("Painting image file", paintingImageId);

        await using var stream = File.OpenRead(physicalPath);
        return await _images.ProcessSimpleImageAsync(stream, Path.GetFileName(physicalPath), _images.ImagesFolder, _images.ThumbsFolder, cancellationToken);
    }

    public async Task<SlideDto> UpdateSlideAsync(int id, UpdateSlideRequest request, Stream? imageContent, string? fileName, CancellationToken cancellationToken = default)
    {
        var slide = await _slides.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException("Slide", id);

        slide.Title = request.Title.Trim();
        slide.Subtitle = request.Subtitle?.Trim();
        slide.LinkUrl = request.LinkUrl?.Trim();
        slide.ButtonText = request.ButtonText?.Trim();
        slide.DisplayOrder = request.DisplayOrder;
        slide.IsActive = request.IsActive;
        slide.StartDate = request.StartDate;
        slide.EndDate = request.EndDate;
        slide.UpdatedAt = _clock.UtcNow;

        if (imageContent is not null && !string.IsNullOrWhiteSpace(fileName))
        {
            var image = await _images.ProcessSimpleImageAsync(imageContent, fileName, _images.ImagesFolder, _images.ThumbsFolder, cancellationToken);
            _images.DeleteFiles(slide.ImagePath);
            slide.ImagePath = image.ImagePath;
        }

        await _slides.UpdateAsync(slide, cancellationToken);
        return MapSlide(slide);
    }

    public async Task DeleteSlideAsync(int id, CancellationToken cancellationToken = default)
    {
        var slide = await _slides.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException("Slide", id);
        await _slides.DeleteAsync(id, cancellationToken);
        _images.DeleteFiles(slide.ImagePath);
    }

    public async Task<IReadOnlyList<SettingDto>> GetSettingsAsync(string? group, CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetAllAsync(group, cancellationToken);
        return _mapper.Map<List<SettingDto>>(settings);
    }

    public async Task UpsertSettingsAsync(UpsertSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var settings = request.Settings.Select(s => new Setting
        {
            Key = s.Key.Trim(),
            Value = s.Value,
            Group = s.Group.Trim(),
            Description = s.Description?.Trim(),
            UpdatedAt = now
        }).ToList();

        await _settings.UpsertManyAsync(settings, cancellationToken);
    }

    private SlideDto MapSlide(Slide slide) =>
        _mapper.Map<SlideDto>(slide) with { ImagePath = _images.BuildImageUrl(slide.ImagePath) ?? string.Empty };

    public async Task<IReadOnlyList<TestimonialDto>> GetTestimonialsAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var testimonials = await _testimonials.GetAllAsync(activeOnly, cancellationToken);
        return testimonials.Select(MapTestimonial).ToList();
    }

    public async Task<TestimonialDto> GetTestimonialAsync(int id, CancellationToken cancellationToken = default)
    {
        var testimonial = await _testimonials.GetByIdAsync(id, cancellationToken)
                           ?? throw new NotFoundException("Testimonial", id);
        return MapTestimonial(testimonial);
    }

    public async Task<TestimonialDto> CreateTestimonialAsync(CreateTestimonialRequest request, Stream? imageContent, string? fileName, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var testimonial = new Testimonial
        {
            CustomerName = request.CustomerName.Trim(),
            Comment = request.Comment.Trim(),
            Rating = request.Rating,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (imageContent is not null && !string.IsNullOrWhiteSpace(fileName))
        {
            var image = await _images.ProcessSimpleImageAsync(imageContent, fileName, _images.CustomersFolder, _images.CustomersFolder, cancellationToken);
            testimonial.ImagePath = image.ImagePath;
            testimonial.ThumbnailPath = image.ThumbnailPath;
        }

        testimonial.Id = await _testimonials.CreateAsync(testimonial, cancellationToken);
        return MapTestimonial(testimonial);
    }

    public async Task<TestimonialDto> UpdateTestimonialAsync(int id, UpdateTestimonialRequest request, Stream? imageContent, string? fileName, CancellationToken cancellationToken = default)
    {
        var testimonial = await _testimonials.GetByIdAsync(id, cancellationToken)
                           ?? throw new NotFoundException("Testimonial", id);

        testimonial.CustomerName = request.CustomerName.Trim();
        testimonial.Comment = request.Comment.Trim();
        testimonial.Rating = request.Rating;
        testimonial.DisplayOrder = request.DisplayOrder;
        testimonial.IsActive = request.IsActive;
        testimonial.UpdatedAt = _clock.UtcNow;

        if (imageContent is not null && !string.IsNullOrWhiteSpace(fileName))
        {
            var image = await _images.ProcessSimpleImageAsync(imageContent, fileName, _images.CustomersFolder, _images.CustomersFolder, cancellationToken);
            _images.DeleteFiles(testimonial.ImagePath, testimonial.ThumbnailPath);
            testimonial.ImagePath = image.ImagePath;
            testimonial.ThumbnailPath = image.ThumbnailPath;
        }

        await _testimonials.UpdateAsync(testimonial, cancellationToken);
        return MapTestimonial(testimonial);
    }

    public async Task DeleteTestimonialAsync(int id, CancellationToken cancellationToken = default)
    {
        var testimonial = await _testimonials.GetByIdAsync(id, cancellationToken)
                           ?? throw new NotFoundException("Testimonial", id);
        await _testimonials.DeleteAsync(id, cancellationToken);
        _images.DeleteFiles(testimonial.ImagePath, testimonial.ThumbnailPath);
    }

    private TestimonialDto MapTestimonial(Testimonial testimonial) => new()
    {
        Id = testimonial.Id,
        CustomerName = testimonial.CustomerName,
        Comment = testimonial.Comment,
        Rating = testimonial.Rating,
        ImagePath = _images.BuildCustomerUrl(testimonial.ImagePath),
        ThumbnailPath = _images.BuildCustomerUrl(testimonial.ThumbnailPath),
        DisplayOrder = testimonial.DisplayOrder,
        IsActive = testimonial.IsActive
    };
}
