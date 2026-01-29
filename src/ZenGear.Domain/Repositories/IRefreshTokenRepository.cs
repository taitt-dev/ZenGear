namespace ZenGear.Domain.Repositories;

/// <summary>
/// Repository for refresh token operations.
/// Manages refresh tokens for authentication.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Get active refresh token by token value.
    /// </summary>
    Task<RefreshTokenInfo?> GetByTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Store new refresh token.
    /// </summary>
    Task<string> CreateAsync(long userId, string token, DateTimeOffset expiresAt, CancellationToken ct = default);

    /// <summary>
    /// Revoke refresh token.
    /// </summary>
    Task RevokeAsync(string token, string? replacedByToken = null, CancellationToken ct = default);

    /// <summary>
    /// Revoke all active refresh tokens for a user.
    /// </summary>
    Task RevokeAllForUserAsync(long userId, CancellationToken ct = default);
}

/// <summary>
/// Refresh token information DTO.
/// Used to avoid coupling Application layer to Infrastructure.
/// </summary>
public class RefreshTokenInfo
{
    public required long Id { get; init; }
    public required long UserId { get; init; }
    public required string Token { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? RevokedAt { get; init; }
    public string? ReplacedByToken { get; init; }
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTimeOffset.UtcNow;
}
