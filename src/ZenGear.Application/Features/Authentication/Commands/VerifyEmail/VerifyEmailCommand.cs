using System.Text.Json.Serialization;
using MediatR;
using ZenGear.Application.Common.Models;
using ZenGear.Application.Features.Authentication.DTOs;

namespace ZenGear.Application.Features.Authentication.Commands.VerifyEmail;

/// <summary>
/// Command to verify email with OTP code.
/// After successful verification, user is automatically logged in.
/// </summary>
public record VerifyEmailCommand : IRequest<Result<AuthenticationDto>>
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("otpCode")]
    public required string OtpCode { get; init; }
}
