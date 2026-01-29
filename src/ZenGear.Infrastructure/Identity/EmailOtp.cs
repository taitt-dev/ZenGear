using ZenGear.Domain.Enums;

namespace ZenGear.Infrastructure.Identity;

/// <summary>
/// Email OTP entity for email verification and password reset.
/// Internal-only entity (NO ExternalId) - never exposed via API.
/// OTP is single-use and expires after 10 minutes.
/// Lives in Infrastructure (not Domain) - authentication infrastructure.
/// </summary>
public class EmailOtp
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
    /// OTP code (6 digits, e.g., "123456").
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// Purpose of the OTP (EmailVerification or PasswordReset).
    /// </summary>
    public OtpPurpose Purpose { get; set; }

    /// <summary>
    /// When the OTP expires (UTC).
    /// Default: 10 minutes from creation.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// When the OTP was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Whether the OTP has been used.
    /// Single-use: once used, cannot be used again.
    /// </summary>
    public bool IsUsed { get; set; }

    // Navigation property
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Check if OTP is expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Check if OTP is valid (not used, not expired).
    /// </summary>
    public bool IsValid => !IsUsed && !IsExpired;
}
