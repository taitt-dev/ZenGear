using MediatR;
using Microsoft.Extensions.Logging;

namespace ZenGear.Application.Common.Behaviours;

/// <summary>
/// Logging pipeline behavior.
/// Logs request name and execution.
/// </summary>
public class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation(
            "Handling {RequestName}: {@Request}",
            requestName,
            request);

        var response = await next();

        logger.LogInformation(
            "Handled {RequestName}",
            requestName);

        return response;
    }
}
