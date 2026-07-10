namespace PluginRuntime.Api.Middleware;

/// <summary>
/// Adds security headers and module identification to all responses.
/// Enforces HTTPS requirement and adds standard security headers.
/// </summary>
public sealed class SecurityHeadersMiddleware : IMiddleware
{
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(ILogger<SecurityHeadersMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Enforce HTTPS in non-development environments
        if (!context.Request.IsHttps
            && !context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "UA-SEC-001",
                    message = "HTTPS required"
                }
            });
            return;
        }

        // Security headers
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "0";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        // Module identification header (set by controllers or resolved from path)
        var moduleName = ResolveModuleFromPath(context.Request.Path.Value);
        if (moduleName is not null)
        {
            context.Response.Headers["X-Module"] = moduleName;
        }

        await next(context);
    }

    private static string? ResolveModuleFromPath(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        return path switch
        {
            _ when path.StartsWith("/api/plugins", StringComparison.OrdinalIgnoreCase) => "Plugins",
            _ when path.StartsWith("/api/tenants", StringComparison.OrdinalIgnoreCase) => "Tenants",
            _ when path.StartsWith("/api/billing", StringComparison.OrdinalIgnoreCase) => "Billing",
            _ when path.StartsWith("/api/subscriptions", StringComparison.OrdinalIgnoreCase) => "Subscriptions",
            _ when path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase) => "Admin",
            _ when path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) => "Infrastructure",
            _ when path.StartsWith("/ready", StringComparison.OrdinalIgnoreCase) => "Infrastructure",
            _ when path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase) => "Infrastructure",
            _ => null
        };
    }
}
