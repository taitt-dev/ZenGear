using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Infrastructure.Services;

namespace ZenGear.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for ApplicationDbContext.
/// Used by EF Core migrations and tooling.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use connection string for migrations
        // This can be overridden by passing --connection argument
        var connectionString = args.Length > 0
            ? args[0]
            : "Host=localhost;Port=5432;Database=zengear;Username=postgres;Password=123456";

        optionsBuilder.UseNpgsql(connectionString);

        // Create mock services for design-time
        var mediator = new NoOpMediator();
        var dateTime = new DateTimeService();

        return new ApplicationDbContext(optionsBuilder.Options, mediator, dateTime);
    }
}

/// <summary>
/// No-op mediator for design-time context creation.
/// </summary>
internal class NoOpMediator : IMediator
{
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
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
        CancellationToken ct = default) where TNotification : INotification
    {
        return Task.CompletedTask;
    }

    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken ct = default)
    {
        return Task.FromResult<TResponse>(default!);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken ct = default)
        where TRequest : IRequest
    {
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken ct = default)
    {
        return Task.FromResult<object?>(null);
    }
}
