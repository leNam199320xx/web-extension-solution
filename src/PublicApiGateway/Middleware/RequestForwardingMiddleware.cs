using System.Net;
using Microsoft.Extensions.Options;
using PublicApiGateway.Configuration;
using PublicApiGateway.Models;
using PublicApiGateway.Services;

namespace PublicApiGateway.Middleware;

/// <summary>
/// Forwards authenticated requests to the upstream PluginRuntime.Api.
/// Strips sensitive headers, adds service auth token and tenant context.
/// </summary>
public sealed class RequestForwardingMiddleware : IMiddleware
{
    private static readonly HashSet<string> StripHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "X-Api-Key", "Connection", "Keep-Alive", "Transfer-Encoding", "Upgrade", "Host"
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenService _tokenService;
    private readonly UpstreamOptions _upstream;
    private readonly ILogger<RequestForwardingMiddleware> _logger;

    public RequestForwardingMiddleware(
        IHttpClientFactory httpClientFactory,
        ITokenService tokenService,
        IOptions<UpstreamOptions> upstream,
        ILogger<RequestForwardingMiddleware> logger)
    {
        _httpClientFactory = httpClientFactory;
        _tokenService = tokenService;
        _upstream = upstream.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var tenantContext = context.Items["TenantContext"] as TenantContext;
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;

        try
        {
            var serviceToken = await _tokenService.GetServiceTokenAsync(context.RequestAborted);

            // Build upstream request
            var upstreamUrl = $"{_upstream.BaseUrl}{context.Request.Path}{context.Request.QueryString}";

            using var client = _httpClientFactory.CreateClient("Upstream");
            client.Timeout = TimeSpan.FromSeconds(_upstream.TimeoutSeconds);

            using var request = new HttpRequestMessage(
                new HttpMethod(context.Request.Method),
                upstreamUrl);

            // Copy headers (excluding stripped ones)
            foreach (var header in context.Request.Headers)
            {
                if (StripHeaders.Contains(header.Key)) continue;
                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            // Add gateway headers
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {serviceToken}");
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantContext?.TenantId.ToString() ?? "");
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);

            // Copy body for methods that have one
            if (context.Request.ContentLength > 0 || context.Request.ContentType is not null)
            {
                request.Content = new StreamContent(context.Request.Body);
                if (context.Request.ContentType is not null)
                {
                    request.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
                }
            }

            // Forward
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

            // Copy response status
            context.Response.StatusCode = (int)response.StatusCode;

            // Copy response headers
            foreach (var header in response.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in response.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            // Remove transfer-encoding since we're buffering
            context.Response.Headers.Remove("Transfer-Encoding");

            // Copy response body
            await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Upstream request timed out for {CorrelationId}", correlationId);
            await WriteUpstreamError(context, correlationId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Upstream unreachable for {CorrelationId}", correlationId);
            await WriteUpstreamError(context, correlationId);
        }
    }

    private static async Task WriteUpstreamError(HttpContext context, string correlationId)
    {
        context.Response.StatusCode = 502;
        context.Response.ContentType = "application/json";

        var error = new GatewayError(
            Code: ErrorCodes.UpstreamError,
            Message: "Upstream service unavailable",
            TraceId: correlationId,
            Timestamp: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

        await context.Response.WriteAsJsonAsync(error);
    }
}
