using CanvasArt.API.Models.DTOs.Categories;
using CanvasArt.API.Models.DTOs.Frames;
using CanvasArt.API.Models.DTOs.Paintings;
using CanvasArt.API.Models.DTOs.Tags;
using FluentValidation;

namespace CanvasArt.API.Validators;

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Slug).MaximumLength(180);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Slug).MaximumLength(180);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Slug).MaximumLength(100);
    }
}

public sealed class UpdateTagRequestValidator : AbstractValidator<UpdateTagRequest>
{
    public UpdateTagRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Slug).MaximumLength(100);
    }
}

public sealed class PaintingSizeInputValidator : AbstractValidator<PaintingSizeInput>
{
    public PaintingSizeInputValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(50);
        RuleFor(x => x.WidthCm).GreaterThan(0);
        RuleFor(x => x.HeightCm).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Sku).MaximumLength(64);
    }
}

public sealed class CreatePaintingRequestValidator : AbstractValidator<CreatePaintingRequest>
{
    public CreatePaintingRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.Slug).MaximumLength(220);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Context).MaximumLength(4000);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Sizes).NotEmpty().WithMessage("At least one size is required.");
        RuleForEach(x => x.Sizes).SetValidator(new PaintingSizeInputValidator());
        RuleFor(x => x.Sizes)
            .Must(s => s.Count(sz => sz.IsDefault) <= 1)
            .WithMessage("Only one size can be marked as default.");
    }
}

public sealed class UpdatePaintingRequestValidator : AbstractValidator<UpdatePaintingRequest>
{
    public UpdatePaintingRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(220);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Context).MaximumLength(4000);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Sizes).NotEmpty().WithMessage("At least one size is required.");
        RuleForEach(x => x.Sizes).SetValidator(new PaintingSizeInputValidator());
        RuleFor(x => x.Sizes)
            .Must(s => s.Count(sz => sz.IsDefault) <= 1)
            .WithMessage("Only one size can be marked as default.");
    }
}

public sealed class CreateFrameRequestValidator : AbstractValidator<CreateFrameRequest>
{
    public CreateFrameRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Material).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateFrameRequestValidator : AbstractValidator<UpdateFrameRequest>
{
    public UpdateFrameRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Material).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(60);
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
    }
}
