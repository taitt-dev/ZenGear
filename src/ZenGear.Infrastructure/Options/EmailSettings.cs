namespace ZenGear.Infrastructure.Options;

/// <summary>
/// Email (SMTP) configuration settings.
/// Loaded from appsettings.json EmailSettings section.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    /// <summary>
    /// Sender email address.
    /// </summary>
    public string FromEmail { get; set; } = null!;

    /// <summary>
    /// Sender display name.
    /// </summary>
    public string FromName { get; set; } = "ZenGear";

    /// <summary>
    /// SMTP server host (e.g., smtp.gmail.com).
    /// </summary>
    public string SmtpHost { get; set; } = null!;

    /// <summary>
    /// SMTP server port (e.g., 587 for TLS).
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// SMTP username (usually same as FromEmail).
    /// </summary>
    public string SmtpUser { get; set; } = null!;

    /// <summary>
    /// SMTP password or app-specific password.
    /// </summary>
    public string SmtpPassword { get; set; } = null!;

    /// <summary>
    /// Enable SSL/TLS.
    /// </summary>
    public bool EnableSsl { get; set; } = true;
}
