using FluentAssertions;
using Moq;
using Xunit;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Features.Authentication.Commands.VerifyEmail;
using ZenGear.Domain.Enums;
using ZenGear.Domain.Repositories;

namespace ZenGear.Application.UnitTests.Features.Authentication.Commands;

/// <summary>
/// Unit tests for VerifyEmailHandler.
/// </summary>
public class VerifyEmailHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly VerifyEmailHandler _handler;

    public VerifyEmailHandlerTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _otpServiceMock = new Mock<IOtpService>();
        _emailServiceMock = new Mock<IEmailService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();

        _handler = new VerifyEmailHandler(
            _identityServiceMock.Object,
            _otpServiceMock.Object,
            _emailServiceMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidOtp_ShouldVerifyEmailAndReturnTokens()
    {
        // Arrange
        var command = new VerifyEmailCommand
        {
            Email = "test@example.com",
            OtpCode = "123456"
        };

        var userInfo = new UserInfo
        {
            Id = 1L,
            ExternalId = "usr_test123",
            Email = command.Email,
            FirstName = "John",
            LastName = "Doe",
            AvatarUrl = null,
            Status = UserStatus.Active,
            EmailConfirmed = false,
            Roles = ["Customer"],
            CreatedAt = DateTimeOffset.UtcNow
        };

        _identityServiceMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _otpServiceMock
            .Setup(x => x.ValidateOtpAsync(
                userInfo.Id,
                command.OtpCode,
                OtpPurpose.EmailVerification,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _identityServiceMock
            .Setup(x => x.ConfirmEmailAsync(userInfo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _identityServiceMock
            .Setup(x => x.GetByIdAsync(userInfo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo
            {
                Id = userInfo.Id,
                ExternalId = userInfo.ExternalId,
                Email = userInfo.Email,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                AvatarUrl = userInfo.AvatarUrl,
                Status = userInfo.Status,
                EmailConfirmed = true,
                Roles = userInfo.Roles,
                CreatedAt = userInfo.CreatedAt
            });

        var accessToken = "test-access-token";
        var refreshToken = "test-refresh-token";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(
                userInfo.Id,
                userInfo.ExternalId,
                userInfo.Email,
                userInfo.FullName,
                It.IsAny<string[]>()))
            .Returns(accessToken);

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        _tokenServiceMock
            .Setup(x => x.GetAccessTokenExpiration())
            .Returns(expiresAt);

        _tokenServiceMock
            .Setup(x => x.GetRefreshTokenExpiration())
            .Returns(DateTimeOffset.UtcNow.AddDays(7));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be(accessToken);
        result.Data.RefreshToken.Should().Be(refreshToken);
        result.Data.User.Id.Should().Be(userInfo.ExternalId);
        result.Data.User.Email.Should().Be(userInfo.Email);
        result.Data.User.EmailConfirmed.Should().BeTrue();

        _identityServiceMock.Verify(
            x => x.ConfirmEmailAsync(userInfo.Id, It.IsAny<CancellationToken>()),
            Times.Once);

        _refreshTokenRepositoryMock.Verify(
            x => x.CreateAsync(
                userInfo.Id,
                refreshToken,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailServiceMock.Verify(
            x => x.SendWelcomeEmailAsync(
                userInfo.Email,
                userInfo.FirstName,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyVerified_ShouldReturnFailure()
    {
        // Arrange
        var command = new VerifyEmailCommand
        {
            Email = "test@example.com",
            OtpCode = "123456"
        };

        var userInfo = new UserInfo
        {
            Id = 1L,
            ExternalId = "usr_test123",
            Email = command.Email,
            FirstName = "John",
            LastName = "Doe",
            AvatarUrl = null,
            Status = UserStatus.Active,
            EmailConfirmed = true,  // Already verified
            Roles = ["Customer"],
            CreatedAt = DateTimeOffset.UtcNow
        };

        _identityServiceMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.User.EmailAlreadyVerified);

        _otpServiceMock.Verify(
            x => x.ValidateOtpAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<OtpPurpose>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidOtp_ShouldReturnFailure()
    {
        // Arrange
        var command = new VerifyEmailCommand
        {
            Email = "test@example.com",
            OtpCode = "999999"
        };

        var userInfo = new UserInfo
        {
            Id = 1L,
            ExternalId = "usr_test123",
            Email = command.Email,
            FirstName = "John",
            LastName = "Doe",
            AvatarUrl = null,
            Status = UserStatus.Active,
            EmailConfirmed = false,
            Roles = ["Customer"],
            CreatedAt = DateTimeOffset.UtcNow
        };

        _identityServiceMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _otpServiceMock
            .Setup(x => x.ValidateOtpAsync(
                userInfo.Id,
                command.OtpCode,
                OtpPurpose.EmailVerification,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.User.InvalidOtpCode);

        _identityServiceMock.Verify(
            x => x.ConfirmEmailAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
