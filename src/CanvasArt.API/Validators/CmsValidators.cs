using CanvasArt.API.Models.DTOs.Cms;
using FluentValidation;

namespace CanvasArt.API.Validators;

public sealed class CreateSlideRequestValidator : AbstractValidator<CreateSlideRequest>
{
    public CreateSlideRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Subtitle).MaximumLength(300);
        RuleFor(x => x.LinkUrl).MaximumLength(500);
        RuleFor(x => x.ButtonText).MaximumLength(80);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}

public sealed class UpdateSlideRequestValidator : AbstractValidator<UpdateSlideRequest>
{
    public UpdateSlideRequestValidator() => Include(new CreateSlideRequestValidator());
}

public sealed class CreateTestimonialRequestValidator : AbstractValidator<CreateTestimonialRequest>
{
    public CreateTestimonialRequestValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Rating).InclusiveBetween((byte)1, (byte)5).When(x => x.Rating.HasValue);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateTestimonialRequestValidator : AbstractValidator<UpdateTestimonialRequest>
{
    public UpdateTestimonialRequestValidator() => Include(new CreateTestimonialRequestValidator());
}

public sealed class UpsertSettingRequestValidator : AbstractValidator<UpsertSettingRequest>
{
    public UpsertSettingRequestValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Group).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Value).MaximumLength(4000);
    }
}

public sealed class UpsertSettingsRequestValidator : AbstractValidator<UpsertSettingsRequest>
{
    public UpsertSettingsRequestValidator()
    {
        RuleFor(x => x.Settings).NotEmpty();
        RuleForEach(x => x.Settings).SetValidator(new UpsertSettingRequestValidator());
    }
}
