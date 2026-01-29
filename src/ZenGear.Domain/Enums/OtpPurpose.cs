namespace ZenGear.Domain.Enums;

/// <summary>
/// Purpose of OTP code.
/// </summary>
public enum OtpPurpose
{
    /// <summary>
    /// OTP for email verification during registration.
    /// </summary>
    EmailVerification = 1,

    /// <summary>
    /// OTP for password reset.
    /// </summary>
    PasswordReset = 2
}
