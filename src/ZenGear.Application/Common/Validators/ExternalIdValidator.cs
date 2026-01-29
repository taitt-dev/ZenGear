using FluentValidation;
using ZenGear.Domain.Common;

namespace ZenGear.Application.Common.Validators;

/// <summary>
/// Reusable external ID format validator.
/// Validates format: {prefix}_{16-char-nanoid}
/// </summary>
public class ExternalIdValidator : AbstractValidator<string>
{
    private readonly IExternalIdGenerator _externalIdGenerator;
    private readonly string _expectedPrefix;

    public ExternalIdValidator(IExternalIdGenerator externalIdGenerator, string expectedPrefix)
    {
        _externalIdGenerator = externalIdGenerator;
        _expectedPrefix = expectedPrefix;

        RuleFor(externalId => externalId)
            .NotEmpty().WithMessage($"{_expectedPrefix} ID is required.")
            .Must(IsValidFormat).WithMessage($"Invalid {_expectedPrefix} ID format.");
    }

    private bool IsValidFormat(string externalId)
    {
        return _externalIdGenerator.IsValid(externalId, _expectedPrefix);
    }
}
