using MediatR;
using Microsoft.Extensions.Logging;

namespace ZenGear.Application.Common.Behaviours;

/// <summary>
/// Catch-all exception handler for unhandled exceptions in the pipeline.
/// </summary>
public class UnhandledExceptionBehaviour<TRequest, TResponse>(
    ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;

            logger.LogError(
                ex,
                "Unhandled Exception for Request {RequestName}: {@Request}",
                requestName,
                request);

            throw;
        }
    }
}
