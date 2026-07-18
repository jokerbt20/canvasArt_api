using CanvasArt.API.Models.DTOs.Auth;
using FluentValidation;

namespace CanvasArt.API.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MaximumLength(32).When(x => x.PhoneNumber is not null);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.AccessToken).NotEmpty();
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RevokeTokenRequestValidator : AbstractValidator<RevokeTokenRequest>
{
    public RevokeTokenRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]");
    }
}

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    private static readonly string[] AllowedRoles = { "Administrator", "Guest" };

    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).NotEmpty()
            .Must(r => AllowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role must be either 'Administrator' or 'Guest'.");
    }
}
