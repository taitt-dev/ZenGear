using FluentValidation;
using System.Text.RegularExpressions;

namespace ZenGear.Application.Common.Validators;

/// <summary>
/// Reusable password strength validator.
/// Requirements: Min 8 chars, uppercase, lowercase, digit, special char.
/// </summary>
public partial class PasswordValidator : AbstractValidator<string>
{
    public PasswordValidator()
    {
        RuleFor(password => password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Must(HaveUppercase).WithMessage("Password must contain at least one uppercase letter.")
            .Must(HaveLowercase).WithMessage("Password must contain at least one lowercase letter.")
            .Must(HaveDigit).WithMessage("Password must contain at least one digit.")
            .Must(HaveSpecialChar).WithMessage("Password must contain at least one special character.");
    }

    private static bool HaveUppercase(string password)
        => UppercaseRegex().IsMatch(password);

    private static bool HaveLowercase(string password)
        => LowercaseRegex().IsMatch(password);

    private static bool HaveDigit(string password)
        => DigitRegex().IsMatch(password);

    private static bool HaveSpecialChar(string password)
        => SpecialCharRegex().IsMatch(password);

    [GeneratedRegex(@"[A-Z]")]
    private static partial Regex UppercaseRegex();

    [GeneratedRegex(@"[a-z]")]
    private static partial Regex LowercaseRegex();

    [GeneratedRegex(@"\d")]
    private static partial Regex DigitRegex();

    [GeneratedRegex(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>?/]")]
    private static partial Regex SpecialCharRegex();
}
