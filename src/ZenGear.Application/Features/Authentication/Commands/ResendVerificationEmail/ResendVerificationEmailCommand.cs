using System.Text.Json.Serialization;
using MediatR;
using ZenGear.Application.Common.Models;

namespace ZenGear.Application.Features.Authentication.Commands.ResendVerificationEmail;

/// <summary>
/// Command to resend email verification OTP.
/// </summary>
public record ResendVerificationEmailCommand : IRequest<Result>
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }
}
