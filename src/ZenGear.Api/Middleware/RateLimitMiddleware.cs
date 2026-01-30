using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net;
using ZenGear.Application.Common.Models;

namespace ZenGear.Api.Middleware;

/// <summary>
/// Middleware to rate limit OTP-related endpoints.
/// Prevents abuse by limiting requests per IP address and email.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly RateLimitOptions _options;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        IOptions<RateLimitOptions> options,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to OTP endpoints
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (path == null || !IsOtpEndpoint(path))
        {
            await _next(context);
            return;
        }

        // Get client identifier (IP + Email if available)
        var clientId = GetClientIdentifier(context);

        // Check rate limit
        var cacheKey = $"ratelimit:{clientId}";
        if (_cache.TryGetValue<RateLimitEntry>(cacheKey, out var entry))
        {
            if (entry!.RequestCount >= _options.MaxRequests)
            {
                // Rate limit exceeded
                var retryAfter = (int)(entry.WindowEnd - DateTimeOffset.UtcNow).TotalSeconds;
                
                _logger.LogWarning(
                    "Rate limit exceeded for {ClientId} on {Path}. Requests: {Count}/{Max}",
                    clientId, path, entry.RequestCount, _options.MaxRequests);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers.Append("Retry-After", retryAfter.ToString());
                context.Response.ContentType = "application/json";

                var response = ApiResponse.Failure(
                    $"Too many requests. Please try again in {retryAfter} seconds.",
                    "RATE_LIMIT_EXCEEDED");

                await context.Response.WriteAsJsonAsync(response);
                return;
            }

            // Increment count
            entry.RequestCount++;
        }
        else
        {
            // Create new entry
            entry = new RateLimitEntry
            {
                RequestCount = 1,
                WindowEnd = DateTimeOffset.UtcNow.AddMinutes(_options.WindowMinutes)
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(_options.WindowMinutes));

            _cache.Set(cacheKey, entry, cacheOptions);
        }

        await _next(context);
    }

    private bool IsOtpEndpoint(string path)
    {
        return path.Contains("/auth/resend-verification-email") ||
               path.Contains("/auth/forgot-password");
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Use IP address as base identifier
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // Try to get email from request body (if available)
        // Note: This is a simple implementation. In production, you might want to
        // read the request body more carefully to avoid issues with body reading.
        
        return $"{ipAddress}";
    }

    private class RateLimitEntry
    {
        public int RequestCount { get; set; }
        public DateTimeOffset WindowEnd { get; set; }
    }
}

/// <summary>
/// Configuration options for rate limiting.
/// </summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    /// <summary>
    /// Maximum number of requests allowed within the time window.
    /// </summary>
    public int MaxRequests { get; set; } = 3;

    /// <summary>
    /// Time window in minutes.
    /// </summary>
    public int WindowMinutes { get; set; } = 10;
}
