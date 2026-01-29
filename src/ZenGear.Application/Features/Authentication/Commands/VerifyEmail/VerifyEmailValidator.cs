using FluentValidation;
using ZenGear.Application.Common.Validators;

namespace ZenGear.Application.Features.Authentication.Commands.VerifyEmail;

/// <summary>
/// Validator for VerifyEmailCommand.
/// </summary>
public class VerifyEmailValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailValidator()
    {
        RuleFor(x => x.Email)
            .SetValidator(new EmailValidator());

        RuleFor(x => x.OtpCode)
            .SetValidator(new OtpValidator());
    }
}
