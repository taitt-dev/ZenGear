using System.Text.Json.Serialization;
using MediatR;
using ZenGear.Application.Common.Models;

namespace ZenGear.Application.Features.Authentication.Commands.ForgotPassword;

/// <summary>
/// Command to request password reset OTP.
/// Sends OTP to user's email.
/// </summary>
public record ForgotPasswordCommand : IRequest<Result>
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }
}
