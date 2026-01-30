using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Domain.Constants;
using ZenGear.Domain.Enums;
using ZenGear.Infrastructure.Identity;
using ZenGear.Infrastructure.Persistence;
using ZenGear.Infrastructure.Services;

namespace ZenGear.Infrastructure.UnitTests.Services;

/// <summary>
/// Unit tests for IdentityService.
/// Tests user registration, login, password management, and role operations.
/// </summary>
public class IdentityServiceTests : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly ApplicationDbContext _context;
    private readonly Mock<IDateTime> _dateTimeMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly IdentityService _identityService;
    private readonly DateTimeOffset _fixedNow = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    public IdentityServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        // Mock IMediator
        _mediatorMock = new Mock<IMediator>();

        // Mock IDateTime
        _dateTimeMock = new Mock<IDateTime>();
        _dateTimeMock.Setup(x => x.UtcNow).Returns(_fixedNow);

        _context = new ApplicationDbContext(options, _mediatorMock.Object, _dateTimeMock.Object);

        // Mock UserManager
        var userStoreMock = Mock.Of<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock, null!, null!, null!, null!, null!, null!, null!, null!);

        // Mock SignInManager
        var contextAccessorMock = Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var userPrincipalFactoryMock = Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            contextAccessorMock,
            userPrincipalFactoryMock,
            null!, null!, null!, null!);

        _identityService = new IdentityService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _context,
            _dateTimeMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var externalId = "usr_V1StGXR8Z5jdHi6B";
        var email = "customer@example.com";
        var password = "Password@123";
        var firstName = "Nguyen";
        var lastName = "Van A";

        ApplicationUser? capturedUser = null;
        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .Callback<ApplicationUser, string>((user, _) => 
            {
                user.Id = 1; // Simulate database-generated ID
                capturedUser = user;
            })
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Customer))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var (succeeded, userId, errors) = await _identityService.CreateUserAsync(
            externalId, email, password, firstName, lastName);

        // Assert
        succeeded.Should().BeTrue();
        userId.Should().Be(1);
        errors.Should().BeEmpty();

        capturedUser.Should().NotBeNull();
        capturedUser!.ExternalId.Should().Be(externalId);
        capturedUser.Email.Should().Be(email);
        capturedUser.UserName.Should().Be(email);
        capturedUser.FirstName.Should().Be(firstName);
        capturedUser.LastName.Should().Be(lastName);
        capturedUser.Status.Should().Be(UserStatus.Active);
        capturedUser.EmailConfirmed.Should().BeFalse();
        capturedUser.CreatedAt.Should().Be(_fixedNow);

        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Customer), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WhenPasswordTooWeak_ShouldReturnErrors()
    {
        // Arrange
        var externalId = "usr_V1StGXR8Z5jdHi6B";
        var email = "customer@example.com";
        var password = "weak";
        var firstName = "Nguyen";
        var lastName = "Van A";

        var identityErrors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Password must be at least 8 characters." },
            new IdentityError { Code = "PasswordRequiresUpper", Description = "Password must contain uppercase letter." }
        };

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var (succeeded, userId, errors) = await _identityService.CreateUserAsync(
            externalId, email, password, firstName, lastName);

        // Assert
        succeeded.Should().BeFalse();
        userId.Should().Be(0);
        errors.Should().HaveCount(2);
        errors.Should().Contain("Password must be at least 8 characters.");
        errors.Should().Contain("Password must contain uppercase letter.");

        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region CreateUserFromGoogleAsync Tests

    [Fact]
    public async Task CreateUserFromGoogleAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var externalId = "usr_nY5WL3K9Xq2mR7hT";
        var email = "google.user@gmail.com";
        var firstName = "John";
        var lastName = "Doe";
        var avatarUrl = "https://lh3.googleusercontent.com/avatar.jpg";

        ApplicationUser? capturedUser = null;
        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(user => 
            {
                user.Id = 2;
                capturedUser = user;
            })
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Customer))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var (succeeded, userId, errors) = await _identityService.CreateUserFromGoogleAsync(
            externalId, email, firstName, lastName, avatarUrl);

        // Assert
        succeeded.Should().BeTrue();
        userId.Should().Be(2);
        errors.Should().BeEmpty();

        capturedUser.Should().NotBeNull();
        capturedUser!.ExternalId.Should().Be(externalId);
        capturedUser.Email.Should().Be(email);
        capturedUser.UserName.Should().Be(email);
        capturedUser.FirstName.Should().Be(firstName);
        capturedUser.LastName.Should().Be(lastName);
        capturedUser.AvatarUrl.Should().Be(avatarUrl);
        capturedUser.Status.Should().Be(UserStatus.Active);
        capturedUser.EmailConfirmed.Should().BeTrue(); // Google emails are pre-verified
        capturedUser.CreatedAt.Should().Be(_fixedNow);

        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Customer), Times.Once);
    }

    [Fact]
    public async Task CreateUserFromGoogleAsync_WithoutAvatar_ShouldSucceed()
    {
        // Arrange
        var externalId = "usr_nY5WL3K9Xq2mR7hT";
        var email = "google.user@gmail.com";
        var firstName = "John";
        var lastName = "Doe";

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(user => user.Id = 2)
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Customer))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var (succeeded, userId, errors) = await _identityService.CreateUserFromGoogleAsync(
            externalId, email, firstName, lastName, null);

        // Assert
        succeeded.Should().BeTrue();
        userId.Should().Be(2);
        errors.Should().BeEmpty();
    }

    #endregion

    #region CheckPasswordAsync Tests

    [Fact]
    public async Task CheckPasswordAsync_WithValidCredentials_ShouldReturnTrue()
    {
        // Arrange
        var email = "customer@example.com";
        var password = "Password@123";
        var user = CreateTestUser(email);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, password))
            .ReturnsAsync(true);

        // Act
        var result = await _identityService.CheckPasswordAsync(email, password);

        // Assert
        result.Should().BeTrue();
        _userManagerMock.Verify(x => x.FindByEmailAsync(email), Times.Once);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(user, password), Times.Once);
    }

    [Fact]
    public async Task CheckPasswordAsync_WithInvalidPassword_ShouldReturnFalse()
    {
        // Arrange
        var email = "customer@example.com";
        var password = "WrongPassword@123";
        var user = CreateTestUser(email);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, password))
            .ReturnsAsync(false);

        // Act
        var result = await _identityService.CheckPasswordAsync(email, password);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPasswordAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "Password@123";

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _identityService.CheckPasswordAsync(email, password);

        // Assert
        result.Should().BeFalse();
        _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_WithExistingUser_ShouldReturnUserInfo()
    {
        // Arrange
        var email = "customer@example.com";
        var user = CreateTestUser(email);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var roles = new List<string> { Roles.Customer };
        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _identityService.GetByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.ExternalId.Should().Be(user.ExternalId);
        result.Email.Should().Be(email);
        result.FirstName.Should().Be(user.FirstName);
        result.LastName.Should().Be(user.LastName);
        result.Status.Should().Be(user.Status);
        result.EmailConfirmed.Should().Be(user.EmailConfirmed);
        result.Roles.Should().BeEquivalentTo(roles);
        result.CreatedAt.Should().Be(user.CreatedAt);
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var email = "nonexistent@example.com";

        // Act
        var result = await _identityService.GetByEmailAsync(email);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByExternalIdAsync Tests

    [Fact]
    public async Task GetByExternalIdAsync_WithExistingUser_ShouldReturnUserInfo()
    {
        // Arrange
        var externalId = "usr_V1StGXR8Z5jdHi6B";
        var user = CreateTestUser("customer@example.com", externalId);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var roles = new List<string> { Roles.Customer };
        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _identityService.GetByExternalIdAsync(externalId);

        // Assert
        result.Should().NotBeNull();
        result!.ExternalId.Should().Be(externalId);
        result.Email.Should().Be(user.Email);
        result.Roles.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public async Task GetByExternalIdAsync_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var externalId = "usr_NonExistent123";

        // Act
        var result = await _identityService.GetByExternalIdAsync(externalId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ShouldReturnUserInfo()
    {
        // Arrange
        var userId = 1L;
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var roles = new List<string> { Roles.Customer };
        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _identityService.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Roles.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var userId = 999L;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _identityService.GetByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region EmailExistsAsync Tests

    [Fact]
    public async Task EmailExistsAsync_WithExistingEmail_ShouldReturnTrue()
    {
        // Arrange
        var email = "customer@example.com";
        var user = CreateTestUser(email);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _identityService.EmailExistsAsync(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WithNonExistentEmail_ShouldReturnFalse()
    {
        // Arrange
        var email = "nonexistent@example.com";

        // Act
        var result = await _identityService.EmailExistsAsync(email);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var email = "Customer@Example.COM";
        var user = CreateTestUser("customer@example.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _identityService.EmailExistsAsync(email);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ConfirmEmailAsync Tests

    [Fact]
    public async Task ConfirmEmailAsync_WithValidUser_ShouldSucceed()
    {
        // Arrange
        var userId = 1L;
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;
        user.EmailConfirmed = false;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _identityService.ConfirmEmailAsync(userId);

        // Assert
        result.Should().BeTrue();
        user.EmailConfirmed.Should().BeTrue();
        user.UpdatedAt.Should().Be(_fixedNow);

        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999L;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _identityService.ConfirmEmailAsync(userId);

        // Assert
        result.Should().BeFalse();
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        var userId = 1L;
        var currentPassword = "OldPassword@123";
        var newPassword = "NewPassword@123";
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.ChangePasswordAsync(user, currentPassword, newPassword))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var (succeeded, errors) = await _identityService.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        succeeded.Should().BeTrue();
        errors.Should().BeEmpty();
        user.UpdatedAt.Should().Be(_fixedNow);

        _userManagerMock.Verify(x => x.ChangePasswordAsync(user, currentPassword, newPassword), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ShouldFail()
    {
        // Arrange
        var userId = 1L;
        var currentPassword = "WrongPassword@123";
        var newPassword = "NewPassword@123";
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var identityErrors = new[]
        {
            new IdentityError { Code = "PasswordMismatch", Description = "Incorrect password." }
        };

        _userManagerMock
            .Setup(x => x.ChangePasswordAsync(user, currentPassword, newPassword))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var (succeeded, errors) = await _identityService.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        succeeded.Should().BeFalse();
        errors.Should().Contain("Incorrect password.");
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonExistentUser_ShouldFail()
    {
        // Arrange
        var userId = 999L;
        var currentPassword = "OldPassword@123";
        var newPassword = "NewPassword@123";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var (succeeded, errors) = await _identityService.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        succeeded.Should().BeFalse();
        errors.Should().Contain("User not found.");
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_WithValidUser_ShouldSucceed()
    {
        // Arrange
        var userId = 1L;
        var newPassword = "NewPassword@123";
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.RemovePasswordAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddPasswordAsync(user, newPassword))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var (succeeded, errors) = await _identityService.ResetPasswordAsync(userId, newPassword);

        // Assert
        succeeded.Should().BeTrue();
        errors.Should().BeEmpty();
        user.UpdatedAt.Should().Be(_fixedNow);

        _userManagerMock.Verify(x => x.RemovePasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.AddPasswordAsync(user, newPassword), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithWeakPassword_ShouldFail()
    {
        // Arrange
        var userId = 1L;
        var newPassword = "weak";
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.RemovePasswordAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var identityErrors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Password must be at least 8 characters." }
        };

        _userManagerMock
            .Setup(x => x.AddPasswordAsync(user, newPassword))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var (succeeded, errors) = await _identityService.ResetPasswordAsync(userId, newPassword);

        // Assert
        succeeded.Should().BeFalse();
        errors.Should().Contain("Password must be at least 8 characters.");
    }

    [Fact]
    public async Task ResetPasswordAsync_WithNonExistentUser_ShouldFail()
    {
        // Arrange
        var userId = 999L;
        var newPassword = "NewPassword@123";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var (succeeded, errors) = await _identityService.ResetPasswordAsync(userId, newPassword);

        // Assert
        succeeded.Should().BeFalse();
        errors.Should().Contain("User not found.");
    }

    #endregion

    #region IncrementAccessFailedCountAsync Tests

    [Fact]
    public async Task IncrementAccessFailedCountAsync_ShouldIncrementCount()
    {
        // Arrange
        var userId = 1L;
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        // Act
        var isLockedOut = await _identityService.IncrementAccessFailedCountAsync(userId);

        // Assert
        isLockedOut.Should().BeFalse();
        _userManagerMock.Verify(x => x.AccessFailedAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.IsLockedOutAsync(user), Times.Once);
    }

    [Fact]
    public async Task IncrementAccessFailedCountAsync_WhenReachesLimit_ShouldLockOut()
    {
        // Arrange
        var userId = 1L;
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(true); // Locked out after increment

        // Act
        var isLockedOut = await _identityService.IncrementAccessFailedCountAsync(userId);

        // Assert
        isLockedOut.Should().BeTrue();
    }

    [Fact]
    public async Task IncrementAccessFailedCountAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999L;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var isLockedOut = await _identityService.IncrementAccessFailedCountAsync(userId);

        // Assert
        isLockedOut.Should().BeFalse();
        _userManagerMock.Verify(x => x.AccessFailedAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

    #region ResetAccessFailedCountAsync Tests

    [Fact]
    public async Task ResetAccessFailedCountAsync_WithValidUser_ShouldReset()
    {
        // Arrange
        var userId = 1L;
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _identityService.ResetAccessFailedCountAsync(userId);

        // Assert
        _userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task ResetAccessFailedCountAsync_WithNonExistentUser_ShouldNotThrow()
    {
        // Arrange
        var userId = 999L;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        await _identityService.ResetAccessFailedCountAsync(userId);

        // Assert
        _userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

    #region UpdateSecurityStampAsync Tests

    [Fact]
    public async Task UpdateSecurityStampAsync_WithValidUser_ShouldUpdate()
    {
        // Arrange
        var userId = 1L;
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.UpdateSecurityStampAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _identityService.UpdateSecurityStampAsync(userId);

        // Assert
        _userManagerMock.Verify(x => x.UpdateSecurityStampAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateSecurityStampAsync_WithNonExistentUser_ShouldNotThrow()
    {
        // Arrange
        var userId = 999L;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        await _identityService.UpdateSecurityStampAsync(userId);

        // Assert
        _userManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

    #region GetRolesAsync Tests

    [Fact]
    public async Task GetRolesAsync_WithValidUser_ShouldReturnRoles()
    {
        // Arrange
        var userId = 1L;
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        var roles = new List<string> { Roles.Customer, Roles.Admin };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _identityService.GetRolesAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public async Task GetRolesAsync_WithNonExistentUser_ShouldReturnEmpty()
    {
        // Arrange
        var userId = 999L;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _identityService.GetRolesAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region AddToRoleAsync Tests

    [Fact]
    public async Task AddToRoleAsync_WithValidUser_ShouldSucceed()
    {
        // Arrange
        var userId = 1L;
        var role = Roles.Admin;
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(user, role))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _identityService.AddToRoleAsync(userId, role);

        // Assert
        result.Should().BeTrue();
        _userManagerMock.Verify(x => x.AddToRoleAsync(user, role), Times.Once);
    }

    [Fact]
    public async Task AddToRoleAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999L;
        var role = Roles.Admin;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _identityService.AddToRoleAsync(userId, role);

        // Assert
        result.Should().BeFalse();
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region RemoveFromRoleAsync Tests

    [Fact]
    public async Task RemoveFromRoleAsync_WithValidUser_ShouldSucceed()
    {
        // Arrange
        var userId = 1L;
        var role = Roles.Customer;
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.RemoveFromRoleAsync(user, role))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _identityService.RemoveFromRoleAsync(userId, role);

        // Assert
        result.Should().BeTrue();
        _userManagerMock.Verify(x => x.RemoveFromRoleAsync(user, role), Times.Once);
    }

    [Fact]
    public async Task RemoveFromRoleAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999L;
        var role = Roles.Customer;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _identityService.RemoveFromRoleAsync(userId, role);

        // Assert
        result.Should().BeFalse();
        _userManagerMock.Verify(x => x.RemoveFromRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region IsLockedOutAsync Tests

    [Fact]
    public async Task IsLockedOutAsync_WhenLockedOut_ShouldReturnTrue()
    {
        // Arrange
        var userId = 1L;
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(true);

        // Act
        var result = await _identityService.IsLockedOutAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsLockedOutAsync_WhenNotLockedOut_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1L;
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        // Act
        var result = await _identityService.IsLockedOutAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsLockedOutAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999L;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _identityService.IsLockedOutAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetLockoutEndAsync Tests

    [Fact]
    public async Task GetLockoutEndAsync_WhenLockedOut_ShouldReturnLockoutEnd()
    {
        // Arrange
        var userId = 1L;
        var lockoutEnd = _fixedNow.AddMinutes(15);
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetLockoutEndDateAsync(user))
            .ReturnsAsync(lockoutEnd);

        // Act
        var result = await _identityService.GetLockoutEndAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(lockoutEnd);
    }

    [Fact]
    public async Task GetLockoutEndAsync_WhenNotLockedOut_ShouldReturnNull()
    {
        // Arrange
        var userId = 1L;
        var user = CreateTestUser("customer@example.com");
        user.Id = userId;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetLockoutEndDateAsync(user))
            .ReturnsAsync((DateTimeOffset?)null);

        // Act
        var result = await _identityService.GetLockoutEndAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLockoutEndAsync_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var userId = 999L;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _identityService.GetLockoutEndAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private ApplicationUser CreateTestUser(string email, string? externalId = null)
    {
        return new ApplicationUser
        {
            ExternalId = externalId ?? "usr_V1StGXR8Z5jdHi6B",
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active,
            EmailConfirmed = true,
            CreatedAt = _fixedNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }

    #endregion
}
