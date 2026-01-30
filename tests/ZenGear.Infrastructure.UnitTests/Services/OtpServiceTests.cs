using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Domain.Enums;
using ZenGear.Infrastructure.Identity;
using ZenGear.Infrastructure.Persistence;
using ZenGear.Infrastructure.Services;

namespace ZenGear.Infrastructure.UnitTests.Services;

/// <summary>
/// Unit tests for OtpService.
/// Tests OTP generation, validation, expiry, single-use, and rate limiting.
/// </summary>
public class OtpServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IDateTime> _dateTimeMock;
    private readonly OtpService _otpService;
    private readonly DateTimeOffset _fixedNow = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    public OtpServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mediatorMock = new Mock<IMediator>();
        _dateTimeMock = new Mock<IDateTime>();
        _dateTimeMock.Setup(x => x.UtcNow).Returns(_fixedNow);

        _context = new ApplicationDbContext(options, mediatorMock.Object, _dateTimeMock.Object);
        _otpService = new OtpService(_context, _dateTimeMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GenerateOtpCode Tests

    [Fact]
    public void GenerateOtpCode_ShouldReturn6DigitCode()
    {
        // Act
        var code = _otpService.GenerateOtpCode();

        // Assert
        code.Should().NotBeNullOrWhiteSpace();
        code.Should().HaveLength(6);
        code.Should().MatchRegex(@"^\d{6}$"); // Should be 6 digits
    }

    [Fact]
    public void GenerateOtpCode_ShouldReturnDifferentCodes()
    {
        // Act
        var code1 = _otpService.GenerateOtpCode();
        var code2 = _otpService.GenerateOtpCode();
        var code3 = _otpService.GenerateOtpCode();

        // Assert - Codes should be different (with very high probability)
        var codes = new[] { code1, code2, code3 };
        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GenerateOtpCode_ShouldBeInValidRange()
    {
        // Act
        var code = _otpService.GenerateOtpCode();

        // Assert
        var number = int.Parse(code);
        number.Should().BeGreaterThanOrEqualTo(100000);
        number.Should().BeLessThan(1000000);
    }

    #endregion

    #region CreateOtpAsync Tests

    [Fact]
    public async Task CreateOtpAsync_ShouldCreateOtpInDatabase()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Act
        var code = await _otpService.CreateOtpAsync(userId, purpose);

        // Assert
        code.Should().NotBeNullOrWhiteSpace();
        code.Should().HaveLength(6);

        var otp = await _context.EmailOtps
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Code == code);

        otp.Should().NotBeNull();
        otp!.UserId.Should().Be(userId);
        otp.Code.Should().Be(code);
        otp.Purpose.Should().Be(purpose);
        otp.CreatedAt.Should().Be(_fixedNow);
        otp.ExpiresAt.Should().Be(_fixedNow.AddMinutes(10));
        otp.IsUsed.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOtpAsync_WithEmailVerificationPurpose_ShouldSetCorrectPurpose()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Act
        var code = await _otpService.CreateOtpAsync(userId, purpose);

        // Assert
        var otp = await _context.EmailOtps.FirstOrDefaultAsync(o => o.Code == code);
        otp.Should().NotBeNull();
        otp!.Purpose.Should().Be(OtpPurpose.EmailVerification);
    }

    [Fact]
    public async Task CreateOtpAsync_WithPasswordResetPurpose_ShouldSetCorrectPurpose()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.PasswordReset;

        // Act
        var code = await _otpService.CreateOtpAsync(userId, purpose);

        // Assert
        var otp = await _context.EmailOtps.FirstOrDefaultAsync(o => o.Code == code);
        otp.Should().NotBeNull();
        otp!.Purpose.Should().Be(OtpPurpose.PasswordReset);
    }

    [Fact]
    public async Task CreateOtpAsync_ShouldSetExpiryTo10Minutes()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Act
        var code = await _otpService.CreateOtpAsync(userId, purpose);

        // Assert
        var otp = await _context.EmailOtps.FirstOrDefaultAsync(o => o.Code == code);
        otp.Should().NotBeNull();
        otp!.ExpiresAt.Should().Be(_fixedNow.AddMinutes(10));
    }

    #endregion

    #region ValidateOtpAsync Tests

    [Fact]
    public async Task ValidateOtpAsync_WithValidOtp_ShouldReturnTrueAndMarkAsUsed()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;
        var code = await _otpService.CreateOtpAsync(userId, purpose);

        // Act
        var result = await _otpService.ValidateOtpAsync(userId, code, purpose);

        // Assert
        result.Should().BeTrue();

        // Verify OTP is marked as used
        var otp = await _context.EmailOtps.FirstOrDefaultAsync(o => o.Code == code);
        otp.Should().NotBeNull();
        otp!.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateOtpAsync_WithWrongCode_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;
        await _otpService.CreateOtpAsync(userId, purpose);

        var wrongCode = "999999";

        // Act
        var result = await _otpService.ValidateOtpAsync(userId, wrongCode, purpose);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateOtpAsync_WithWrongPurpose_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;
        var code = await _otpService.CreateOtpAsync(userId, purpose);

        // Act - Validate with different purpose
        var result = await _otpService.ValidateOtpAsync(userId, code, OtpPurpose.PasswordReset);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateOtpAsync_WithExpiredOtp_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;
        var code = await _otpService.CreateOtpAsync(userId, purpose);

        // Advance time to after expiry (10 minutes + 1 second)
        _dateTimeMock.Setup(x => x.UtcNow).Returns(_fixedNow.AddMinutes(10).AddSeconds(1));

        // Act
        var result = await _otpService.ValidateOtpAsync(userId, code, purpose);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateOtpAsync_WithAlreadyUsedOtp_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;
        var code = await _otpService.CreateOtpAsync(userId, purpose);

        // Use OTP once
        await _otpService.ValidateOtpAsync(userId, code, purpose);

        // Act - Try to use again
        var result = await _otpService.ValidateOtpAsync(userId, code, purpose);

        // Assert - Should fail (single-use)
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateOtpAsync_WithOtpForDifferentUser_ShouldReturnFalse()
    {
        // Arrange
        var userId1 = 1L;
        var userId2 = 2L;
        var purpose = OtpPurpose.EmailVerification;
        var code = await _otpService.CreateOtpAsync(userId1, purpose);

        // Act - Different user tries to use OTP
        var result = await _otpService.ValidateOtpAsync(userId2, code, purpose);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateOtpAsync_AtExactExpiryTime_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;
        var code = await _otpService.CreateOtpAsync(userId, purpose);

        // Set time to exactly expiry time
        _dateTimeMock.Setup(x => x.UtcNow).Returns(_fixedNow.AddMinutes(10));

        // Act
        var result = await _otpService.ValidateOtpAsync(userId, code, purpose);

        // Assert - Should fail (ExpiresAt > now, so at exact time should fail)
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateOtpAsync_JustBeforeExpiry_ShouldReturnTrue()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;
        var code = await _otpService.CreateOtpAsync(userId, purpose);

        // Set time to 1 second before expiry
        _dateTimeMock.Setup(x => x.UtcNow).Returns(_fixedNow.AddMinutes(10).AddSeconds(-1));

        // Act
        var result = await _otpService.ValidateOtpAsync(userId, code, purpose);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region InvalidateOtpsAsync Tests

    [Fact]
    public async Task InvalidateOtpsAsync_ShouldMarkAllUnusedOtpsAsUsed()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Create multiple OTPs
        await _otpService.CreateOtpAsync(userId, purpose);
        await _otpService.CreateOtpAsync(userId, purpose);
        await _otpService.CreateOtpAsync(userId, purpose);

        // Act
        await _otpService.InvalidateOtpsAsync(userId, purpose);

        // Assert
        var otps = await _context.EmailOtps
            .Where(o => o.UserId == userId && o.Purpose == purpose)
            .ToListAsync();

        otps.Should().HaveCount(3);
        otps.Should().OnlyContain(o => o.IsUsed);
    }

    [Fact]
    public async Task InvalidateOtpsAsync_ShouldOnlyInvalidateSpecificPurpose()
    {
        // Arrange
        var userId = 1L;

        // Create OTPs for both purposes
        await _otpService.CreateOtpAsync(userId, OtpPurpose.EmailVerification);
        await _otpService.CreateOtpAsync(userId, OtpPurpose.PasswordReset);

        // Act - Invalidate only EmailVerification
        await _otpService.InvalidateOtpsAsync(userId, OtpPurpose.EmailVerification);

        // Assert
        var emailOtps = await _context.EmailOtps
            .Where(o => o.UserId == userId && o.Purpose == OtpPurpose.EmailVerification)
            .ToListAsync();
        emailOtps.Should().OnlyContain(o => o.IsUsed);

        var passwordOtps = await _context.EmailOtps
            .Where(o => o.UserId == userId && o.Purpose == OtpPurpose.PasswordReset)
            .ToListAsync();
        passwordOtps.Should().OnlyContain(o => !o.IsUsed); // Should NOT be invalidated
    }

    [Fact]
    public async Task InvalidateOtpsAsync_ShouldOnlyInvalidateSpecificUser()
    {
        // Arrange
        var userId1 = 1L;
        var userId2 = 2L;
        var purpose = OtpPurpose.EmailVerification;

        // Create OTPs for both users
        await _otpService.CreateOtpAsync(userId1, purpose);
        await _otpService.CreateOtpAsync(userId2, purpose);

        // Act - Invalidate only user 1
        await _otpService.InvalidateOtpsAsync(userId1, purpose);

        // Assert
        var user1Otps = await _context.EmailOtps
            .Where(o => o.UserId == userId1)
            .ToListAsync();
        user1Otps.Should().OnlyContain(o => o.IsUsed);

        var user2Otps = await _context.EmailOtps
            .Where(o => o.UserId == userId2)
            .ToListAsync();
        user2Otps.Should().OnlyContain(o => !o.IsUsed); // Should NOT be affected
    }

    [Fact]
    public async Task InvalidateOtpsAsync_ShouldNotAffectAlreadyUsedOtps()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        var code1 = await _otpService.CreateOtpAsync(userId, purpose);
        await _otpService.CreateOtpAsync(userId, purpose);

        // Mark first OTP as used
        await _otpService.ValidateOtpAsync(userId, code1, purpose);

        // Act
        await _otpService.InvalidateOtpsAsync(userId, purpose);

        // Assert
        var otps = await _context.EmailOtps
            .Where(o => o.UserId == userId && o.Purpose == purpose)
            .ToListAsync();

        otps.Should().HaveCount(2);
        otps.Should().OnlyContain(o => o.IsUsed);
    }

    [Fact]
    public async Task InvalidateOtpsAsync_WithNoOtps_ShouldNotThrow()
    {
        // Arrange
        var userId = 999L;
        var purpose = OtpPurpose.EmailVerification;

        // Act
        var act = async () => await _otpService.InvalidateOtpsAsync(userId, purpose);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region IsRateLimitExceededAsync Tests

    [Fact]
    public async Task IsRateLimitExceededAsync_WithNoRequests_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Act
        var result = await _otpService.IsRateLimitExceededAsync(userId, purpose);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRateLimitExceededAsync_WithinLimit_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Create 4 OTPs (under limit of 5)
        for (int i = 0; i < 4; i++)
        {
            await _otpService.CreateOtpAsync(userId, purpose);
        }

        // Act
        var result = await _otpService.IsRateLimitExceededAsync(userId, purpose);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRateLimitExceededAsync_AtExactLimit_ShouldReturnTrue()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Create 5 OTPs (at limit)
        for (int i = 0; i < 5; i++)
        {
            await _otpService.CreateOtpAsync(userId, purpose);
        }

        // Act
        var result = await _otpService.IsRateLimitExceededAsync(userId, purpose);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsRateLimitExceededAsync_OverLimit_ShouldReturnTrue()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Create 6 OTPs (over limit)
        for (int i = 0; i < 6; i++)
        {
            await _otpService.CreateOtpAsync(userId, purpose);
        }

        // Act
        var result = await _otpService.IsRateLimitExceededAsync(userId, purpose);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsRateLimitExceededAsync_ShouldOnlyCountRecentRequests()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Create 5 OTPs in the past (16 minutes ago - outside window)
        _dateTimeMock.Setup(x => x.UtcNow).Returns(_fixedNow.AddMinutes(-16));
        for (int i = 0; i < 5; i++)
        {
            await _otpService.CreateOtpAsync(userId, purpose);
        }

        // Reset time to now
        _dateTimeMock.Setup(x => x.UtcNow).Returns(_fixedNow);

        // Create 1 new OTP
        await _otpService.CreateOtpAsync(userId, purpose);

        // Act
        var result = await _otpService.IsRateLimitExceededAsync(userId, purpose);

        // Assert - Only 1 recent request (within 15 min window)
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRateLimitExceededAsync_ShouldCountRequestsInLast15Minutes()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Create OTPs at different times
        // t=0: 2 OTPs
        await _otpService.CreateOtpAsync(userId, purpose);
        await _otpService.CreateOtpAsync(userId, purpose);

        // t=5: 2 OTPs
        _dateTimeMock.Setup(x => x.UtcNow).Returns(_fixedNow.AddMinutes(5));
        await _otpService.CreateOtpAsync(userId, purpose);
        await _otpService.CreateOtpAsync(userId, purpose);

        // t=10: Check rate limit
        _dateTimeMock.Setup(x => x.UtcNow).Returns(_fixedNow.AddMinutes(10));

        // Act - All 4 requests are within 15-min window
        var result = await _otpService.IsRateLimitExceededAsync(userId, purpose);

        // Assert
        result.Should().BeFalse(); // 4 < 5
    }

    [Fact]
    public async Task IsRateLimitExceededAsync_ShouldBeSeparateByPurpose()
    {
        // Arrange
        var userId = 1L;

        // Create 5 EmailVerification OTPs
        for (int i = 0; i < 5; i++)
        {
            await _otpService.CreateOtpAsync(userId, OtpPurpose.EmailVerification);
        }

        // Create 2 PasswordReset OTPs
        for (int i = 0; i < 2; i++)
        {
            await _otpService.CreateOtpAsync(userId, OtpPurpose.PasswordReset);
        }

        // Act
        var emailVerificationLimit = await _otpService.IsRateLimitExceededAsync(userId, OtpPurpose.EmailVerification);
        var passwordResetLimit = await _otpService.IsRateLimitExceededAsync(userId, OtpPurpose.PasswordReset);

        // Assert
        emailVerificationLimit.Should().BeTrue(); // 5 requests
        passwordResetLimit.Should().BeFalse(); // Only 2 requests
    }

    [Fact]
    public async Task IsRateLimitExceededAsync_ShouldBeSeparateByUser()
    {
        // Arrange
        var userId1 = 1L;
        var userId2 = 2L;
        var purpose = OtpPurpose.EmailVerification;

        // User 1: 5 OTPs
        for (int i = 0; i < 5; i++)
        {
            await _otpService.CreateOtpAsync(userId1, purpose);
        }

        // User 2: 2 OTPs
        for (int i = 0; i < 2; i++)
        {
            await _otpService.CreateOtpAsync(userId2, purpose);
        }

        // Act
        var user1Limit = await _otpService.IsRateLimitExceededAsync(userId1, purpose);
        var user2Limit = await _otpService.IsRateLimitExceededAsync(userId2, purpose);

        // Assert
        user1Limit.Should().BeTrue(); // 5 requests
        user2Limit.Should().BeFalse(); // Only 2 requests
    }

    #endregion

    #region Integration Tests (Create + Validate Flow)

    [Fact]
    public async Task CompleteFlow_CreateAndValidate_ShouldSucceed()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Act - Create OTP
        var code = await _otpService.CreateOtpAsync(userId, purpose);

        // Act - Validate OTP
        var result = await _otpService.ValidateOtpAsync(userId, code, purpose);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteFlow_CreateInvalidateAndCreate_ShouldWork()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Create first OTP
        var code1 = await _otpService.CreateOtpAsync(userId, purpose);

        // Invalidate all OTPs
        await _otpService.InvalidateOtpsAsync(userId, purpose);

        // Create new OTP
        var code2 = await _otpService.CreateOtpAsync(userId, purpose);

        // Act - Try to validate old OTP (should fail)
        var oldResult = await _otpService.ValidateOtpAsync(userId, code1, purpose);

        // Act - Validate new OTP (should succeed)
        var newResult = await _otpService.ValidateOtpAsync(userId, code2, purpose);

        // Assert
        oldResult.Should().BeFalse(); // Old OTP invalidated
        newResult.Should().BeTrue(); // New OTP valid
    }

    [Fact]
    public async Task CompleteFlow_RateLimitScenario_ShouldPrevent6thRequest()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Create 5 OTPs (at limit)
        for (int i = 0; i < 5; i++)
        {
            await _otpService.CreateOtpAsync(userId, purpose);
        }

        // Act - Check rate limit before 6th request
        var isLimitExceeded = await _otpService.IsRateLimitExceededAsync(userId, purpose);

        // Assert
        isLimitExceeded.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteFlow_RateLimitReset_AfterWindowExpires()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Create 5 OTPs at t=0 (at limit)
        for (int i = 0; i < 5; i++)
        {
            await _otpService.CreateOtpAsync(userId, purpose);
        }

        // Advance time beyond rate limit window (15 minutes + 1 second)
        _dateTimeMock.Setup(x => x.UtcNow).Returns(_fixedNow.AddMinutes(15).AddSeconds(1));

        // Act - Check rate limit
        var isLimitExceeded = await _otpService.IsRateLimitExceededAsync(userId, purpose);

        // Assert - Should be reset
        isLimitExceeded.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ValidateOtpAsync_WithEmptyCode_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Act
        var result = await _otpService.ValidateOtpAsync(userId, string.Empty, purpose);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateOtpAsync_WithNullCode_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Act
        var result = await _otpService.ValidateOtpAsync(userId, null!, purpose);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOtpAsync_MultipleTimes_ShouldCreateMultipleOtps()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Act
        var code1 = await _otpService.CreateOtpAsync(userId, purpose);
        var code2 = await _otpService.CreateOtpAsync(userId, purpose);
        var code3 = await _otpService.CreateOtpAsync(userId, purpose);

        // Assert
        var otps = await _context.EmailOtps
            .Where(o => o.UserId == userId && o.Purpose == purpose)
            .ToListAsync();

        otps.Should().HaveCount(3);
        otps.Select(o => o.Code).Should().Contain(code1);
        otps.Select(o => o.Code).Should().Contain(code2);
        otps.Select(o => o.Code).Should().Contain(code3);
    }

    #endregion

    #region Business Rule Tests

    [Fact]
    public async Task OtpValidityWindow_ShouldBe10Minutes()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Act
        var code = await _otpService.CreateOtpAsync(userId, purpose);

        // Assert
        var otp = await _context.EmailOtps.FirstOrDefaultAsync(o => o.Code == code);
        otp.Should().NotBeNull();

        var expectedExpiry = _fixedNow.AddMinutes(10);
        otp!.ExpiresAt.Should().Be(expectedExpiry);
    }

    [Fact]
    public async Task RateLimitWindow_ShouldBe15Minutes()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Create OTP at t=-16 minutes (outside window)
        _dateTimeMock.Setup(x => x.UtcNow).Returns(_fixedNow.AddMinutes(-16));
        await _otpService.CreateOtpAsync(userId, purpose);

        // Reset to current time
        _dateTimeMock.Setup(x => x.UtcNow).Returns(_fixedNow);

        // Create 4 more OTPs (within window)
        for (int i = 0; i < 4; i++)
        {
            await _otpService.CreateOtpAsync(userId, purpose);
        }

        // Act
        var result = await _otpService.IsRateLimitExceededAsync(userId, purpose);

        // Assert - Only 4 requests in window (old one doesn't count)
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RateLimitMaxRequests_ShouldBe5()
    {
        // Arrange
        var userId = 1L;
        var purpose = OtpPurpose.EmailVerification;

        // Create exactly 5 OTPs
        for (int i = 0; i < 5; i++)
        {
            await _otpService.CreateOtpAsync(userId, purpose);
        }

        // Act
        var result = await _otpService.IsRateLimitExceededAsync(userId, purpose);

        // Assert
        result.Should().BeTrue(); // At limit
    }

    #endregion
}
