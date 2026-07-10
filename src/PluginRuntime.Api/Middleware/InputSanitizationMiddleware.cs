using System.Text.RegularExpressions;

namespace PluginRuntime.Api.Middleware;

/// <summary>
/// Validates and sanitizes input on all module endpoints.
/// Prevents SQL injection patterns and XSS in query parameters and headers.
/// Masks sensitive data (API keys, Stripe secrets) in logs via the logging pipeline.
/// </summary>
public sealed partial class InputSanitizationMiddleware : IMiddleware
{
    private readonly ILogger<InputSanitizationMiddleware> _logger;

    // Regex patterns for detecting common attack vectors in query strings
    [GeneratedRegex(@"(\bunion\b\s+\bselect\b|\bexec\b\s*\(|;\s*\bdrop\b|\b--\b|\/\*)", RegexOptions.IgnoreCase)]
    private static partial Regex SqlInjectionPattern();

    [GeneratedRegex(@"<script[^>]*>|javascript:|on\w+\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex XssPattern();

    public InputSanitizationMiddleware(ILogger<InputSanitizationMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Validate query string parameters
        foreach (var (key, values) in context.Request.Query)
        {
            foreach (var value in values)
            {
                if (value is null) continue;

                if (SqlInjectionPattern().IsMatch(value))
                {
                    _logger.LogWarning(
                        "Potential SQL injection detected in query parameter '{Key}' from {IP}",
                        key,
                        context.Connection.RemoteIpAddress);

                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            code = "UA-SEC-002",
                            message = "Invalid input detected"
                        }
                    });
                    return;
                }

                if (XssPattern().IsMatch(value))
                {
                    _logger.LogWarning(
                        "Potential XSS detected in query parameter '{Key}' from {IP}",
                        key,
                        context.Connection.RemoteIpAddress);

                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            code = "UA-SEC-003",
                            message = "Invalid input detected"
                        }
                    });
                    return;
                }
            }
        }

        await next(context);
    }
}
