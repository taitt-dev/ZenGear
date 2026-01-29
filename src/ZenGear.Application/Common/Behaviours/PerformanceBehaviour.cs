using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ZenGear.Application.Common.Behaviours;

/// <summary>
/// Performance monitoring pipeline behavior.
/// Logs warning if request takes longer than 500ms.
/// </summary>
public class PerformanceBehaviour<TRequest, TResponse>(
    ILogger<PerformanceBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int ThresholdMilliseconds = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        if (elapsedMilliseconds > ThresholdMilliseconds)
        {
            var requestName = typeof(TRequest).Name;

            logger.LogWarning(
                "Long Running Request: {RequestName} ({ElapsedMilliseconds} ms) {@Request}",
                requestName,
                elapsedMilliseconds,
                request);
        }

        return response;
    }
}
