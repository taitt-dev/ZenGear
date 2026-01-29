namespace ZenGear.Application.Common.Interfaces;

/// <summary>
/// Service for accessing current authenticated user information.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Internal user ID (long) from JWT claims.
    /// Returns 0 if not authenticated.
    /// </summary>
    long UserId { get; }

    /// <summary>
    /// External user ID (string, e.g., usr_xxx) from JWT "sub" claim.
    /// Returns null if not authenticated.
    /// </summary>
    string? UserExternalId { get; }

    /// <summary>
    /// User email from JWT claims.
    /// Returns null if not authenticated.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// User roles from JWT claims.
    /// Returns empty array if not authenticated.
    /// </summary>
    string[] Roles { get; }
}
