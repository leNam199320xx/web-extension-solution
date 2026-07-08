using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Api.Modules.Tenants.DTOs;
using PluginRuntime.Api.Modules.Tenants.Services;
using PluginRuntime.Api.Shared.DTOs;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Exceptions;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Tenants.Controllers;

/// <summary>
/// API controller for tenant registration, lifecycle management, and listing.
/// </summary>
[ApiController]
[Route("api/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ICurrentTenantContext _tenantContext;

    public TenantsController(ITenantService tenantService, ICurrentTenantContext tenantContext)
    {
        _tenantService = tenantService;
        _tenantContext = tenantContext;
    }

    /// <summary>Registers a new external tenant.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] TenantRegistrationRequest request,
        CancellationToken ct)
    {
        var tenant = await _tenantService.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = tenant.TenantId }, tenant);
    }

    /// <summary>Registers an internal tenant (Platform_Admin only).</summary>
    [HttpPost("internal")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegisterInternal(
        [FromBody] InternalTenantRequest request,
        CancellationToken ct)
    {
        if (!_tenantContext.IsAdmin)
            throw new InternalTenantAuthException();

        var tenant = await _tenantService.RegisterInternalAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = tenant.TenantId }, tenant);
    }

    /// <summary>Retrieves a tenant by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tenant = await _tenantService.GetByIdAsync(id, ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    /// <summary>Lists tenants with optional filtering and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TenantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] TenantStatus? status,
        [FromQuery] Guid? plan,
        [FromQuery] bool? isInternal,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var filter = new TenantFilter
        {
            Status = status,
            PlanId = plan,
            IsInternal = isInternal
        };

        var paging = new PaginationParams
        {
            Page = page,
            PageSize = pageSize
        };

        var result = await _tenantService.ListAsync(filter, paging, ct);
        return Ok(result);
    }

    /// <summary>Suspends an active tenant.</summary>
    [HttpPut("{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Suspend(
        Guid id,
        [FromBody] LifecycleRequest request,
        CancellationToken ct)
    {
        await _tenantService.SuspendAsync(id, request.ActorId, request.Reason, ct);
        return NoContent();
    }

    /// <summary>Reactivates a suspended tenant.</summary>
    [HttpPut("{id:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(
        Guid id,
        [FromBody] LifecycleRequest request,
        CancellationToken ct)
    {
        await _tenantService.ReactivateAsync(id, request.ActorId, request.Reason, ct);
        return NoContent();
    }

    /// <summary>Soft-deletes a tenant.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromBody] LifecycleRequest request,
        CancellationToken ct)
    {
        await _tenantService.DeleteAsync(id, request.ActorId, request.Reason, ct);
        return NoContent();
    }
}

/// <summary>
/// Request body for tenant lifecycle operations (suspend, reactivate, delete).
/// </summary>
public sealed record LifecycleRequest
{
    /// <summary>ID of the actor performing the operation.</summary>
    public string ActorId { get; init; } = string.Empty;

    /// <summary>Reason for the lifecycle change.</summary>
    public string Reason { get; init; } = string.Empty;
}
