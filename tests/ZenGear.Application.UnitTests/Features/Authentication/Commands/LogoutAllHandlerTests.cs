using FluentAssertions;
using Moq;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Features.Authentication.Commands.LogoutAll;
using ZenGear.Domain.Repositories;

namespace ZenGear.Application.UnitTests.Features.Authentication.Commands;

/// <summary>
/// Unit tests for LogoutAllHandler.
/// </summary>
public class LogoutAllHandlerTests
{
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly LogoutAllHandler _handler;

    public LogoutAllHandlerTests()
    {
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        _mockIdentityService = new Mock<IIdentityService>();

        _handler = new LogoutAllHandler(
            _mockCurrentUser.Object,
            _mockRefreshTokenRepo.Object,
            _mockIdentityService.Object);
    }

    [Fact]
    public async Task Handle_WithAuthenticatedUser_ShouldRevokeAllTokensAndUpdateSecurityStamp()
    {
        // Arrange
        var userId = 123L;
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var command = new LogoutAllCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _mockRefreshTokenRepo.Verify(
            x => x.RevokeAllForUserAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockIdentityService.Verify(
            x => x.UpdateSecurityStampAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnauthenticatedUser_ShouldReturnFailure()
    {
        // Arrange
        _mockCurrentUser.Setup(x => x.UserId).Returns(0L); // Not authenticated

        var command = new LogoutAllCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not authenticated.");
        result.ErrorCode.Should().Be("UNAUTHORIZED");
        _mockRefreshTokenRepo.Verify(
            x => x.RevokeAllForUserAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
