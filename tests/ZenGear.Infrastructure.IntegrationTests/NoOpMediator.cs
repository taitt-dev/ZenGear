using MediatR;

namespace ZenGear.Infrastructure.IntegrationTests;

/// <summary>
/// No-operation mediator for integration tests.
/// Does not publish domain events.
/// </summary>
internal class NoOpMediator : IMediator
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("NoOpMediator does not support Send operations in tests");
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        throw new NotImplementedException("NoOpMediator does not support Send operations in tests");
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("NoOpMediator does not support streaming in tests");
    }

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        // Silently ignore domain events in tests
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        // Silently ignore domain events in tests
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("NoOpMediator does not support streaming in tests");
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("NoOpMediator does not support Send operations in tests");
    }
}
