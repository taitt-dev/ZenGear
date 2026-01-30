using System.Text.Json.Serialization;

namespace ZenGear.Application.Features.Authentication.DTOs;

/// <summary>
/// Authentication response for web browsers.
/// Refresh token is sent via httpOnly cookie instead of JSON body.
/// </summary>
public record WebAuthenticationDto
{
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { get; init; }

    [JsonPropertyName("user")]
    public required UserDto User { get; init; }
}
