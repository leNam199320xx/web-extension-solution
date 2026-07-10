using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Api.Modules.Subscriptions.DTOs;
using PluginRuntime.Api.Modules.Subscriptions.Services;

namespace PluginRuntime.Api.Modules.Subscriptions.Controllers;

/// <summary>
/// Controller for tenant package subscription operations (subscribe, unsubscribe, list active).
/// </summary>
[ApiController]
[Route("api/subscriptions/packages")]
public sealed class PackageSubscriptionController : ControllerBase
{
    private readonly IPackageSubscriptionService _subscriptionService;

    public PackageSubscriptionController(IPackageSubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    /// <summary>Subscribes a tenant to a plugin package.</summary>
    [HttpPost("{packageId:guid}/subscribe")]
    [ProducesResponseType(typeof(PackageSubscriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Subscribe(
        Guid packageId,
        [FromQuery] Guid tenantId,
        CancellationToken ct)
    {
        var result = await _subscriptionService.SubscribeAsync(tenantId, packageId, ct);
        return CreatedAtAction(nameof(ListActive), new { tenantId }, result);
    }

    /// <summary>Unsubscribes a tenant from a plugin package.</summary>
    [HttpPost("{packageId:guid}/unsubscribe")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unsubscribe(
        Guid packageId,
        [FromQuery] Guid tenantId,
        CancellationToken ct)
    {
        await _subscriptionService.UnsubscribeAsync(tenantId, packageId, ct);
        return NoContent();
    }

    /// <summary>Lists all active package subscriptions for a tenant.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PackageSubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListActive(
        [FromQuery] Guid tenantId,
        CancellationToken ct)
    {
        var result = await _subscriptionService.ListActiveAsync(tenantId, ct);
        return Ok(result);
    }
}
