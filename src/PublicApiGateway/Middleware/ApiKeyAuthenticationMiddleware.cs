using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PublicApiGateway.Configuration;
using PublicApiGateway.Models;
using PublicApiGateway.Services;

namespace PublicApiGateway.Middleware;

/// <summary>
/// Authenticates requests via X-Api-Key header.
/// Checks IP block list, validates key format, resolves tenant context.
/// </summary>
public sealed partial class ApiKeyAuthenticationMiddleware : IMiddleware
{
    private readonly IApiKeyService _apiKeyService;
    private readonly IIpBlockingService _ipBlockingService;
    private readonly GatewayOptions _options;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    [GeneratedRegex(@"^[a-zA-Z0-9\-_]{32,128}$")]
    private static partial Regex ApiKeyFormatRegex();

    public ApiKeyAuthenticationMiddleware(
        IApiKeyService apiKeyService,
        IIpBlockingService ipBlockingService,
        IOptions<GatewayOptions> options,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _apiKeyService = apiKeyService;
        _ipBlockingService = ipBlockingService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Check IP block list
        if (await _ipBlockingService.IsBlockedAsync(ip, context.RequestAborted))
        {
            _logger.LogWarning("Blocked IP {IP} attempted access", ip);
            await WriteError(context, 403, ErrorCodes.AuthRevoked, "Access denied");
            return;
        }

        // Extract X-Api-Key header
        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader)
            || string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            await WriteError(context, 401, ErrorCodes.AuthRequired, "API key required");
            return;
        }

        var apiKey = apiKeyHeader.ToString();

        // Validate format
        if (!ApiKeyFormatRegex().IsMatch(apiKey))
        {
            await _ipBlockingService.RecordFailedAttemptAsync(ip, context.RequestAborted);
            await WriteError(context, 400, ErrorCodes.InvalidKeyFormat, "Invalid API key format");
            return;
        }

        // Validate against cache/DB
        var keyInfo = await _apiKeyService.ValidateAsync(apiKey, context.RequestAborted);

        if (keyInfo is null)
        {
            await _ipBlockingService.RecordFailedAttemptAsync(ip, context.RequestAborted);
            _logger.LogWarning("Invalid API key from {IP} (key: ...{Suffix})", ip, MaskKey(apiKey));
            await WriteError(context, 401, ErrorCodes.AuthRequired, "Invalid API key");
            return;
        }

        // Check status
        if (keyInfo.Status == ApiKeyStatus.Expired || (keyInfo.ExpiresAt.HasValue && keyInfo.ExpiresAt.Value < DateTime.UtcNow))
        {
            await _ipBlockingService.RecordFailedAttemptAsync(ip, context.RequestAborted);
            await WriteError(context, 401, ErrorCodes.AuthExpired, "API key has expired");
            return;
        }

        if (keyInfo.Status == ApiKeyStatus.Revoked)
        {
            await _ipBlockingService.RecordFailedAttemptAsync(ip, context.RequestAborted);
            await WriteError(context, 403, ErrorCodes.AuthRevoked, "API key has been revoked");
            return;
        }

        // Set TenantContext
        var tenantContext = new TenantContext(
            TenantId: keyInfo.TenantId,
            TenantName: keyInfo.TenantName,
            PlanType: keyInfo.PlanType,
            Limits: keyInfo.Limits);

        context.Items["TenantContext"] = tenantContext;

        await next(context);
    }

    private static string MaskKey(string key) =>
        key.Length >= 4 ? key[^4..] : "****";

    private static async Task WriteError(HttpContext context, int statusCode, string errorCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var error = new GatewayError(
            Code: errorCode,
            Message: message,
            TraceId: context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier,
            Timestamp: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

        await context.Response.WriteAsJsonAsync(error);
    }
}
