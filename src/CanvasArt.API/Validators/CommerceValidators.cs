using CanvasArt.API.Models.DTOs.Cart;
using CanvasArt.API.Models.DTOs.Orders;
using CanvasArt.API.Models.DTOs.Promotions;
using CanvasArt.API.Models.Enums;
using FluentValidation;

namespace CanvasArt.API.Validators;

public sealed class CreatePromotionRequestValidator : AbstractValidator<CreatePromotionRequest>
{
    public CreatePromotionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.DiscountValue).LessThanOrEqualTo(100)
            .When(x => x.DiscountType == DiscountType.Percentage)
            .WithMessage("Percentage discount cannot exceed 100.");
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");
        RuleFor(x => x.TargetPaintingId).NotNull()
            .When(x => x.PromotionType == PromotionType.Painting && x.TargetCategoryId is null)
            .WithMessage("A painting promotion must target a painting or a category.");
        RuleFor(x => x.TargetFrameId).NotNull()
            .When(x => x.PromotionType == PromotionType.Frame)
            .WithMessage("A frame promotion must target a frame.");
    }
}

public sealed class UpdatePromotionRequestValidator : AbstractValidator<UpdatePromotionRequest>
{
    public UpdatePromotionRequestValidator()
    {
        Include(new CreatePromotionRequestValidator());
    }
}

public sealed class CreateCombinationPromotionRequestValidator : AbstractValidator<CreateCombinationPromotionRequest>
{
    public CreateCombinationPromotionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PaintingId).GreaterThan(0);
        RuleFor(x => x.FrameId).GreaterThan(0);
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.DiscountValue).LessThanOrEqualTo(100)
            .When(x => x.DiscountType == DiscountType.Percentage);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}

public sealed class UpdateCombinationPromotionRequestValidator : AbstractValidator<UpdateCombinationPromotionRequest>
{
    public UpdateCombinationPromotionRequestValidator()
    {
        Include(new CreateCombinationPromotionRequestValidator());
    }
}

public sealed class CartLineRequestValidator : AbstractValidator<CartLineRequest>
{
    public CartLineRequestValidator()
    {
        RuleFor(x => x.PaintingId).GreaterThan(0);
        RuleFor(x => x.PaintingSizeId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 999);
    }
}

public sealed class CartRequestValidator : AbstractValidator<CartRequest>
{
    public CartRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CartLineRequestValidator());
    }
}

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.AddressLine).NotEmpty().MaximumLength(300);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(120);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CartLineRequestValidator());
    }
}

public sealed class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
