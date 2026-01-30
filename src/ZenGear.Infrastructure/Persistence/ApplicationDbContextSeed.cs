using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZenGear.Application.Common.Constants;
using ZenGear.Domain.Common;
using ZenGear.Domain.Constants;
using ZenGear.Domain.Enums;
using ZenGear.Infrastructure.Identity;

namespace ZenGear.Infrastructure.Persistence;

/// <summary>
/// Seeds initial data for development and testing.
/// Includes roles and test users.
/// </summary>
public static class ApplicationDbContextSeed
{
    /// <summary>
    /// Seed initial data (roles and admin user).
    /// Call this in Program.cs on application startup.
    /// </summary>
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<long>> roleManager,
        IExternalIdGenerator externalIdGenerator)
    {
        // Ensure database is created
        await context.Database.MigrateAsync();

        // Seed roles
        await SeedRolesAsync(roleManager);

        // Seed admin user
        await SeedAdminUserAsync(userManager, externalIdGenerator);
    }

    /// <summary>
    /// Seed default roles.
    /// </summary>
    private static async Task SeedRolesAsync(RoleManager<IdentityRole<long>> roleManager)
    {
        var roles = new[]
        {
            Roles.Admin,
            Roles.Manager,
            Roles.Staff,
            Roles.Customer
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<long>(role));
            }
        }
    }

    /// <summary>
    /// Seed default admin user.
    /// Email: admin@zengear.com
    /// Password: Admin123!
    /// </summary>
    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        IExternalIdGenerator externalIdGenerator)
    {
        const string adminEmail = "admin@zengear.com";
        const string adminPassword = "Admin123!";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

        if (existingAdmin != null)
            return;

        var adminExternalId = externalIdGenerator.Generate(EntityPrefixes.User);

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            ExternalId = adminExternalId,
            FirstName = "Admin",
            LastName = "User",
            Status = UserStatus.Active,
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(admin, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }
}
