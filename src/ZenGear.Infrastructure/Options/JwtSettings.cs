namespace ZenGear.Infrastructure.Options;

/// <summary>
/// JWT configuration settings.
/// Loaded from appsettings.json JwtSettings section.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// Secret key for signing JWT tokens.
    /// MUST be at least 32 characters for HS256.
    /// </summary>
    public string SecretKey { get; set; } = null!;

    /// <summary>
    /// Token issuer (your API URL).
    /// </summary>
    public string Issuer { get; set; } = null!;

    /// <summary>
    /// Token audience (your client app URL).
    /// </summary>
    public string Audience { get; set; } = null!;

    /// <summary>
    /// Access token expiration in minutes.
    /// Default: 60 minutes.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration in days.
    /// Default: 7 days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
