namespace ZenGear.Application.Common.Interfaces;

/// <summary>
/// Email service for sending emails.
/// Implementation in Infrastructure layer (SMTP, SendGrid, etc.).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send email verification OTP.
    /// </summary>
    /// <param name="email">Recipient email</param>
    /// <param name="firstName">Recipient first name</param>
    /// <param name="otpCode">6-digit OTP code</param>
    Task SendEmailVerificationAsync(string email, string firstName, string otpCode, CancellationToken ct = default);

    /// <summary>
    /// Send password reset OTP.
    /// </summary>
    /// <param name="email">Recipient email</param>
    /// <param name="firstName">Recipient first name</param>
    /// <param name="otpCode">6-digit OTP code</param>
    Task SendPasswordResetAsync(string email, string firstName, string otpCode, CancellationToken ct = default);

    /// <summary>
    /// Send welcome email after registration.
    /// </summary>
    /// <param name="email">Recipient email</param>
    /// <param name="firstName">Recipient first name</param>
    Task SendWelcomeEmailAsync(string email, string firstName, CancellationToken ct = default);
}
