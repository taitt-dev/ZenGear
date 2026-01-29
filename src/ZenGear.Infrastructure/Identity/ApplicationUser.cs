using Microsoft.AspNetCore.Identity;
using ZenGear.Domain.Enums;

namespace ZenGear.Infrastructure.Identity;

/// <summary>
/// Application user entity extending ASP.NET Core Identity.
/// Lives in Infrastructure layer (not Domain) - authentication is infrastructure concern.
/// Uses long (BIGSERIAL) for internal Id and string (NanoId) for ExternalId.
/// </summary>
public class ApplicationUser : IdentityUser<long>
{
    /// <summary>
    /// External identifier for API responses.
    /// Format: usr_{16-char-nanoid} (e.g., usr_V1StGXR8Z5jdHi6B)
    /// This is what clients see as "id" in JSON responses.
    /// </summary>
    public string ExternalId { get; set; } = null!;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; set; } = null!;

    /// <summary>
    /// Computed full name (FirstName + LastName).
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Avatar image URL (Firebase Storage, Cloudinary, etc.).
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Account status (Active, Inactive, Banned).
    /// </summary>
    public UserStatus Status { get; set; }

    /// <summary>
    /// When the user account was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the user account was last updated (UTC).
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public virtual ICollection<EmailOtp> EmailOtps { get; set; } = [];
}
