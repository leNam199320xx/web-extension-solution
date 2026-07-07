using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Capabilities.Extension;
using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ExtensionsController : ControllerBase
{
    private readonly SubscriptionService _subscriptionService;
    private readonly IExtensionSubscriptionRepository _subscriptionRepository;

    public ExtensionsController(
        SubscriptionService subscriptionService,
        IExtensionSubscriptionRepository subscriptionRepository)
    {
        _subscriptionService = subscriptionService;
        _subscriptionRepository = subscriptionRepository;
    }

    [HttpPost("{targetId}/subscribe")]
    public async Task<IActionResult> Subscribe(
        [FromRoute] string targetId,
        [FromBody] SubscribeRequest body,
        CancellationToken cancellationToken)
    {
        // sourceExtensionId would come from authenticated caller context
        // For now, use a header or claim to identify the calling extension
        var sourceExtensionId = HttpContext.Request.Headers["X-Extension-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(sourceExtensionId))
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "MISSING_SOURCE_EXTENSION",
                    category = "Validation",
                    message = "X-Extension-Id header is required to identify the subscribing extension.",
                    traceId = HttpContext.TraceIdentifier,
                    timestamp = DateTime.UtcNow
                }
            });
        }

        try
        {
            var subscriptionId = await _subscriptionService.RequestSubscriptionAsync(
                sourceExtensionId, targetId, body.Reason, body.ExpectedUsage, cancellationToken);

            return Ok(new { subscriptionId, targetId, status = "Requested" });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                error = new
                {
                    code = "SUBSCRIPTION_EXISTS",
                    category = "Validation",
                    message = ex.Message,
                    traceId = HttpContext.TraceIdentifier,
                    timestamp = DateTime.UtcNow
                }
            });
        }
    }

    [HttpGet("{extensionId}/subscriptions")]
    public async Task<IActionResult> ListSubscriptions(
        [FromRoute] string extensionId,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetBySourceAndTargetAsync(
            extensionId, extensionId, cancellationToken);

        // Return all subscriptions where extensionId is target
        // For a full implementation, we'd query by target; for now return single match if exists
        if (subscription is not null)
        {
            return Ok(new[] { subscription });
        }

        return Ok(Array.Empty<object>());
    }

    [HttpPost("{extensionId}/subscriptions/{id:guid}/decide")]
    public async Task<IActionResult> Decide(
        [FromRoute] string extensionId,
        [FromRoute] Guid id,
        [FromBody] DecisionRequest body,
        CancellationToken cancellationToken)
    {
        // decidedBy would come from authenticated user claims
        var decidedBy = Guid.Empty; // Placeholder: extract from JWT claims in production
        var userIdClaim = HttpContext.User.FindFirst("sub")?.Value;
        if (Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            decidedBy = parsedUserId;
        }

        try
        {
            await _subscriptionService.DecideAsync(
                id, body.Decision, decidedBy, body.Comment, body.ExpiresAt, cancellationToken);

            return Ok(new { subscriptionId = id, extensionId, decision = body.Decision });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "INVALID_DECISION",
                    category = "Validation",
                    message = ex.Message,
                    traceId = HttpContext.TraceIdentifier,
                    timestamp = DateTime.UtcNow
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "SUBSCRIPTION_NOT_FOUND",
                    category = "NotFound",
                    message = ex.Message,
                    traceId = HttpContext.TraceIdentifier,
                    timestamp = DateTime.UtcNow
                }
            });
        }
    }

    [HttpPost("{extensionId}/subscriptions/{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(
        [FromRoute] string extensionId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _subscriptionService.RevokeAsync(id, cancellationToken);
            return Ok(new { subscriptionId = id, extensionId, status = "Revoked" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "SUBSCRIPTION_NOT_FOUND",
                    category = "NotFound",
                    message = ex.Message,
                    traceId = HttpContext.TraceIdentifier,
                    timestamp = DateTime.UtcNow
                }
            });
        }
    }
}

public record SubscribeRequest
{
    public string? Reason { get; init; }
    public string? ExpectedUsage { get; init; }
}

public record DecisionRequest
{
    public string Decision { get; init; } = "";
    public string? Comment { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
