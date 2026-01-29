using ZenGear.Domain.Enums;

namespace ZenGear.Application.Common.Interfaces;

/// <summary>
/// Service for OTP (One-Time Password) operations.
/// Implementation in Infrastructure layer.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generate a new 6-digit OTP code.
    /// Cryptographically secure random generation.
    /// </summary>
    string GenerateOtpCode();

    /// <summary>
    /// Create and store OTP for user.
    /// Invalidates all previous OTPs for the same purpose.
    /// </summary>
    /// <param name="userId">Internal user ID (long)</param>
    /// <param name="purpose">OTP purpose (EmailVerification or PasswordReset)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Generated OTP code</returns>
    Task<string> CreateOtpAsync(
        long userId,
        OtpPurpose purpose,
        CancellationToken ct = default);

    /// <summary>
    /// Validate OTP code for user.
    /// Marks OTP as used if valid.
    /// </summary>
    /// <param name="userId">Internal user ID (long)</param>
    /// <param name="code">OTP code to validate</param>
    /// <param name="purpose">Expected OTP purpose</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if valid and marked as used, false otherwise</returns>
    Task<bool> ValidateOtpAsync(
        long userId,
        string code,
        OtpPurpose purpose,
        CancellationToken ct = default);

    /// <summary>
    /// Invalidate all OTPs for user and purpose.
    /// Used before generating new OTP or after successful use.
    /// </summary>
    Task InvalidateOtpsAsync(
        long userId,
        OtpPurpose purpose,
        CancellationToken ct = default);

    /// <summary>
    /// Check if rate limit is exceeded for OTP requests.
    /// </summary>
    /// <param name="userId">Internal user ID (long)</param>
    /// <param name="purpose">OTP purpose</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if rate limit exceeded, false otherwise</returns>
    Task<bool> IsRateLimitExceededAsync(
        long userId,
        OtpPurpose purpose,
        CancellationToken ct = default);
}
