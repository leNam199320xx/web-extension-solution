using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Plugins.Controllers;

/// <summary>
/// Plugin CRUD, execution, manifests, capabilities.
/// Enforces max_plugins_upload limit from tenant's plan.
/// </summary>
[ApiController]
[Route("api/plugins")]
public sealed class PluginsController : ControllerBase
{
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ILogger<PluginsController> _logger;

    public PluginsController(
        ICurrentTenantContext tenantContext,
        ILogger<PluginsController> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Lists plugins accessible to the current tenant.
    /// </summary>
    [HttpGet]
    public IActionResult ListPlugins([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (pageSize > 100) pageSize = 100;
        if (page < 1) page = 1;

        // Include system extensions in the listing
        var systemExtensions = HttpContext.RequestServices.GetServices<ISystemExtension>()
            .Select(ext => new
            {
                ExtensionId = ext.ExtensionId,
                Name = ext.Name,
                Version = ext.Version,
                Description = ext.Description,
                Type = "system",
                Status = "Active",
                Visibility = "public"
            })
            .ToList();

        _logger.LogInformation(
            "Listing plugins for tenant {TenantId}, page {Page}, pageSize {PageSize}",
            _tenantContext.TenantId, page, pageSize);

        return Ok(new { plugins = systemExtensions, page, pageSize, total = systemExtensions.Count });
    }

    /// <summary>
    /// Gets details of a specific plugin.
    /// </summary>
    [HttpGet("{pluginId:guid}")]
    public IActionResult GetPlugin(Guid pluginId)
    {
        _logger.LogInformation(
            "Getting plugin {PluginId} for tenant {TenantId}",
            pluginId,
            _tenantContext.TenantId);

        // Placeholder: actual plugin retrieval
        return NotFound();
    }

    /// <summary>
    /// Uploads a new plugin. Enforces max_plugins_upload limit.
    /// </summary>
    [HttpPost]
    public IActionResult UploadPlugin()
    {
        _logger.LogInformation(
            "Plugin upload requested by tenant {TenantId}",
            _tenantContext.TenantId);

        // Placeholder: actual upload logic with plan limit enforcement
        return StatusCode(501, new { message = "Plugin upload not yet implemented in unified architecture" });
    }
}
