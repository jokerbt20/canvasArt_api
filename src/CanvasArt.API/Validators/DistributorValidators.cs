using CanvasArt.API.Models.DTOs.Distributors;
using FluentValidation;

namespace CanvasArt.API.Validators;

public sealed class CreateDistributorRequestValidator : AbstractValidator<CreateDistributorRequest>
{
    public CreateDistributorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(40);
    }
}

public sealed class UpdateDistributorRequestValidator : AbstractValidator<UpdateDistributorRequest>
{
    public UpdateDistributorRequestValidator()
    {
        Include(new CreateDistributorRequestValidator());
    }
}

public sealed class CreatePromoCodeRequestValidator : AbstractValidator<CreatePromoCodeRequest>
{
    public CreatePromoCodeRequestValidator()
    {
        RuleFor(x => x.DistributorId).GreaterThan(0);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DiscountPercentage).GreaterThan(0).LessThanOrEqualTo(100)
            .WithMessage("Discount percentage must be between 0 and 100.");
        RuleFor(x => x.EndsAt).GreaterThanOrEqualTo(x => x.StartsAt!.Value)
            .When(x => x.StartsAt.HasValue && x.EndsAt.HasValue)
            .WithMessage("End date must be on or after the start date.");
    }
}

public sealed class UpdatePromoCodeRequestValidator : AbstractValidator<UpdatePromoCodeRequest>
{
    public UpdatePromoCodeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DiscountPercentage).GreaterThan(0).LessThanOrEqualTo(100)
            .WithMessage("Discount percentage must be between 0 and 100.");
        RuleFor(x => x.EndsAt).GreaterThanOrEqualTo(x => x.StartsAt!.Value)
            .When(x => x.StartsAt.HasValue && x.EndsAt.HasValue)
            .WithMessage("End date must be on or after the start date.");
    }
}

public sealed class ApplyPromoCodeRequestValidator : AbstractValidator<ApplyPromoCodeRequest>
{
    public ApplyPromoCodeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
    }
}
