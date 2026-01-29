using FluentValidation;

namespace ZenGear.Application.Features.Authentication.Commands.Logout;

/// <summary>
/// Validator for LogoutCommand.
/// </summary>
public class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required.");
    }
}
