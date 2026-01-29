using System.Text.Json.Serialization;
using MediatR;
using ZenGear.Application.Common.Models;

namespace ZenGear.Application.Features.Authentication.Commands.Logout;

/// <summary>
/// Command to logout (revoke refresh token).
/// </summary>
public record LogoutCommand : IRequest<Result>
{
    [JsonPropertyName("refreshToken")]
    public required string RefreshToken { get; init; }
}
