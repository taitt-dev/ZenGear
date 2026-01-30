using System.Text.Json.Serialization;
using MediatR;
using ZenGear.Application.Common.Models;

namespace ZenGear.Application.Features.Authentication.Commands.VerifyEmail;

/// <summary>
/// Command to verify email with OTP code.
/// After successful verification, user can login.
/// </summary>
public record VerifyEmailCommand : IRequest<Result>
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("otpCode")]
    public required string OtpCode { get; init; }
}
