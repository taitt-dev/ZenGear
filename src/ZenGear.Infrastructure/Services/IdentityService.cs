using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Domain.Common;
using ZenGear.Domain.Constants;
using ZenGear.Domain.Enums;
using ZenGear.Infrastructure.Identity;
using ZenGear.Infrastructure.Persistence;

namespace ZenGear.Infrastructure.Services;

/// <summary>
/// Service for user identity management (registration, login, password management).
/// Wraps ASP.NET Core Identity with our domain logic.
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IDateTime dateTime)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<(bool Succeeded, long UserId, string[] Errors)> CreateUserAsync(
        string externalId,
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            ExternalId = externalId,
            FirstName = firstName,
            LastName = lastName,
            Status = UserStatus.Active,
            CreatedAt = _dateTime.UtcNow,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return (false, 0, result.Errors.Select(e => e.Description).ToArray());
        }

        await _userManager.AddToRoleAsync(user, "Customer");

        return (true, user.Id, []);
    }

    public async Task<(bool Succeeded, long UserId, string[] Errors)> CreateUserFromGoogleAsync(
        string externalId,
        string email,
        string firstName,
        string lastName,
        string? avatarUrl,
        CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            ExternalId = externalId,
            FirstName = firstName,
            LastName = lastName,
            AvatarUrl = avatarUrl,
            Status = UserStatus.Active,
            CreatedAt = _dateTime.UtcNow,
            EmailConfirmed = true  // Google emails are pre-verified
        };

        var result = await _userManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            return (false, 0, result.Errors.Select(e => e.Description).ToArray());
        }

        await _userManager.AddToRoleAsync(user, "Customer");

        return (true, user.Id, []);
    }

    public async Task<bool> CheckPasswordAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return false;

        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<UserInfo?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserInfo
        {
            Id = user.Id,
            ExternalId = user.ExternalId,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles.ToArray(),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserInfo?> GetByExternalIdAsync(string externalId, CancellationToken ct = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, ct);

        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserInfo
        {
            Id = user.Id,
            ExternalId = user.ExternalId,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles.ToArray(),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserInfo?> GetByIdAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserInfo
        {
            Id = user.Id,
            ExternalId = user.ExternalId,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles.ToArray(),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        return await _context.Users.AnyAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), ct);
    }

    public async Task<bool> ConfirmEmailAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return false;

        user.EmailConfirmed = true;
        user.UpdatedAt = _dateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<(bool Succeeded, string[] Errors)> ChangePasswordAsync(
        long userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            return (false, ["User not found."]);
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description).ToArray());
        }

        user.UpdatedAt = _dateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return (true, []);
    }

    public async Task<(bool Succeeded, string[] Errors)> ResetPasswordAsync(
        long userId,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            return (false, ["User not found."]);
        }

        await _userManager.RemovePasswordAsync(user);
        var result = await _userManager.AddPasswordAsync(user, newPassword);

        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description).ToArray());
        }

        user.UpdatedAt = _dateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return (true, []);
    }

    public async Task<bool> IncrementAccessFailedCountAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return false;

        await _userManager.AccessFailedAsync(user);
        return await _userManager.IsLockedOutAsync(user);
    }

    public async Task ResetAccessFailedCountAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user != null)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
        }
    }

    public async Task UpdateSecurityStampAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user != null)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }
    }

    public async Task<string[]> GetRolesAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return [];

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToArray();
    }

    public async Task<bool> AddToRoleAsync(long userId, string role, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return false;

        var result = await _userManager.AddToRoleAsync(user, role);
        return result.Succeeded;
    }

    public async Task<bool> RemoveFromRoleAsync(long userId, string role, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return false;

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        return result.Succeeded;
    }

    public async Task<bool> IsLockedOutAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return false;

        return await _userManager.IsLockedOutAsync(user);
    }

    public async Task<DateTimeOffset?> GetLockoutEndAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return null;

        return await _userManager.GetLockoutEndDateAsync(user);
    }
}
