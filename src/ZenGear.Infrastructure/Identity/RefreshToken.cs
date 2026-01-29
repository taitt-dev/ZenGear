namespace ZenGear.Infrastructure.Identity;

/// <summary>
/// Refresh token entity for JWT refresh flow.
/// Internal-only entity (NO ExternalId) - never exposed via API.
/// Token rotation: old token is revoked and replaced with new token.
/// Lives in Infrastructure (not Domain) - authentication infrastructure.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Internal database identifier (BIGSERIAL).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Foreign key to User (internal long Id).
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Refresh token value (UUID).
    /// </summary>
    public string Token { get; set; } = null!;

    /// <summary>
    /// When the token expires (UTC).
    /// Default: 7 days from creation.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// When the token was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the token was revoked (UTC).
    /// Null if still active.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Token that replaced this one (for rotation tracking).
    /// </summary>
    public string? ReplacedByToken { get; set; }

    // Navigation property
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Check if token is expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Check if token is revoked.
    /// </summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>
    /// Check if token is active (not expired and not revoked).
    /// </summary>
    public bool IsActive => !IsExpired && !IsRevoked;
}
