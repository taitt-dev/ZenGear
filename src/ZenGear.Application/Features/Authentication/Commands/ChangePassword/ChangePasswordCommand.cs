using System.Text.Json.Serialization;
using MediatR;
using ZenGear.Application.Common.Models;

namespace ZenGear.Application.Features.Authentication.Commands.ChangePassword;

/// <summary>
/// Command to change user password (requires current password).
/// Used by authenticated users.
/// </summary>
public record ChangePasswordCommand : IRequest<Result>
{
    [JsonPropertyName("currentPassword")]
    public required string CurrentPassword { get; init; }

    [JsonPropertyName("newPassword")]
    public required string NewPassword { get; init; }
}
