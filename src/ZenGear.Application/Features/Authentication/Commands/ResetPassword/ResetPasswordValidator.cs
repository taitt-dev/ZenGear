using FluentValidation;
using ZenGear.Application.Common.Validators;

namespace ZenGear.Application.Features.Authentication.Commands.ResetPassword;

/// <summary>
/// Validator for ResetPasswordCommand.
/// </summary>
public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email)
            .SetValidator(new EmailValidator());

        RuleFor(x => x.OtpCode)
            .SetValidator(new OtpValidator());

        RuleFor(x => x.NewPassword)
            .SetValidator(new PasswordValidator());
    }
}
