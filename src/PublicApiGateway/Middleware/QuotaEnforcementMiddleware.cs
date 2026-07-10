using PublicApiGateway.Models;
using PublicApiGateway.Services;

namespace PublicApiGateway.Middleware;

/// <summary>
/// Enforces daily quota per tenant.
/// Returns 429 with Retry-After header when quota is exceeded.
/// </summary>
public sealed class QuotaEnforcementMiddleware : IMiddleware
{
    private readonly IQuotaService _quotaService;

    public QuotaEnforcementMiddleware(IQuotaService quotaService)
    {
        _quotaService = quotaService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var tenantContext = context.Items["TenantContext"] as TenantContext;
        if (tenantContext is null)
        {
            await next(context);
            return;
        }

        var result = await _quotaService.IncrementAndCheckAsync(
            tenantContext.TenantId.ToString(),
            tenantContext.Limits,
            context.RequestAborted);

        if (!result.IsAllowed)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = result.RetryAfterSeconds.ToString();
            context.Response.ContentType = "application/json";

            var error = new GatewayError(
                Code: ErrorCodes.QuotaExceeded,
                Message: "Daily quota exceeded",
                TraceId: context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier,
                Timestamp: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            await context.Response.WriteAsJsonAsync(error);
            return;
        }

        await next(context);
    }
}
