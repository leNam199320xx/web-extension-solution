using System.Security.Claims;
using PluginRuntime.Api.Shared.Exceptions;
using PluginRuntime.Api.Shared.Infrastructure;

namespace PluginRuntime.Api.Middleware;

/// <summary>
/// Resolves the current tenant from JWT claims and populates ICurrentTenantContext.
/// Enforces role-based access:
///   - Platform_Admin for /api/admin/* endpoints
///   - Tenant owner for self-service endpoints (cross-tenant access rejected with UA-AUTH-001)
/// </summary>
public sealed class TenantContextMiddleware : IMiddleware
{
    private static readonly HashSet<string> SkipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/ready",
        "/metrics",
        "/api/billing/webhooks/stripe"
    };

    private readonly ILogger<TenantContextMiddleware> _logger;

    public TenantContextMiddleware(ILogger<TenantContextMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (ShouldSkip(path))
        {
            await next(context);
            return;
        }

        // Only process if user is authenticated
        if (context.User.Identity is not { IsAuthenticated: true })
        {
            await next(context);
            return;
        }

        var tenantContext = context.RequestServices.GetRequiredService<CurrentTenantContext>();

        // Extract claims
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        var planIdClaim = context.User.FindFirst("plan_id")?.Value;
        var isInternalClaim = context.User.FindFirst("is_internal")?.Value;
        var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value
                     ?? context.User.FindFirst("role")?.Value;

        var isAdmin = string.Equals(roleClaim, "Platform_Admin", StringComparison.OrdinalIgnoreCase);

        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            tenantContext.SetContext(
                tenantId: tenantId,
                planId: Guid.TryParse(planIdClaim, out var planId) ? planId : null,
                isInternal: bool.TryParse(isInternalClaim, out var isInternal) && isInternal,
                isAdmin: isAdmin);
        }
        else if (isAdmin)
        {
            // Admin without tenant context (platform-level admin)
            tenantContext.SetContext(
                tenantId: null,
                planId: null,
                isInternal: false,
                isAdmin: true);
        }

        // Enforce admin-only for /api/admin/* endpoints
        if (path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase) && !isAdmin)
        {
            throw new InternalTenantAuthException();
        }

        // Enforce tenant isolation for tenant-scoped endpoints
        if (!isAdmin && tenantId != Guid.Empty)
        {
            var routeTenantId = ExtractRouteTenantId(context);
            if (routeTenantId.HasValue && routeTenantId.Value != tenantId)
            {
                throw new TenantIsolationException();
            }
        }

        await next(context);
    }

    private static Guid? ExtractRouteTenantId(HttpContext context)
    {
        // Check route values for tenantId parameter
        if (context.Request.RouteValues.TryGetValue("tenantId", out var routeValue)
            && routeValue is string routeStr
            && Guid.TryParse(routeStr, out var routeTenantId))
        {
            return routeTenantId;
        }

        return null;
    }

    private static bool ShouldSkip(string path)
    {
        return SkipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}
