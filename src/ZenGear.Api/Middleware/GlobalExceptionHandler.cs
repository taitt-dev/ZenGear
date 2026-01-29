using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Models;

namespace ZenGear.Api.Middleware;

/// <summary>
/// Global exception handler for API.
/// Catches unhandled exceptions and returns standardized error response.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        _logger.LogError(
            exception,
            "Unhandled exception occurred. Path: {Path}, Method: {Method}",
            httpContext.Request.Path,
            httpContext.Request.Method);

        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                ApiResponse.Failure(
                    validationEx.Errors.Select(e => e.ErrorMessage).ToArray(),
                    ErrorCodes.ValidationError)),

            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                ApiResponse.Failure("Access denied.", ErrorCodes.Forbidden)),

            InvalidOperationException invalidOpEx => (
                HttpStatusCode.BadRequest,
                ApiResponse.Failure(invalidOpEx.Message, ErrorCodes.ValidationError)),

            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                ApiResponse.Failure(argEx.Message, ErrorCodes.ValidationError)),

            _ => (
                HttpStatusCode.InternalServerError,
                ApiResponse.Failure(
                    "An error occurred while processing your request.",
                    ErrorCodes.InternalError))
        };

        httpContext.Response.StatusCode = (int)statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, ct);

        return true;
    }
}
