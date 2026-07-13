using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Portal;

/// <summary>
/// Plugin execution endpoint — the core runtime API.
/// Flow: Authenticate → Resolve extension → Check access → Check quota → Execute pipeline → Record usage
/// </summary>
[ApiController]
[Route("api/plugins")]
public sealed class ExecuteController : ControllerBase
{
    private readonly IExecutionPipeline _pipeline;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<ExecuteController> _logger;

    public ExecuteController(
        IExecutionPipeline pipeline,
        ICurrentTenantContext tenantContext,
        IRateLimiter rateLimiter,
        ILogger<ExecuteController> logger)
    {
        _pipeline = pipeline;
        _tenantContext = tenantContext;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    /// <summary>
    /// Execute a plugin by extension ID.
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ExecuteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Execute(
        [FromBody] ExecuteRequest request,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.ExtensionId) && request.PluginId == Guid.Empty)
        {
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = "extension_id or plugin_id is required." } });
        }

        var tenantId = _tenantContext.TenantId?.ToString();

        // Rate limiting check
        if (tenantId is not null)
        {
            var rateLimitKey = $"execute:{tenantId}";
            var rateLimitResult = await _rateLimiter.CheckAsync(
                rateLimitKey, maxRequests: 10000, window: TimeSpan.FromMinutes(1), cancellationToken);

            if (!rateLimitResult.IsAllowed)
            {
                var retrySeconds = (int)rateLimitResult.RetryAfter.TotalSeconds;
                Response.Headers["Retry-After"] = retrySeconds.ToString();
                return StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    error = new { code = "RATE_LIMITED", message = "Too many requests. Please try again later." },
                    retryAfter = retrySeconds
                });
            }
        }

        // Resolve plugin ID
        var pluginId = request.PluginId != Guid.Empty
            ? request.PluginId
            : TryParseExtensionId(request.ExtensionId);

        if (pluginId == Guid.Empty)
        {
            return BadRequest(new { error = new { code = "INVALID_EXTENSION_ID", message = "Cannot parse extension_id." } });
        }

        // Build execution request
        var executionRequest = new ExecutionRequest(
            PluginId: pluginId,
            Version: request.Version,
            Input: request.Input,
            CorrelationId: request.CorrelationId,
            UserId: null,
            TenantId: tenantId);

        // Execute through pipeline
        _logger.LogInformation("Executing plugin {PluginId} for tenant {TenantId}",
            pluginId, tenantId);

        var result = await _pipeline.ProcessAsync(executionRequest, cancellationToken);

        // Map result to response
        if (result.Success)
        {
            return Ok(new ExecuteResponse
            {
                ExecutionId = result.ExecutionId,
                Status = "Completed",
                Output = result.Data,
                DurationMs = result.DurationMs,
                TraceId = result.TraceId
            });
        }

        // Determine HTTP status code based on error category
        var statusCode = result.ErrorCategory switch
        {
            "NotFound" => StatusCodes.Status404NotFound,
            "Security" => StatusCodes.Status403Forbidden,
            "Timeout" => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(statusCode, new ExecuteResponse
        {
            ExecutionId = result.ExecutionId,
            Status = "Failed",
            Output = null,
            DurationMs = result.DurationMs,
            TraceId = result.TraceId,
            Error = new ExecuteErrorResponse
            {
                Code = result.ErrorCode ?? "UNKNOWN",
                Message = result.ErrorMessage ?? "Execution failed.",
                Stage = result.FailingStage
            }
        });
    }

    private static Guid TryParseExtensionId(string? extensionId)
    {
        if (string.IsNullOrWhiteSpace(extensionId)) return Guid.Empty;
        return Guid.TryParse(extensionId, out var id) ? id : Guid.Empty;
    }
}

// ── Request/Response DTOs ────────────────────────────────────────────

public sealed record ExecuteRequest
{
    public string? ExtensionId { get; init; }
    public Guid PluginId { get; init; }
    public string? Version { get; init; }
    public JsonElement Input { get; init; }
    public string? CorrelationId { get; init; }
    public int? TimeoutMs { get; init; }
}

public sealed class ExecuteResponse
{
    public string ExecutionId { get; init; } = "";
    public string Status { get; init; } = "";
    public JsonElement? Output { get; init; }
    public int DurationMs { get; init; }
    public string TraceId { get; init; } = "";
    public ExecuteErrorResponse? Error { get; init; }
}

public sealed class ExecuteErrorResponse
{
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";
    public string? Stage { get; init; }
}
