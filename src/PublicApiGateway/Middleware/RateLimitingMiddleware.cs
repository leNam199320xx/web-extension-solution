using PublicApiGateway.Models;
using PublicApiGateway.Services;

namespace PublicApiGateway.Middleware;

/// <summary>
/// Enforces per-tenant rate limits using sliding window.
/// Adds X-RateLimit-Limit, X-RateLimit-Remaining, X-RateLimit-Reset headers.
/// </summary>
public sealed class RateLimitingMiddleware : IMiddleware
{
    private readonly IRateLimitService _rateLimitService;

    public RateLimitingMiddleware(IRateLimitService rateLimitService)
    {
        _rateLimitService = rateLimitService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var tenantContext = context.Items["TenantContext"] as TenantContext;
        if (tenantContext is null)
        {
            await next(context);
            return;
        }

        var result = await _rateLimitService.CheckAsync(
            tenantContext.TenantId.ToString(),
            tenantContext.Limits,
            context.RequestAborted);

        // Always add rate limit headers (even for unlimited plans where limit=0)
        if (result.Limit > 0)
        {
            context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = result.ResetAtUnixSeconds.ToString();
        }

        if (!result.IsAllowed)
        {
            context.Response.StatusCode = 429;
            context.Response.ContentType = "application/json";

            var error = new GatewayError(
                Code: ErrorCodes.RateLimitExceeded,
                Message: "Rate limit exceeded",
                TraceId: context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier,
                Timestamp: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            await context.Response.WriteAsJsonAsync(error);
            return;
        }

        await next(context);
    }
}
