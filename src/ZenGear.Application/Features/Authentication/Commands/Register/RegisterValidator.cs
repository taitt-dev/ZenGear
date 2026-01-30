using FluentValidation;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Common.Validators;

namespace ZenGear.Application.Features.Authentication.Commands.Register;

/// <summary>
/// Validator for RegisterCommand.
/// </summary>
public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    private readonly IIdentityService _identityService;

    public RegisterValidator(IIdentityService identityService)
    {
        _identityService = identityService;

        RuleFor(x => x.Email)
            .SetValidator(new EmailValidator());

        RuleFor(x => x.Email)
            .MustAsync(BeUniqueEmail)
            .WithMessage("Email is already registered.")
            .WithErrorCode("EMAIL_ALREADY_EXISTS");

        RuleFor(x => x.Password)
            .SetValidator(new PasswordValidator());

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Last name must not exceed 100 characters.");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken ct)
    {
        var exists = await _identityService.EmailExistsAsync(email, ct);
        return !exists;
    }
}
