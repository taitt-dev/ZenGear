using System.Text.Json.Serialization;
using MediatR;
using ZenGear.Application.Common.Models;

namespace ZenGear.Application.Features.Authentication.Commands.ResetPassword;

/// <summary>
/// Command to reset password using OTP.
/// Part of forgot password flow.
/// </summary>
public record ResetPasswordCommand : IRequest<Result>
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("otpCode")]
    public required string OtpCode { get; init; }

    [JsonPropertyName("newPassword")]
    public required string NewPassword { get; init; }
}
