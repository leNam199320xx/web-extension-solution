using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Api.Modules.Subscriptions.DTOs;
using PluginRuntime.Api.Modules.Subscriptions.Services;
using PluginRuntime.Api.Shared.DTOs;

namespace PluginRuntime.Api.Modules.Subscriptions.Controllers;

/// <summary>
/// Admin controller for managing plugin packages.
/// Also exposes a public listing endpoint for active packages.
/// </summary>
[ApiController]
public sealed class PluginPackagesController : ControllerBase
{
    private readonly IPluginPackageService _packageService;

    public PluginPackagesController(IPluginPackageService packageService)
    {
        _packageService = packageService;
    }

    /// <summary>Creates a new plugin package (admin only).</summary>
    [HttpPost("api/admin/packages")]
    [ProducesResponseType(typeof(PluginPackageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePackageRequest request,
        CancellationToken ct)
    {
        var result = await _packageService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(ListActive), null, result);
    }

    /// <summary>Updates an existing plugin package (admin only).</summary>
    [HttpPut("api/admin/packages/{packageId:guid}")]
    [ProducesResponseType(typeof(PluginPackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid packageId,
        [FromBody] UpdatePackageRequest request,
        CancellationToken ct)
    {
        var result = await _packageService.UpdateAsync(packageId, request, ct);
        return Ok(result);
    }

    /// <summary>Deactivates a plugin package (admin only). Existing subscriptions are preserved.</summary>
    [HttpDelete("api/admin/packages/{packageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid packageId, CancellationToken ct)
    {
        await _packageService.DeactivateAsync(packageId, ct);
        return NoContent();
    }

    /// <summary>Lists active plugin packages with pagination (admin route).</summary>
    [HttpGet("api/admin/packages")]
    [ProducesResponseType(typeof(PagedResult<PluginPackageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListActiveAdmin(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var paging = new PaginationParams { Page = page, PageSize = pageSize };
        var result = await _packageService.ListActiveAsync(paging, ct);
        return Ok(result);
    }

    /// <summary>Lists active plugin packages with pagination (public route for catalog browsing).</summary>
    [HttpGet("api/packages")]
    [ProducesResponseType(typeof(PagedResult<PluginPackageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListActive(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var paging = new PaginationParams { Page = page, PageSize = pageSize };
        var result = await _packageService.ListActiveAsync(paging, ct);
        return Ok(result);
    }
}
