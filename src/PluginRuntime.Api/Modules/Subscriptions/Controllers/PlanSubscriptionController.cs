using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Api.Modules.Subscriptions.DTOs;
using PluginRuntime.Api.Modules.Subscriptions.Services;

namespace PluginRuntime.Api.Modules.Subscriptions.Controllers;

/// <summary>
/// Controller for managing tenant plan subscriptions and viewing combined subscription state.
/// </summary>
[ApiController]
[Route("api/subscriptions")]
public sealed class PlanSubscriptionController : ControllerBase
{
    private readonly IPlanSubscriptionService _planService;
    private readonly IPackageSubscriptionService _packageService;

    public PlanSubscriptionController(
        IPlanSubscriptionService planService,
        IPackageSubscriptionService packageService)
    {
        _planService = planService;
        _packageService = packageService;
    }

    /// <summary>Changes the tenant's plan (upgrade or downgrade).</summary>
    [HttpPut("plan")]
    [ProducesResponseType(typeof(PlanChangeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePlan(
        [FromBody] PlanChangeRequest request,
        [FromQuery] Guid tenantId,
        CancellationToken ct)
    {
        var result = await _planService.ChangePlanAsync(tenantId, request, ct);
        return Ok(result);
    }

    /// <summary>Returns the tenant's current plan and all active package subscriptions.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CombinedSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] Guid tenantId,
        CancellationToken ct)
    {
        var currentPlan = await _planService.GetCurrentAsync(tenantId, ct);
        var activePackages = await _packageService.ListActiveAsync(tenantId, ct);

        var response = new CombinedSubscriptionResponse(currentPlan, activePackages);
        return Ok(response);
    }
}

/// <summary>
/// Combined response containing the tenant's current plan and active package subscriptions.
/// </summary>
public sealed record CombinedSubscriptionResponse(
    CurrentSubscriptionDto Plan,
    IReadOnlyList<PackageSubscriptionDto> PackageSubscriptions);
