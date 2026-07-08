using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PluginRuntime.Api.Shared.Exceptions;

namespace PluginRuntime.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions and maps them to standardized JSON error responses.
/// </summary>
public sealed class GlobalExceptionMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (UnifiedApiException ex)
        {
            _logger.LogWarning(
                ex,
                "API error {ErrorCode}: {Message}",
                ex.ErrorCode,
                ex.Message);

            if (ex is BillingProviderException billingEx)
            {
                _logger.LogError(
                    "Billing provider internal detail: {Detail}",
                    billingEx.InternalDetail);
            }

            await WriteErrorResponseAsync(context, ex.HttpStatusCode, ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain error: {Message}", ex.Message);

            await WriteErrorResponseAsync(context, 400, "UA-DOMAIN-001", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            await WriteErrorResponseAsync(context, 500, "UA-INTERNAL", "An internal error occurred");
        }
    }

    private static async Task WriteErrorResponseAsync(
        HttpContext context,
        int statusCode,
        string errorCode,
        string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var errorResponse = new
        {
            error = new
            {
                code = errorCode,
                message,
                traceId,
                timestamp
            }
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(errorResponse, JsonOptions));
    }
}
