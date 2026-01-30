using FluentValidation;
using ZenGear.Application.Common.Validators;

namespace ZenGear.Application.Features.Authentication.Commands.ResendVerificationEmail;

/// <summary>
/// Validator for ResendVerificationEmailCommand.
/// </summary>
public class ResendVerificationEmailValidator : AbstractValidator<ResendVerificationEmailCommand>
{
    public ResendVerificationEmailValidator()
    {
        RuleFor(x => x.Email)
            .SetValidator(new EmailValidator());
    }
}
