using FluentAssertions;
using Moq;
using Xunit;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Features.Authentication.Commands.Register;
using ZenGear.Domain.Common;
using ZenGear.Domain.Constants;
using ZenGear.Domain.Enums;

namespace ZenGear.Application.UnitTests.Features.Authentication.Commands;

/// <summary>
/// Unit tests for RegisterHandler.
/// </summary>
public class RegisterHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IExternalIdGenerator> _externalIdGeneratorMock;
    private readonly RegisterHandler _handler;

    public RegisterHandlerTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _otpServiceMock = new Mock<IOtpService>();
        _emailServiceMock = new Mock<IEmailService>();
        _externalIdGeneratorMock = new Mock<IExternalIdGenerator>();

        _handler = new RegisterHandler(
            _identityServiceMock.Object,
            _otpServiceMock.Object,
            _emailServiceMock.Object,
            _externalIdGeneratorMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCreateUserAndSendOtp()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        var externalId = "usr_test123";
        var userId = 1L;
        var otpCode = "123456";

        _externalIdGeneratorMock
            .Setup(x => x.Generate(EntityPrefixes.User))
            .Returns(externalId);

        _identityServiceMock
            .Setup(x => x.CreateUserAsync(
                externalId,
                command.Email,
                command.Password,
                command.FirstName,
                command.LastName,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, userId, Array.Empty<string>()));

        _otpServiceMock
            .Setup(x => x.CreateOtpAsync(
                userId,
                OtpPurpose.EmailVerification,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpCode);

        _emailServiceMock
            .Setup(x => x.SendEmailVerificationAsync(
                command.Email,
                command.FirstName,
                otpCode,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        _identityServiceMock.Verify(
            x => x.CreateUserAsync(
                externalId,
                command.Email,
                command.Password,
                command.FirstName,
                command.LastName,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _otpServiceMock.Verify(
            x => x.CreateOtpAsync(
                userId,
                OtpPurpose.EmailVerification,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailServiceMock.Verify(
            x => x.SendEmailVerificationAsync(
                command.Email,
                command.FirstName,
                otpCode,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserCreationFails_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        var externalId = "usr_test123";
        var errors = new[] { "Email already exists." };

        _externalIdGeneratorMock
            .Setup(x => x.Generate(EntityPrefixes.User))
            .Returns(externalId);

        _identityServiceMock
            .Setup(x => x.CreateUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, 0L, errors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().BeEquivalentTo(errors);
        result.ErrorCode.Should().Be(ErrorCodes.User.RegistrationFailed);

        _otpServiceMock.Verify(
            x => x.CreateOtpAsync(
                It.IsAny<long>(),
                It.IsAny<OtpPurpose>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _emailServiceMock.Verify(
            x => x.SendEmailVerificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailSendFails_ShouldStillSucceed()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        var externalId = "usr_test123";
        var userId = 1L;
        var otpCode = "123456";

        _externalIdGeneratorMock
            .Setup(x => x.Generate(EntityPrefixes.User))
            .Returns(externalId);

        _identityServiceMock
            .Setup(x => x.CreateUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, userId, Array.Empty<string>()));

        _otpServiceMock
            .Setup(x => x.CreateOtpAsync(
                It.IsAny<long>(),
                It.IsAny<OtpPurpose>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpCode);

        _emailServiceMock
            .Setup(x => x.SendEmailVerificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Should still succeed (user can resend OTP later)
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
    }
}
