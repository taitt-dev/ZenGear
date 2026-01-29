using FluentValidation;
using ZenGear.Application.Common.Validators;

namespace ZenGear.Application.Features.Authentication.Commands.Login;

/// <summary>
/// Validator for LoginCommand.
/// </summary>
public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .SetValidator(new EmailValidator());

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}
