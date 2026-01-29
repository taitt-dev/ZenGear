using FluentValidation;

namespace ZenGear.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Validator for RefreshTokenCommand.
/// </summary>
public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required.");
    }
}
