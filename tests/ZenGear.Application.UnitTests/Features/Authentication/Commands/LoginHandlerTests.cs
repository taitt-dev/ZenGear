using FluentAssertions;
using Moq;
using Xunit;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Features.Authentication.Commands.Login;
using ZenGear.Domain.Enums;
using ZenGear.Domain.Repositories;

namespace ZenGear.Application.UnitTests.Features.Authentication.Commands;

/// <summary>
/// Unit tests for LoginHandler.
/// </summary>
public class LoginHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();

        _handler = new LoginHandler(
            _identityServiceMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "SecurePass123!"
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
            EmailConfirmed = true,
            Roles = ["Customer"],
            CreatedAt = DateTimeOffset.UtcNow
        };

        var accessToken = "access-token";
        var refreshToken = "refresh-token";
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        _identityServiceMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _identityServiceMock
            .Setup(x => x.CheckPasswordAsync(command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(
                userInfo.Id,
                userInfo.ExternalId,
                userInfo.Email,
                userInfo.FullName,
                userInfo.Roles))
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

        _refreshTokenRepositoryMock
            .Setup(x => x.CreateAsync(
                userInfo.Id,
                refreshToken,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

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
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "nonexistent@example.com",
            Password = "SecurePass123!"
        };

        _identityServiceMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfo?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.User.InvalidCredentials);
        result.Errors.Should().Contain("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_WithUnverifiedEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "SecurePass123!"
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
            EmailConfirmed = false,  // Not verified
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
        result.ErrorCode.Should().Be(ErrorCodes.User.EmailNotVerified);
        result.Errors.Should().Contain("Email not verified. Please verify your email first.");
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ShouldIncrementFailedCount()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "WrongPassword!"
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
            EmailConfirmed = true,
            Roles = ["Customer"],
            CreatedAt = DateTimeOffset.UtcNow
        };

        _identityServiceMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _identityServiceMock
            .Setup(x => x.CheckPasswordAsync(command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _identityServiceMock
            .Setup(x => x.IncrementAccessFailedCountAsync(userInfo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);  // Not locked out yet

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.User.InvalidCredentials);

        _identityServiceMock.Verify(
            x => x.IncrementAccessFailedCountAsync(userInfo.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAccountLockedOut_ShouldReturnLockedError()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "WrongPassword!"
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
            EmailConfirmed = true,
            Roles = ["Customer"],
            CreatedAt = DateTimeOffset.UtcNow
        };

        _identityServiceMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _identityServiceMock
            .Setup(x => x.CheckPasswordAsync(command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _identityServiceMock
            .Setup(x => x.IncrementAccessFailedCountAsync(userInfo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);  // Account locked

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.User.AccountLocked);
        result.Errors.Should().Contain("Account locked due to too many failed login attempts.");
    }

    [Fact]
    public async Task Handle_WhenAccountIsLockedOut_ShouldCheckLockoutBeforePassword()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "CorrectPassword123!"
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
            EmailConfirmed = true,
            Roles = ["Customer"],
            CreatedAt = DateTimeOffset.UtcNow
        };

        var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10);

        _identityServiceMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _identityServiceMock
            .Setup(x => x.IsLockedOutAsync(userInfo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _identityServiceMock
            .Setup(x => x.GetLockoutEndAsync(userInfo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockoutEnd);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.User.AccountLocked);
        result.Errors.Should().ContainMatch("*locked*");

        // Verify password was NOT checked
        _identityServiceMock.Verify(
            x => x.CheckPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithSuccessfulLogin_ShouldResetFailedCount()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "SecurePass123!"
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
            EmailConfirmed = true,
            Roles = ["Customer"],
            CreatedAt = DateTimeOffset.UtcNow
        };

        _identityServiceMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _identityServiceMock
            .Setup(x => x.IsLockedOutAsync(userInfo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _identityServiceMock
            .Setup(x => x.CheckPasswordAsync(command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string[]>()))
            .Returns("access-token");

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        _tokenServiceMock
            .Setup(x => x.GetAccessTokenExpiration())
            .Returns(DateTimeOffset.UtcNow.AddHours(1));

        _tokenServiceMock
            .Setup(x => x.GetRefreshTokenExpiration())
            .Returns(DateTimeOffset.UtcNow.AddDays(7));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        // Verify failed count was reset
        _identityServiceMock.Verify(
            x => x.ResetAccessFailedCountAsync(userInfo.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

