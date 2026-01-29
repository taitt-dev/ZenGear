using ZenGear.Domain.Enums;

namespace ZenGear.Application.Common.Interfaces;

/// <summary>
/// Service for user identity operations.
/// Abstraction over ASP.NET Core Identity.
/// Implementation in Infrastructure layer.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Create a new user with email and password.
    /// </summary>
    /// <param name="externalId">Generated external ID (usr_xxx)</param>
    /// <param name="email">User email</param>
    /// <param name="password">User password</param>
    /// <param name="firstName">First name</param>
    /// <param name="lastName">Last name</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>(Succeeded, UserId, Errors)</returns>
    Task<(bool Succeeded, long UserId, string[] Errors)> CreateUserAsync(
        string externalId,
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken ct = default);

    /// <summary>
    /// Create a new user from Google OAuth.
    /// </summary>
    /// <param name="externalId">Generated external ID (usr_xxx)</param>
    /// <param name="email">Google email</param>
    /// <param name="firstName">First name from Google</param>
    /// <param name="lastName">Last name from Google</param>
    /// <param name="avatarUrl">Avatar URL from Google</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>(Succeeded, UserId, Errors)</returns>
    Task<(bool Succeeded, long UserId, string[] Errors)> CreateUserFromGoogleAsync(
        string externalId,
        string email,
        string firstName,
        string lastName,
        string? avatarUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Check if password is correct for user.
    /// </summary>
    Task<bool> CheckPasswordAsync(string email, string password, CancellationToken ct = default);

    /// <summary>
    /// Get user by email.
    /// </summary>
    Task<UserInfo?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Get user by external ID.
    /// </summary>
    Task<UserInfo?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);

    /// <summary>
    /// Get user by internal ID.
    /// </summary>
    Task<UserInfo?> GetByIdAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Check if email exists.
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Confirm user email.
    /// </summary>
    Task<bool> ConfirmEmailAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Change user password.
    /// </summary>
    Task<(bool Succeeded, string[] Errors)> ChangePasswordAsync(
        long userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default);

    /// <summary>
    /// Reset user password (admin/OTP-based).
    /// </summary>
    Task<(bool Succeeded, string[] Errors)> ResetPasswordAsync(
        long userId,
        string newPassword,
        CancellationToken ct = default);

    /// <summary>
    /// Increment failed login count.
    /// Returns true if account should be locked out.
    /// </summary>
    Task<bool> IncrementAccessFailedCountAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Reset failed login count (after successful login).
    /// </summary>
    Task ResetAccessFailedCountAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Update security stamp (invalidates all tokens).
    /// </summary>
    Task UpdateSecurityStampAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Get user roles.
    /// </summary>
    Task<string[]> GetRolesAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Add user to role.
    /// </summary>
    Task<bool> AddToRoleAsync(long userId, string role, CancellationToken ct = default);

    /// <summary>
    /// Remove user from role.
    /// </summary>
    Task<bool> RemoveFromRoleAsync(long userId, string role, CancellationToken ct = default);

    /// <summary>
    /// Check if user is in lockout.
    /// </summary>
    Task<bool> IsLockedOutAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Get lockout end time.
    /// </summary>
    Task<DateTimeOffset?> GetLockoutEndAsync(long userId, CancellationToken ct = default);
}

/// <summary>
/// User information DTO for Identity operations.
/// This is returned by IIdentityService, NOT exposed in API.
/// </summary>
public class UserInfo
{
    public required long Id { get; init; }
    public required string ExternalId { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}";
    public string? AvatarUrl { get; init; }
    public required UserStatus Status { get; init; }
    public required bool EmailConfirmed { get; init; }
    public required string[] Roles { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
