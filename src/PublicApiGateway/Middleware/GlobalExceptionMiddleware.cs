using PublicApiGateway.Models;

namespace PublicApiGateway.Middleware;

/// <summary>
/// Global exception handler. Maps GatewayException types to structured error responses.
/// Ensures no internal details leak in responses.
/// </summary>
public sealed class GlobalExceptionMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

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
        catch (GatewayException ex)
        {
            _logger.LogWarning(ex, "Gateway error {Code}: {Message}", ex.ErrorCode, ex.Message);
            await WriteErrorAsync(context, ex.HttpStatusCode, ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, 500, "GW-INTERNAL-001", "An internal error occurred");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string code, string message)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        // Truncate message to 500 chars
        var safeMessage = message.Length > 500 ? message[..500] : message;

        var error = new GatewayError(
            Code: code,
            Message: safeMessage,
            TraceId: context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier,
            Timestamp: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

        await context.Response.WriteAsJsonAsync(error);
    }
}
