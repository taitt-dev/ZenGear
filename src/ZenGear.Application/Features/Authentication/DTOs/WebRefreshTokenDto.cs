using System.Text.Json.Serialization;

namespace ZenGear.Application.Features.Authentication.DTOs;

/// <summary>
/// Refresh token response for web browsers.
/// Only contains new access token. New refresh token is sent via httpOnly cookie.
/// </summary>
public record WebRefreshTokenDto
{
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { get; init; }
}
