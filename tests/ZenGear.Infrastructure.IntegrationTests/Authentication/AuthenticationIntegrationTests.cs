using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Domain.Common;
using ZenGear.Domain.Constants;
using ZenGear.Infrastructure.Identity;
using ZenGear.Infrastructure.Persistence;
using ZenGear.Infrastructure.Persistence.Repositories;
using ZenGear.Infrastructure.Services;

namespace ZenGear.Infrastructure.IntegrationTests.Authentication;

/// <summary>
/// Integration tests for Authentication flows.
/// Focus: ExternalId consistency across all layers.
/// </summary>
public class AuthenticationIntegrationTests : IAsyncDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IExternalIdGenerator _externalIdGenerator;
    private readonly IDateTime _dateTime;

    public AuthenticationIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dateTime = new DateTimeService();
        _context = new ApplicationDbContext(options, new NoOpMediator(), _dateTime);

        // Setup Identity
        var userStore = new TestUserStore(_context);
        var userManager = new UserManager<ApplicationUser>(
            userStore,
            null!,
            new PasswordHasher<ApplicationUser>(),
            null!,
            null!,
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            null!);

        // Seed roles
        _context.Roles.AddRange(
            new IdentityRole<long> { Id = 1, Name = Roles.Customer, NormalizedName = "CUSTOMER" },
            new IdentityRole<long> { Id = 2, Name = Roles.Admin, NormalizedName = "ADMIN" }
        );
        _context.SaveChanges();

        _identityService = new IdentityService(userManager, null!, _context, _dateTime);
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "ThisIsAVerySecureSecretKeyForIntegrationTestingPurposesOnly123",
                ["JwtSettings:Issuer"] = "ZenGear.Test",
                ["JwtSettings:Audience"] = "ZenGear.Client.Test",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "15",
                ["JwtSettings:RefreshTokenExpirationDays"] = "7"
            })
            .Build();

        _tokenService = new TokenService(config, _dateTime);
        _externalIdGenerator = new NanoIdGenerator();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    #region ExternalId Generation & Database Tests

    [Fact]
    public async Task CreateUser_ShouldGenerateValidExternalId()
    {
        // Arrange
        var email = "test@example.com";
        var externalId = _externalIdGenerator.Generate(EntityPrefixes.User);

        // Act
        var (succeeded, userId, _) = await _identityService.CreateUserAsync(
            externalId, email, "Password@123", "Test", "User");

        // Assert
        succeeded.Should().BeTrue();
        userId.Should().BeGreaterThan(0); // Internal long Id

        // Verify ExternalId format
        externalId.Should().StartWith("usr_");
        externalId.Should().HaveLength(20); // "usr_" (4) + NanoId (16)
        _externalIdGenerator.IsValid(externalId, EntityPrefixes.User).Should().BeTrue();

        // Verify in database
        var user = await _context.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId);
        user.Should().NotBeNull();
        user!.Id.Should().Be(userId); // Internal Id matches
        user.ExternalId.Should().Be(externalId); // ExternalId matches
    }

    #endregion

    #region JWT Token Tests

    [Fact]
    public async Task GenerateAccessToken_ShouldContainExternalIdInSubClaim()
    {
        // Arrange
        var email = "test@example.com";
        var externalId = _externalIdGenerator.Generate(EntityPrefixes.User);
        
        var (succeeded, userId, _) = await _identityService.CreateUserAsync(
            externalId, email, "Password@123", "Test", "User");

        succeeded.Should().BeTrue();

        // Act - Generate JWT
        var accessToken = _tokenService.GenerateAccessToken(
            userId,
            externalId,
            email,
            "Test User",
            new[] { Roles.Customer });

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        // CRITICAL: "sub" claim must be ExternalId, NOT internal long Id
        var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Should().NotBeNull();
        subClaim!.Value.Should().Be(externalId); // ExternalId, not userId.ToString()
        
        // Verify it's NOT internal Id
        subClaim.Value.Should().NotBe(userId.ToString());
    }

    [Fact]
    public async Task ValidateToken_ShouldPreserveExternalIdInPrincipal()
    {
        // Arrange
        var email = "test@example.com";
        var externalId = _externalIdGenerator.Generate(EntityPrefixes.User);
        
        var (succeeded, userId, _) = await _identityService.CreateUserAsync(
            externalId, email, "Password@123", "Test", "User");

        var accessToken = _tokenService.GenerateAccessToken(
            userId, externalId, email, "Test User", new[] { Roles.Customer });

        // Act - Validate token
        var principal = _tokenService.ValidateAccessToken(accessToken);

        // Assert
        principal.Should().NotBeNull();
        
        var nameIdClaims = principal!.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
            .Select(c => c.Value)
            .ToList();

        // ExternalId should be preserved in NameIdentifier claims
        nameIdClaims.Should().Contain(externalId);
    }

    #endregion

    #region ExternalId Consistency Tests

    [Fact]
    public async Task ExternalId_ShouldBeConsistentAcrossAllLayers()
    {
        // Arrange
        var email = "test@example.com";
        var externalId = _externalIdGenerator.Generate(EntityPrefixes.User);

        // Act & Assert - Create user
        var (succeeded, userId, _) = await _identityService.CreateUserAsync(
            externalId, email, "Password@123", "Test", "User");
        
        succeeded.Should().BeTrue();

        // Layer 1: Database
        var dbUser = await _context.Users.FirstAsync(u => u.Id == userId);
        dbUser.ExternalId.Should().Be(externalId);

        // Layer 2: IdentityService
        var userInfo = await _identityService.GetByExternalIdAsync(externalId);
        userInfo.Should().NotBeNull();
        userInfo!.ExternalId.Should().Be(externalId);

        // Layer 3: JWT Token
        var accessToken = _tokenService.GenerateAccessToken(
            userId, externalId, email, "Test User", new[] { Roles.Customer });

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        var subClaim = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub);
        
        subClaim.Value.Should().Be(externalId);

        // CONCLUSION: ExternalId remains consistent across all layers
    }

    [Fact]
    public async Task ExternalId_ShouldBeUniqueForEachUser()
    {
        // Arrange & Act - Create multiple users
        var externalIds = new List<string>();
        
        for (int i = 0; i < 10; i++)
        {
            var externalId = _externalIdGenerator.Generate(EntityPrefixes.User);
            var (succeeded, _, _) = await _identityService.CreateUserAsync(
                externalId,
                $"user{i}@example.com",
                "Password@123",
                "Test",
                $"User{i}");

            succeeded.Should().BeTrue();
            externalIds.Add(externalId);
        }

        // Assert - All ExternalIds should be unique
        externalIds.Should().OnlyHaveUniqueItems();
        externalIds.Should().AllSatisfy(id => 
        {
            id.Should().StartWith("usr_");
            _externalIdGenerator.IsValid(id, EntityPrefixes.User).Should().BeTrue();
        });
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task InternalId_ShouldNeverBeExposedInJwt()
    {
        // Arrange
        var email = "test@example.com";
        var externalId = _externalIdGenerator.Generate(EntityPrefixes.User);
        
        var (succeeded, userId, _) = await _identityService.CreateUserAsync(
            externalId, email, "Password@123", "Test", "User");

        // Act - Generate JWT
        var accessToken = _tokenService.GenerateAccessToken(
            userId, externalId, email, "Test User", new[] { Roles.Customer });

        // Assert - Decode JWT and verify internal Id is not in "sub" claim
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        // The "sub" claim specifically should NOT contain internal userId
        var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Should().NotBeNull();
        subClaim!.Value.Should().NotBe(userId.ToString());
        subClaim.Value.Should().Be(externalId); // Should be ExternalId instead
    }

    #endregion
}

/// <summary>
/// Minimal UserStore implementation for tests.
/// </summary>
internal class TestUserStore : IUserStore<ApplicationUser>, IUserEmailStore<ApplicationUser>,
    IUserPasswordStore<ApplicationUser>, IUserLockoutStore<ApplicationUser>, IUserRoleStore<ApplicationUser>
{
    private readonly ApplicationDbContext _context;

    public TestUserStore(ApplicationDbContext context) => _context = context;

    public void Dispose() { }

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken ct)
        => Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken ct)
        => Task.FromResult(user.UserName);

    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken ct)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken ct)
        => Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken ct)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken ct)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken ct)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken ct)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(ct);
        return IdentityResult.Success;
    }

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken ct)
        => _context.Users.FirstOrDefaultAsync(u => u.Id == long.Parse(userId), ct);

    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct)
        => _context.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, ct);

    // IUserEmailStore
    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken ct)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken ct)
        => Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken ct)
        => Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken ct)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken ct)
        => _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken ct)
        => Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken ct)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    // IUserPasswordStore
    public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken ct)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken ct)
        => Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken ct)
        => Task.FromResult(user.PasswordHash != null);

    // IUserLockoutStore
    public Task<DateTimeOffset?> GetLockoutEndDateAsync(ApplicationUser user, CancellationToken ct)
        => Task.FromResult(user.LockoutEnd);

    public Task SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd, CancellationToken ct)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public async Task<int> IncrementAccessFailedCountAsync(ApplicationUser user, CancellationToken ct)
    {
        user.AccessFailedCount++;
        await UpdateAsync(user, ct);
        return user.AccessFailedCount;
    }

    public async Task ResetAccessFailedCountAsync(ApplicationUser user, CancellationToken ct)
    {
        user.AccessFailedCount = 0;
        await UpdateAsync(user, ct);
    }

    public Task<int> GetAccessFailedCountAsync(ApplicationUser user, CancellationToken ct)
        => Task.FromResult(user.AccessFailedCount);

    public Task<bool> GetLockoutEnabledAsync(ApplicationUser user, CancellationToken ct)
        => Task.FromResult(user.LockoutEnabled);

    public Task SetLockoutEnabledAsync(ApplicationUser user, bool enabled, CancellationToken ct)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    // IUserRoleStore
    public async Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken ct)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant(), ct);
        if (role != null)
        {
            _context.UserRoles.Add(new IdentityUserRole<long> { UserId = user.Id, RoleId = role.Id });
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken ct)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant(), ct);
        if (role != null)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, ct);
            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync(ct);
            }
        }
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken ct)
    {
        var roleIds = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        var roleNames = await _context.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name!)
            .ToListAsync(ct);

        return roleNames;
    }

    public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken ct)
    {
        var roles = await GetRolesAsync(user, ct);
        return roles.Contains(roleName);
    }

    public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken ct)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant(), ct);
        if (role == null)
            return new List<ApplicationUser>();

        var userIds = await _context.UserRoles
            .Where(ur => ur.RoleId == role.Id)
            .Select(ur => ur.UserId)
            .ToListAsync(ct);

        return await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(ct);
    }
}
