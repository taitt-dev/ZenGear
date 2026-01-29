using System.Text.Json.Serialization;

namespace ZenGear.Application.Features.Authentication.DTOs;

/// <summary>
/// Authentication response with access and refresh tokens.
/// </summary>
public record AuthenticationDto
{
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("refreshToken")]
    public required string RefreshToken { get; init; }

    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { get; init; }

    [JsonPropertyName("user")]
    public required UserDto User { get; init; }
}

/// <summary>
/// User information in authentication response.
/// </summary>
public record UserDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }  // ExternalId

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("firstName")]
    public required string FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public required string LastName { get; init; }

    [JsonPropertyName("fullName")]
    public required string FullName { get; init; }

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; init; }

    [JsonPropertyName("roles")]
    public required string[] Roles { get; init; }

    [JsonPropertyName("emailConfirmed")]
    public required bool EmailConfirmed { get; init; }
}

/// <summary>
/// Refresh token response.
/// </summary>
public record RefreshTokenDto
{
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("refreshToken")]
    public required string RefreshToken { get; init; }

    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { get; init; }
}
