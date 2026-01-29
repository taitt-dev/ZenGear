using System.Security.Claims;

namespace ZenGear.Application.Common.Interfaces;

/// <summary>
/// Service for JWT token generation and validation.
/// Implementation in Infrastructure layer.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate JWT access token.
    /// Token contains ExternalId in "sub" claim (NOT internal long Id).
    /// </summary>
    /// <param name="userExternalId">User external ID (e.g., usr_xxx)</param>
    /// <param name="email">User email</param>
    /// <param name="fullName">User full name</param>
    /// <param name="roles">User roles</param>
    /// <returns>JWT token string</returns>
    string GenerateAccessToken(
        string userExternalId,
        string email,
        string fullName,
        string[] roles);

    /// <summary>
    /// Generate refresh token (UUID).
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validate JWT access token.
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>ClaimsPrincipal if valid, null if invalid</returns>
    ClaimsPrincipal? ValidateAccessToken(string token);

    /// <summary>
    /// Get principal from expired token (for refresh flow).
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    /// <summary>
    /// Get access token expiration time.
    /// </summary>
    DateTimeOffset GetAccessTokenExpiration();

    /// <summary>
    /// Get refresh token expiration time.
    /// </summary>
    DateTimeOffset GetRefreshTokenExpiration();
}
