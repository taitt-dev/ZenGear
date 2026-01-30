using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ZenGear.Domain.Repositories;
using ZenGear.Infrastructure.Persistence;
using ZenGear.Infrastructure.Persistence.Repositories;
using ZenGear.Infrastructure.Services;

namespace ZenGear.Infrastructure.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for RefreshTokenRepository.
/// Uses in-memory database for fast tests.
/// </summary>
public class RefreshTokenRepositoryTests : IAsyncDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IRefreshTokenRepository _repository;
    private readonly DateTimeService _dateTime;

    public RefreshTokenRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dateTime = new DateTimeService();

        _context = new ApplicationDbContext(
            options,
            new NoOpMediator(),
            _dateTime);

        _repository = new RefreshTokenRepository(_context, _dateTime);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateToken()
    {
        // Arrange
        var userId = 1L;
        var token = "test-refresh-token";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        var result = await _repository.CreateAsync(userId, token, expiresAt);

        // Assert
        result.Should().Be(token);

        var savedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        savedToken.Should().NotBeNull();
        savedToken!.UserId.Should().Be(userId);
        savedToken.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetByTokenAsync_WithValidToken_ShouldReturnToken()
    {
        // Arrange
        var userId = 1L;
        var token = "test-refresh-token";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        await _repository.CreateAsync(userId, token, expiresAt);

        // Act
        var result = await _repository.GetByTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be(token);
        result.UserId.Should().Be(userId);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "non-existent-token";

        // Act
        var result = await _repository.GetByTokenAsync(invalidToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAsync_WithValidToken_ShouldRevokeToken()
    {
        // Arrange
        var userId = 1L;
        var token = "test-refresh-token";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var replacedBy = "new-token";

        await _repository.CreateAsync(userId, token, expiresAt);

        // Act
        await _repository.RevokeAsync(token, replacedBy);

        // Assert
        var result = await _repository.GetByTokenAsync(token);

        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
        result.RevokedAt.Should().NotBeNull();
        result.ReplacedByToken.Should().Be(replacedBy);
    }

    [Fact]
    public async Task RevokeAsync_WithNonExistentToken_ShouldNotThrow()
    {
        // Arrange
        var invalidToken = "non-existent-token";

        // Act
        var act = async () => await _repository.RevokeAsync(invalidToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeAllForUserAsync_ShouldRevokeAllActiveTokens()
    {
        // Arrange
        var userId = 1L;
        var token1 = "token-1";
        var token2 = "token-2";
        var token3 = "token-3";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        await _repository.CreateAsync(userId, token1, expiresAt);
        await _repository.CreateAsync(userId, token2, expiresAt);
        await _repository.CreateAsync(userId, token3, expiresAt);

        // Revoke one token manually
        await _repository.RevokeAsync(token2);

        // Act - Revoke all
        await _repository.RevokeAllForUserAsync(userId);

        // Assert
        var token1Info = await _repository.GetByTokenAsync(token1);
        var token2Info = await _repository.GetByTokenAsync(token2);
        var token3Info = await _repository.GetByTokenAsync(token3);

        token1Info!.IsActive.Should().BeFalse();
        token2Info!.IsActive.Should().BeFalse(); // Already revoked
        token3Info!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetByTokenAsync_WithExpiredToken_ShouldReturnInactive()
    {
        // Arrange
        var userId = 1L;
        var token = "expired-token";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(-1); // Expired 1 min ago

        await _repository.CreateAsync(userId, token, expiresAt);

        // Act
        var result = await _repository.GetByTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse(); // Expired
        result.RevokedAt.Should().BeNull(); // Not manually revoked
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}

/// <summary>
/// No-op mediator for testing.
/// </summary>
internal class NoOpMediator : MediatR.IMediator
{
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        MediatR.IStreamRequest<TResponse> request,
        CancellationToken ct = default)
    {
        return AsyncEnumerable.Empty<TResponse>();
    }

    public IAsyncEnumerable<object?> CreateStream(
        object request,
        CancellationToken ct = default)
    {
        return AsyncEnumerable.Empty<object?>();
    }

    public Task Publish(object notification, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(
        TNotification notification,
        CancellationToken ct = default) where TNotification : MediatR.INotification
    {
        return Task.CompletedTask;
    }

    public Task<TResponse> Send<TResponse>(
        MediatR.IRequest<TResponse> request,
        CancellationToken ct = default)
    {
        return Task.FromResult<TResponse>(default!);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken ct = default)
        where TRequest : MediatR.IRequest
    {
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken ct = default)
    {
        return Task.FromResult<object?>(null);
    }
}
