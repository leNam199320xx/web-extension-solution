using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Api.Models;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ExecuteController : ControllerBase
{
    private readonly IExecutionPipeline _pipeline;

    public ExecuteController(IExecutionPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    [HttpPost("{pluginId:guid}")]
    public async Task<IActionResult> Execute(
        [FromRoute] Guid pluginId,
        [FromBody] ExecuteRequest body,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value;
        var tenantId = User.FindFirst("tenant_id")?.Value;

        var request = new ExecutionRequest(
            PluginId: pluginId,
            Version: body.Version,
            Input: body.Input,
            CorrelationId: body.Metadata?.CorrelationId,
            UserId: userId,
            TenantId: tenantId);

        var result = await _pipeline.ProcessAsync(request, cancellationToken);

        if (!result.Success)
        {
            var statusCode = Helpers.ErrorCategoryMapper.GetHttpStatus(result.ErrorCategory ?? "Execution");
            return StatusCode(statusCode, new ErrorResponse(
                new ErrorDetail(
                    result.ErrorCode ?? "UNKNOWN",
                    result.ErrorCategory ?? "Execution",
                    result.ErrorMessage ?? "Execution failed.",
                    result.TraceId,
                    DateTime.UtcNow.ToString("O"))));
        }

        return Ok(new
        {
            success = result.Success,
            data = result.Data,
            executionId = result.ExecutionId,
            traceId = result.TraceId,
            durationMs = result.DurationMs
        });
    }
}

public record ExecuteRequest
{
    public JsonElement Input { get; init; }
    public string? Version { get; init; }
    public ExecuteRequestMetadata? Metadata { get; init; }
}

public record ExecuteRequestMetadata
{
    public string? CorrelationId { get; init; }
}
