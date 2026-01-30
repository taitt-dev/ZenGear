using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Features.Authentication.Commands.ForgotPassword;
using ZenGear.Application.Features.Authentication.Commands.Login;
using ZenGear.Application.Features.Authentication.Commands.RefreshToken;
using ZenGear.Application.Features.Authentication.Commands.Register;
using ZenGear.Application.Features.Authentication.Commands.ResetPassword;
using ZenGear.Application.Features.Authentication.Commands.VerifyEmail;
using ZenGear.Domain.Common;
using ZenGear.Domain.Constants;
using ZenGear.Domain.Enums;
using ZenGear.Domain.Repositories;
using ZenGear.Infrastructure.Identity;
using ZenGear.Infrastructure.Persistence;
using ZenGear.Infrastructure.Persistence.Repositories;
using ZenGear.Infrastructure.Services;

namespace ZenGear.Infrastructure.IntegrationTests.Authentication;

/// <summary>
/// Integration tests for complete authentication workflows.
/// Tests full end-to-end user journeys with multiple steps.
/// </summary>
public class AuthenticationWorkflowTests : IAsyncDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IOtpService _otpService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IExternalIdGenerator _externalIdGenerator;
    private readonly IDateTime _dateTime;

    public AuthenticationWorkflowTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dateTime = new DateTimeService();
        _context = new ApplicationDbContext(options, new NoOpMediator(), _dateTime);

        // Setup Identity with custom UserStore
        var userStore = new WorkflowTestUserStore(_context);
        var userManager = new UserManager<ApplicationUser>(
            userStore,
            null!,
            new PasswordHasher<ApplicationUser>(),
            null!,
            null!,
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance); // Add logger

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
        _otpService = new OtpService(_context, _dateTime);
        _refreshTokenRepository = new RefreshTokenRepository(_context, _dateTime);
        _externalIdGenerator = new NanoIdGenerator();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    #region Complete Registration → Verification → Login Workflow

    [Fact]
    public async Task Workflow_RegisterVerifyLogin_ShouldSucceedWithExternalId()
    {
        // ========== STEP 1: Register ==========
        var registerHandler = new RegisterHandler(
            _identityService,
            _otpService,
            null!, // EmailService
            _externalIdGenerator);

        var registerResult = await registerHandler.Handle(new RegisterCommand
        {
            Email = "customer@example.com",
            Password = "Password@123",
            FirstName = "Nguyen",
            LastName = "Van A"
        }, default);

        // Assert registration
        registerResult.Succeeded.Should().BeTrue();
        
        // Get user from database (no Data property - Result not Result<T>)
        var user = await _context.Users.FirstAsync(u => u.Email == "customer@example.com");
        var userExternalId = user.ExternalId;
        userExternalId.Should().StartWith("usr_");
        user.EmailConfirmed.Should().BeFalse(); // Not confirmed yet

        // Get OTP
        var otp = await _context.EmailOtps
            .FirstAsync(o => o.UserId == user.Id && o.Purpose == OtpPurpose.EmailVerification);

        // ========== STEP 2: Verify Email ==========
        var verifyHandler = new VerifyEmailHandler(
            _identityService,
            _otpService,
            null!);

        var verifyResult = await verifyHandler.Handle(new VerifyEmailCommand
        {
            Email = "customer@example.com",
            OtpCode = otp.Code
        }, default);

        // Assert verification
        verifyResult.Succeeded.Should().BeTrue();

        // User should now be confirmed
        var confirmedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        confirmedUser.EmailConfirmed.Should().BeTrue();

        // ========== STEP 3: Login ==========
        var loginHandler = new LoginHandler(
            _identityService,
            _tokenService,
            _refreshTokenRepository);

        var loginResult = await loginHandler.Handle(new LoginCommand
        {
            Email = "customer@example.com",
            Password = "Password@123"
        }, default);

        // Assert login
        loginResult.Succeeded.Should().BeTrue();
        loginResult.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        loginResult.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        loginResult.Data.User.Id.Should().Be(userExternalId); // CRITICAL: ExternalId in response

        // Verify JWT contains ExternalId
        var jwtHandler = new JwtSecurityTokenHandler();
        var jwt = jwtHandler.ReadJwtToken(loginResult.Data.AccessToken);
        var subClaim = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Value.Should().Be(userExternalId); // CRITICAL: JWT "sub" = ExternalId
    }

    #endregion

    #region Password Reset Workflow

    [Fact]
    public async Task Workflow_ForgotPasswordReset_ShouldSucceed()
    {
        // ========== STEP 1: Create and confirm user ==========
        var email = "customer@example.com";
        var externalId = _externalIdGenerator.Generate(EntityPrefixes.User);
        
        var (succeeded, userId, _) = await _identityService.CreateUserAsync(
            externalId, email, "OldPassword@123", "Test", "User");

        succeeded.Should().BeTrue();
        await _identityService.ConfirmEmailAsync(userId);

        // Create a refresh token (simulate logged in session)
        var refreshToken1 = await _refreshTokenRepository.CreateAsync(
            userId,
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow.AddDays(7));

        // ========== STEP 2: Forgot Password (request OTP) ==========
        var mockEmailService = new MockEmailService(); // Use mock instead of null!
        var forgotHandler = new ForgotPasswordHandler(
            _identityService,
            _otpService,
            mockEmailService);

        var forgotResult = await forgotHandler.Handle(new ForgotPasswordCommand
        {
            Email = email
        }, default);

        forgotResult.Succeeded.Should().BeTrue();

        // Get OTP
        var otp = await _context.EmailOtps
            .FirstAsync(o => o.UserId == userId && o.Purpose == OtpPurpose.PasswordReset);

        // ========== STEP 3: Reset Password ==========
        var resetHandler = new ResetPasswordHandler(
            _identityService,
            _otpService,
            _refreshTokenRepository);

        var resetResult = await resetHandler.Handle(new ResetPasswordCommand
        {
            Email = email,
            OtpCode = otp.Code,
            NewPassword = "NewPassword@123"
        }, default);

        // Assert reset success
        resetResult.Succeeded.Should().BeTrue();

        // Verify OTP marked as used
        var usedOtp = await _context.EmailOtps.FirstAsync(o => o.Id == otp.Id);
        usedOtp.IsUsed.Should().BeTrue();

        // Verify old refresh token revoked (security: logout all devices)
        var revokedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken1);
        revokedToken.Should().NotBeNull();
        revokedToken!.IsActive.Should().BeFalse(); // Should be revoked

        // ========== STEP 4: Login with new password ==========
        var loginHandler = new LoginHandler(
            _identityService,
            _tokenService,
            _refreshTokenRepository);

        var loginResult = await loginHandler.Handle(new LoginCommand
        {
            Email = email,
            Password = "NewPassword@123"
        }, default);

        // Assert login with new password succeeds
        loginResult.Succeeded.Should().BeTrue();

        // Verify cannot login with old password
        var oldPasswordResult = await loginHandler.Handle(new LoginCommand
        {
            Email = email,
            Password = "OldPassword@123"
        }, default);

        oldPasswordResult.Succeeded.Should().BeFalse();
    }

    #endregion

    #region Token Refresh Workflow

    [Fact]
    public async Task Workflow_LoginRefreshLogout_ShouldRotateTokens()
    {
        // ========== STEP 1: Create and confirm user ==========
        var email = "customer@example.com";
        var externalId = _externalIdGenerator.Generate(EntityPrefixes.User);
        
        var (succeeded, userId, _) = await _identityService.CreateUserAsync(
            externalId, email, "Password@123", "Test", "User");

        succeeded.Should().BeTrue();
        await _identityService.ConfirmEmailAsync(userId);

        // ========== STEP 2: Login ==========
        var loginHandler = new LoginHandler(
            _identityService,
            _tokenService,
            _refreshTokenRepository);

        var loginResult = await loginHandler.Handle(new LoginCommand
        {
            Email = email,
            Password = "Password@123"
        }, default);

        loginResult.Succeeded.Should().BeTrue();
        var originalAccessToken = loginResult.Data!.AccessToken;
        var originalRefreshToken = loginResult.Data.RefreshToken;

        // Verify original JWT contains ExternalId
        var jwtHandler = new JwtSecurityTokenHandler();
        var originalJwt = jwtHandler.ReadJwtToken(originalAccessToken);
        var originalSub = originalJwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        originalSub.Should().Be(externalId);

        // ========== STEP 3: Refresh Token ==========
        var refreshHandler = new RefreshTokenHandler(
            _identityService,
            _tokenService,
            _refreshTokenRepository);

        var refreshResult = await refreshHandler.Handle(new RefreshTokenCommand
        {
            RefreshToken = originalRefreshToken
        }, default);

        // Assert refresh success
        refreshResult.Succeeded.Should().BeTrue();
        var newAccessToken = refreshResult.Data!.AccessToken;
        var newRefreshToken = refreshResult.Data.RefreshToken;

        // Tokens should be different (rotation)
        newAccessToken.Should().NotBe(originalAccessToken);
        newRefreshToken.Should().NotBe(originalRefreshToken);

        // New JWT should still contain ExternalId
        var newJwt = jwtHandler.ReadJwtToken(newAccessToken);
        var newSub = newJwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        newSub.Should().Be(externalId); // CRITICAL: ExternalId preserved

        // Old refresh token should be revoked
        var oldToken = await _refreshTokenRepository.GetByTokenAsync(originalRefreshToken);
        oldToken.Should().NotBeNull();
        oldToken!.IsActive.Should().BeFalse(); // Revoked
        oldToken.ReplacedByToken.Should().Be(newRefreshToken);

        // ========== STEP 4: Try to use old refresh token (should fail) ==========
        var reuseResult = await refreshHandler.Handle(new RefreshTokenCommand
        {
            RefreshToken = originalRefreshToken
        }, default);

        reuseResult.Succeeded.Should().BeFalse();
        reuseResult.ErrorCode.Should().Be(ErrorCodes.User.RefreshTokenExpired);
    }

    #endregion

    #region Account Lockout Workflow

    [Fact]
    public async Task Workflow_MultipleFailedLoginsThenSuccess_ShouldLockThenUnlock()
    {
        // ========== STEP 1: Create and confirm user ==========
        var email = "customer@example.com";
        var correctPassword = "Password@123";
        var externalId = _externalIdGenerator.Generate(EntityPrefixes.User);

        var (succeeded, userId, _) = await _identityService.CreateUserAsync(
            externalId, email, correctPassword, "Test", "User");

        succeeded.Should().BeTrue();
        await _identityService.ConfirmEmailAsync(userId);

        var loginHandler = new LoginHandler(
            _identityService,
            _tokenService,
            _refreshTokenRepository);

        // ========== STEP 2: 5 failed login attempts ==========
        for (int i = 1; i <= 5; i++)
        {
            var result = await loginHandler.Handle(new LoginCommand
            {
                Email = email,
                Password = "WrongPassword"
            }, default);

            result.Succeeded.Should().BeFalse();

            // Check lockout status
            var isLocked = await _identityService.IsLockedOutAsync(userId);
            
            if (i < 5)
            {
                isLocked.Should().BeFalse($"should not be locked after {i} attempts");
            }
            else
            {
                isLocked.Should().BeTrue("should be locked after 5 failed attempts");
            }
        }

        // ========== STEP 3: Try correct password while locked ==========
        var lockedResult = await loginHandler.Handle(new LoginCommand
        {
            Email = email,
            Password = correctPassword
        }, default);

        lockedResult.Succeeded.Should().BeFalse();
        lockedResult.ErrorCode.Should().Be(ErrorCodes.User.AccountLocked); // USER_ACCOUNT_LOCKED

        // ========== STEP 4: Manually unlock (simulate time passed) ==========
        // In real scenario, lockout would expire after 15 minutes
        var lockedUser = await _context.Users.FirstAsync(u => u.Id == userId);
        lockedUser.LockoutEnd = null;
        lockedUser.AccessFailedCount = 0;
        await _context.SaveChangesAsync();

        // ========== STEP 5: Login with correct password after unlock ==========
        var unlockedResult = await loginHandler.Handle(new LoginCommand
        {
            Email = email,
            Password = correctPassword
        }, default);

        unlockedResult.Succeeded.Should().BeTrue();
        unlockedResult.Data!.User.Id.Should().Be(externalId); // ExternalId in response
    }

    #endregion

    #region OTP Reuse Prevention Workflow

    [Fact]
    public async Task Workflow_ReuseOtp_ShouldFail()
    {
        // ========== STEP 1: Register user ==========
        var email = "customer@example.com";
        var registerHandler = new RegisterHandler(
            _identityService,
            _otpService,
            null!, // EmailService
            _externalIdGenerator);

        var registerResult = await registerHandler.Handle(new RegisterCommand
        {
            Email = email,
            Password = "Password@123",
            FirstName = "Test",
            LastName = "User"
        }, default);

        registerResult.Succeeded.Should().BeTrue();

        var user = await _context.Users.FirstAsync(u => u.Email == email);
        var otp = await _context.EmailOtps
            .FirstAsync(o => o.UserId == user.Id && o.Purpose == OtpPurpose.EmailVerification);

        // ========== STEP 2: Verify email (use OTP first time) ==========
        var verifyHandler = new VerifyEmailHandler(
            _identityService,
            _otpService,
            null!);

        var firstVerify = await verifyHandler.Handle(new VerifyEmailCommand
        {
            Email = email,
            OtpCode = otp.Code
        }, default);

        firstVerify.Succeeded.Should().BeTrue();

        // ========== STEP 3: Try to reuse same OTP (should fail) ==========
        var secondVerify = await verifyHandler.Handle(new VerifyEmailCommand
        {
            Email = email,
            OtpCode = otp.Code
        }, default);

        secondVerify.Succeeded.Should().BeFalse();
        secondVerify.ErrorCode.Should().Be(ErrorCodes.User.EmailAlreadyVerified);

        // Verify OTP is marked as used
        var usedOtp = await _context.EmailOtps.FirstAsync(o => o.Id == otp.Id);
        usedOtp.IsUsed.Should().BeTrue();
    }

    #endregion

    #region Expired OTP Workflow

    [Fact]
    public async Task Workflow_ExpiredOtp_ShouldFailValidation()
    {
        // ========== STEP 1: Create user ==========
        var email = "customer@example.com";
        var externalId = _externalIdGenerator.Generate(EntityPrefixes.User);

        var (succeeded, userId, _) = await _identityService.CreateUserAsync(
            externalId, email, "Password@123", "Test", "User");

        succeeded.Should().BeTrue();

        // ========== STEP 2: Create OTP ==========
        var otpCode = await _otpService.CreateOtpAsync(userId, OtpPurpose.EmailVerification);

        // Manually expire OTP
        var otp = await _context.EmailOtps.FirstAsync(o => o.UserId == userId);
        otp.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1); // Already expired
        await _context.SaveChangesAsync();

        // ========== STEP 3: Try to verify with expired OTP ==========
        var verifyHandler = new VerifyEmailHandler(
            _identityService,
            _otpService,
            null!);

        var verifyResult = await verifyHandler.Handle(new VerifyEmailCommand
        {
            Email = email,
            OtpCode = otpCode
        }, default);

        // Assert should fail
        verifyResult.Succeeded.Should().BeFalse();
        verifyResult.ErrorCode.Should().Be(ErrorCodes.User.InvalidOtpCode);
    }

    #endregion

    #region Token Rotation Security Workflow

    [Fact]
    public async Task Workflow_MultipleRefreshes_ShouldCreateTokenChain()
    {
        // ========== STEP 1: Setup user ==========
        var email = "customer@example.com";
        var externalId = _externalIdGenerator.Generate(EntityPrefixes.User);

        var (succeeded, userId, _) = await _identityService.CreateUserAsync(
            externalId, email, "Password@123", "Test", "User");

        succeeded.Should().BeTrue();
        await _identityService.ConfirmEmailAsync(userId);

        // ========== STEP 2: Login ==========
        var loginHandler = new LoginHandler(
            _identityService,
            _tokenService,
            _refreshTokenRepository);

        var loginResult = await loginHandler.Handle(new LoginCommand
        {
            Email = email,
            Password = "Password@123"
        }, default);

        var token1 = loginResult.Data!.RefreshToken;

        // ========== STEP 3: Refresh 3 times ==========
        var refreshHandler = new RefreshTokenHandler(
            _identityService,
            _tokenService,
            _refreshTokenRepository);

        var refresh1 = await refreshHandler.Handle(new RefreshTokenCommand { RefreshToken = token1 }, default);
        var token2 = refresh1.Data!.RefreshToken;

        var refresh2 = await refreshHandler.Handle(new RefreshTokenCommand { RefreshToken = token2 }, default);
        var token3 = refresh2.Data!.RefreshToken;

        var refresh3 = await refreshHandler.Handle(new RefreshTokenCommand { RefreshToken = token3 }, default);
        var token4 = refresh3.Data!.RefreshToken;

        // ========== STEP 4: Verify token chain ==========
        var allTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .OrderBy(rt => rt.CreatedAt)
            .ToListAsync();

        allTokens.Should().HaveCount(4); // Initial + 3 refreshes

        // Verify chain: token1 → token2 → token3 → token4
        var token1Info = allTokens[0];
        token1Info.Token.Should().Be(token1);
        token1Info.IsActive.Should().BeFalse(); // Revoked
        token1Info.ReplacedByToken.Should().Be(token2);

        var token2Info = allTokens[1];
        token2Info.IsActive.Should().BeFalse(); // Revoked
        token2Info.ReplacedByToken.Should().Be(token3);

        var token3Info = allTokens[2];
        token3Info.IsActive.Should().BeFalse(); // Revoked
        token3Info.ReplacedByToken.Should().Be(token4);

        var token4Info = allTokens[3];
        token4Info.IsActive.Should().BeTrue(); // Latest token is active
        token4Info.ReplacedByToken.Should().BeNullOrEmpty();
    }

    #endregion

    #region Email Not Confirmed Workflow

    [Fact]
    public async Task Workflow_LoginBeforeEmailConfirmation_ShouldFail()
    {
        // ========== STEP 1: Register user (email not confirmed) ==========
        var registerHandler = new RegisterHandler(
            _identityService,
            _otpService,
            null!, // EmailService
            _externalIdGenerator);

        var registerResult = await registerHandler.Handle(new RegisterCommand
        {
            Email = "customer@example.com",
            Password = "Password@123",
            FirstName = "Test",
            LastName = "User"
        }, default);

        registerResult.Succeeded.Should().BeTrue();

        // ========== STEP 2: Try to login without verification ==========
        var loginHandler = new LoginHandler(
            _identityService,
            _tokenService,
            _refreshTokenRepository);

        var loginResult = await loginHandler.Handle(new LoginCommand
        {
            Email = "customer@example.com",
            Password = "Password@123"
        }, default);

        // Assert login should fail
        loginResult.Succeeded.Should().BeFalse();
        loginResult.ErrorCode.Should().Be(ErrorCodes.User.EmailNotVerified);
    }

    #endregion
}

/// <summary>
/// UserStore for workflow tests with full Identity support.
/// </summary>
internal class WorkflowTestUserStore : IUserStore<ApplicationUser>, IUserEmailStore<ApplicationUser>,
    IUserPasswordStore<ApplicationUser>, IUserLockoutStore<ApplicationUser>, IUserRoleStore<ApplicationUser>,
    IUserSecurityStampStore<ApplicationUser>
{
    private readonly ApplicationDbContext _context;

    public WorkflowTestUserStore(ApplicationDbContext context) => _context = context;

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

    // IUserSecurityStampStore
    public Task SetSecurityStampAsync(ApplicationUser user, string stamp, CancellationToken ct)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(ApplicationUser user, CancellationToken ct)
        => Task.FromResult(user.SecurityStamp);
}

/// <summary>
/// Mock email service for tests. Does not send actual emails.
/// </summary>
internal class MockEmailService : IEmailService
{
    public Task SendEmailVerificationAsync(string email, string firstName, string otpCode, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SendPasswordResetAsync(string toEmail, string firstName, string otpCode, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SendWelcomeEmailAsync(string email, string firstName, CancellationToken ct = default)
        => Task.CompletedTask;
}


