using FluentValidation;

namespace ZenGear.Application.Features.Authentication.Commands.LogoutAll;

/// <summary>
/// Validator for LogoutAllCommand.
/// No validation needed as command has no parameters.
/// </summary>
public class LogoutAllValidator : AbstractValidator<LogoutAllCommand>
{
    public LogoutAllValidator()
    {
        // No validation rules - command has no parameters
    }
}
