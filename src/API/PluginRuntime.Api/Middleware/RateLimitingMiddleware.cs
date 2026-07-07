using System.Text.Json;
using Microsoft.Extensions.Options;
using PluginRuntime.Api.Models;
using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Api.Middleware;

/// <summary>
/// Rate limiting middleware that delegates to IRateLimiter for per-endpoint configurable
/// rate limiting. Returns HTTP 429 with Retry-After header when limit is exceeded.
/// Rate limit key is based on endpoint path + authenticated user ID or client IP.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RateLimitingMiddleware(
        RequestDelegate next,
        IRateLimiter rateLimiter,
        ILogger<RateLimitingMiddleware> logger,
        IOptions<RateLimitOptions> options)
    {
        _next = next;
        _rateLimiter = rateLimiter;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.Request.Path.Value ?? "/";
        var clientIdentifier = GetClientIdentifier(context);
        var key = $"{endpoint}:{clientIdentifier}";

        var result = await _rateLimiter.CheckAsync(
            key,
            _options.MaxRequestsPerWindow,
            _options.Window,
            context.RequestAborted);

        if (!result.IsAllowed)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {Key}. Retry after {RetryAfter}s",
                key,
                (int)result.RetryAfter.TotalSeconds);

            var retryAfterSeconds = Math.Max(1, (int)result.RetryAfter.TotalSeconds);
            context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";

            var errorResponse = new ErrorResponse(
                new ErrorDetail(
                    "RATE_LIMIT_EXCEEDED",
                    "ResourceLimit",
                    $"Rate limit exceeded. Retry after {retryAfterSeconds} seconds.",
                    context.TraceIdentifier,
                    DateTime.UtcNow.ToString("O")));

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(errorResponse, JsonOptions));
            return;
        }

        await _next(context);
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        // Use authenticated user ID if available, otherwise fall back to IP address
        var userId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("sub")?.Value ?? context.User.Identity.Name
            : null;

        if (!string.IsNullOrEmpty(userId))
        {
            return userId;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Configuration options for rate limiting per endpoint.
/// </summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Maximum number of requests allowed per window. Default: 100.
    /// </summary>
    public int MaxRequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// Time window for rate limiting. Default: 1 minute.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
