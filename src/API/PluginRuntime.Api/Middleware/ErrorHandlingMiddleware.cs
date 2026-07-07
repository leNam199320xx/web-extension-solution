using System.Net;
using System.Text.Json;
using PluginRuntime.Api.Models;
using PluginRuntime.Core.Exceptions;

namespace PluginRuntime.Api.Middleware;

/// <summary>
/// Global exception handling middleware. Catches all unhandled exceptions and returns
/// a standardized error response format. Maps PluginRuntimeException categories to
/// appropriate HTTP status codes.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (PluginRuntimeException ex)
        {
            _logger.LogWarning(
                ex,
                "Plugin runtime error: {Category}/{ErrorCode} - {Message}",
                ex.Category,
                ex.ErrorCode,
                ex.Message);

            await WriteErrorResponseAsync(context, ex.ErrorCode, ex.Category, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");

            await WriteErrorResponseAsync(
                context,
                "INTERNAL_ERROR",
                "Execution",
                "An internal server error occurred.");
        }
    }

    private static async Task WriteErrorResponseAsync(
        HttpContext context,
        string code,
        string category,
        string message)
    {
        var traceId = context.TraceIdentifier;
        var timestamp = DateTime.UtcNow.ToString("O");

        var errorResponse = new ErrorResponse(
            new ErrorDetail(code, category, message, traceId, timestamp));

        var statusCode = MapCategoryToStatusCode(category);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(errorResponse, JsonOptions));
    }

    private static HttpStatusCode MapCategoryToStatusCode(string category) => category switch
    {
        "Validation" => HttpStatusCode.BadRequest,
        "Security" => HttpStatusCode.Forbidden,
        "NotFound" => HttpStatusCode.NotFound,
        "Execution" => HttpStatusCode.InternalServerError,
        "Timeout" => HttpStatusCode.GatewayTimeout,
        "ResourceLimit" => HttpStatusCode.TooManyRequests,
        _ => HttpStatusCode.InternalServerError
    };
}
