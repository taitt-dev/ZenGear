using FluentValidation;

namespace ZenGear.Application.Common.Validators;

/// <summary>
/// Reusable email validator.
/// Use with FluentValidation's RuleFor().SetValidator().
/// </summary>
public class EmailValidator : AbstractValidator<string>
{
    public EmailValidator()
    {
        RuleFor(email => email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");
    }
}
