using FluentValidation;
using ZenGear.Application.Common.Validators;

namespace ZenGear.Application.Features.Authentication.Commands.ForgotPassword;

/// <summary>
/// Validator for ForgotPasswordCommand.
/// </summary>
public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .SetValidator(new EmailValidator());
    }
}
