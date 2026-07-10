using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Api.Shared.DTOs;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Plugins.Controllers;

/// <summary>
/// Cross-module platform admin operations.
/// Uses IRepository for provider-agnostic persistence.
/// </summary>
[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IRepository<Tenant> _tenants;
    private readonly IRepository<Plan> _plans;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IRepository<Tenant> tenants,
        IRepository<Plan> plans,
        ILogger<AdminController> logger)
    {
        _tenants = tenants;
        _plans = plans;
        _logger = logger;
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> ListTenants(
        [FromQuery] bool? isInternal,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (pageSize > 100) pageSize = 100;
        if (page < 1) page = 1;

        var query = _tenants.Query();
        if (isInternal.HasValue)
            query = query.Where(t => t.IsInternal == isInternal.Value);

        var total = query.Count();
        var tenants = query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList()
            .Select(t => new { t.TenantId, t.Name, ContactEmail = t.ContactEmail.Value, t.Status, t.PlanId, t.IsInternal, t.CreatedAt })
            .ToList();

        return Ok(new PagedResult<object> { Items = tenants.Cast<object>().ToList(), TotalCount = total, Page = page, PageSize = pageSize });
    }

    [HttpGet("plans")]
    public async Task<IActionResult> ListPlans(CancellationToken ct)
    {
        var plans = await _plans.GetAllAsync(ct);
        var result = plans.OrderBy(p => p.MonthlyPrice).Select(p => new
        {
            p.PlanId, p.Name, p.Type, p.RateLimit, p.DailyQuota, p.MaxApiKeys, p.MaxPluginsUpload, p.MaxPackageSubscriptions, p.MonthlyPrice, p.IsBillable
        }).ToList();
        return Ok(new { plans = result });
    }
}
