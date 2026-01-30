using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using ZenGear.Application.Common.Interfaces;

namespace ZenGear.Infrastructure.Services;

/// <summary>
/// Email service using MailKit (SMTP).
/// Sends transactional emails for OTP verification, password reset, etc.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Send email verification OTP.
    /// </summary>
    public async Task SendEmailVerificationAsync(
        string toEmail,
        string toName,
        string otpCode,
        CancellationToken ct = default)
    {
        var subject = "Verify Your Email - ZenGear";
        var body = $"""
            <html>
            <body>
                <h2>Welcome to ZenGear, {toName}!</h2>
                <p>Please verify your email address by entering this code:</p>
                <h1 style="color: #4CAF50; letter-spacing: 8px; font-family: monospace;">{otpCode}</h1>
                <p>This code will expire in 10 minutes.</p>
                <p>If you didn't create this account, please ignore this email.</p>
                <br>
                <p>Best regards,<br>The ZenGear Team</p>
            </body>
            </html>
            """;

        await SendEmailAsync(toEmail, toName, subject, body, ct);
    }

    /// <summary>
    /// Send password reset OTP.
    /// </summary>
    public async Task SendPasswordResetAsync(
        string toEmail,
        string toName,
        string otpCode,
        CancellationToken ct = default)
    {
        var subject = "Reset Your Password - ZenGear";
        var body = $"""
            <html>
            <body>
                <h2>Password Reset Request</h2>
                <p>Hello {toName},</p>
                <p>We received a request to reset your password. Enter this code to proceed:</p>
                <h1 style="color: #FF5722; letter-spacing: 8px; font-family: monospace;">{otpCode}</h1>
                <p>This code will expire in 10 minutes.</p>
                <p>If you didn't request a password reset, please ignore this email and your password will remain unchanged.</p>
                <br>
                <p>Best regards,<br>The ZenGear Team</p>
            </body>
            </html>
            """;

        await SendEmailAsync(toEmail, toName, subject, body, ct);
    }

    /// <summary>
    /// Send welcome email after email verification.
    /// </summary>
    public async Task SendWelcomeEmailAsync(
        string toEmail,
        string toName,
        CancellationToken ct = default)
    {
        var subject = "Welcome to ZenGear! 🎮";
        var body = $"""
            <html>
            <body>
                <h2>Welcome to ZenGear, {toName}! 🚀</h2>
                <p>Your email has been successfully verified.</p>
                <p>You can now explore our collection of gaming gear and computer parts.</p>
                <p>Start shopping and enjoy exclusive deals!</p>
                <br>
                <p>Best regards,<br>The ZenGear Team</p>
            </body>
            </html>
            """;

        await SendEmailAsync(toEmail, toName, subject, body, ct);
    }

    /// <summary>
    /// Send order confirmation email.
    /// </summary>
    public async Task SendOrderConfirmationAsync(
        string toEmail,
        string toName,
        string orderExternalId,
        decimal totalAmount,
        CancellationToken ct = default)
    {
        var subject = $"Order Confirmation - {orderExternalId}";
        var body = $"""
            <html>
            <body>
                <h2>Order Confirmed!</h2>
                <p>Hello {toName},</p>
                <p>Thank you for your order. Your order has been confirmed.</p>
                <p><strong>Order ID:</strong> {orderExternalId}</p>
                <p><strong>Total:</strong> {totalAmount:N0} VND</p>
                <p>You can track your order status in your account dashboard.</p>
                <br>
                <p>Best regards,<br>The ZenGear Team</p>
            </body>
            </html>
            """;

        await SendEmailAsync(toEmail, toName, subject, body, ct);
    }

    /// <summary>
    /// Send generic email.
    /// </summary>
    public async Task SendEmailAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        try
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var fromEmail = emailSettings["FromEmail"]
                ?? throw new InvalidOperationException("FromEmail not configured.");
            var fromName = emailSettings["FromName"] ?? "ZenGear";
            var smtpHost = emailSettings["SmtpHost"]
                ?? throw new InvalidOperationException("SmtpHost not configured.");
            var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
            var smtpUser = emailSettings["SmtpUser"] ?? string.Empty;
            var smtpPassword = emailSettings["SmtpPassword"] ?? string.Empty;
            var enableSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Determine SecureSocketOptions based on EnableSsl setting
            var secureSocketOptions = enableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            // Connect to SMTP server
            await client.ConnectAsync(smtpHost, smtpPort, secureSocketOptions, ct);

            // Authenticate (skip if credentials are empty - for MailHog)
            if (!string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPassword))
            {
                await client.AuthenticateAsync(smtpUser, smtpPassword, ct);
            }

            // Send email
            await client.SendAsync(message, ct);

            // Disconnect
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }
}
