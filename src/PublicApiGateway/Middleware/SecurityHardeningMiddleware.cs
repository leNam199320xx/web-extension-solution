using System.Text.Json;
using Microsoft.Extensions.Options;
using PublicApiGateway.Configuration;
using PublicApiGateway.Models;

namespace PublicApiGateway.Middleware;

/// <summary>
/// First middleware in the pipeline. Enforces:
/// - HTTPS (reject HTTP with 421 / GW-SEC-004)
/// - Max request body size (reject with 413 / GW-SEC-001)
/// - Max request header size (reject with 431 / GW-SEC-002)
/// </summary>
public sealed class SecurityHardeningMiddleware : IMiddleware
{
    private readonly GatewayOptions _options;
    private readonly ILogger<SecurityHardeningMiddleware> _logger;

    public SecurityHardeningMiddleware(IOptions<GatewayOptions> options, ILogger<SecurityHardeningMiddleware> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // HTTPS enforcement (skip in development)
        if (!context.Request.IsHttps
            && !context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            _logger.LogWarning("HTTP request rejected — HTTPS required from {IP}", context.Connection.RemoteIpAddress);
            await WriteError(context, 421, ErrorCodes.HttpsRequired, "HTTPS required");
            return;
        }

        // Request body size check
        if (context.Request.ContentLength > _options.MaxRequestBodyBytes)
        {
            _logger.LogWarning("Request body too large: {Size} bytes from {IP}",
                context.Request.ContentLength, context.Connection.RemoteIpAddress);
            await WriteError(context, 413, ErrorCodes.BodyTooLarge, "Request body exceeds maximum allowed size");
            return;
        }

        // Request header size check
        var totalHeaderSize = context.Request.Headers.Sum(h => h.Key.Length + h.Value.ToString().Length);
        if (totalHeaderSize > _options.MaxRequestHeaderBytes)
        {
            _logger.LogWarning("Request headers too large: {Size} bytes from {IP}",
                totalHeaderSize, context.Connection.RemoteIpAddress);
            await WriteError(context, 431, ErrorCodes.HeaderTooLarge, "Request headers exceed maximum allowed size");
            return;
        }

        await next(context);
    }

    private static async Task WriteError(HttpContext context, int statusCode, string errorCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var error = new GatewayError(
            Code: errorCode,
            Message: message,
            TraceId: context.TraceIdentifier,
            Timestamp: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

        await context.Response.WriteAsJsonAsync(error);
    }
}
