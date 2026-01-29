using FluentValidation;

namespace ZenGear.Application.Common.Validators;

/// <summary>
/// Reusable OTP code validator.
/// Requirements: 6 digits, numeric only.
/// </summary>
public class OtpValidator : AbstractValidator<string>
{
    public OtpValidator()
    {
        RuleFor(code => code)
            .NotEmpty().WithMessage("OTP code is required.")
            .Length(6).WithMessage("OTP code must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("OTP code must contain only digits.");
    }
}
