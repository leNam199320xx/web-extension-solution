using System.Diagnostics;

namespace PluginRuntime.Api.Middleware;

/// <summary>
/// Structured JSON logging middleware.
/// Logs: timestamp (ISO 8601), level, traceId, spanId, tenantId, module, method, path, statusCode, durationMs.
/// Ensures no sensitive data (API keys, Stripe secrets) appears in logs.
/// </summary>
public sealed class RequestLoggingMiddleware : IMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var sw = Stopwatch.StartNew();
        var activity = Activity.Current;

        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();

            var tenantId = context.User?.FindFirst("tenant_id")?.Value ?? "anonymous";
            var module = context.Response.Headers["X-Module"].ToString();

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {DurationMs}ms " +
                "[tenant:{TenantId} module:{Module} trace:{TraceId} span:{SpanId}]",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                tenantId,
                string.IsNullOrEmpty(module) ? "Unknown" : module,
                activity?.TraceId.ToString() ?? context.TraceIdentifier,
                activity?.SpanId.ToString() ?? "none");
        }
    }
}
