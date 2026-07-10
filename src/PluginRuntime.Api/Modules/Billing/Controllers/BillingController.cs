using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Modules.Billing.Domain;
using PluginRuntime.Api.Modules.Billing.DTOs;
using PluginRuntime.Api.Modules.Billing.Services;
using PluginRuntime.Api.Shared.DTOs;
using PluginRuntime.Api.Shared.Infrastructure;

namespace PluginRuntime.Api.Modules.Billing.Controllers;

/// <summary>
/// API controller for billing queries including invoices and usage aggregates.
/// </summary>
[ApiController]
[Route("api/billing")]
public sealed class BillingController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly AppDbContext _dbContext;

    public BillingController(IInvoiceService invoiceService, AppDbContext dbContext)
    {
        _invoiceService = invoiceService;
        _dbContext = dbContext;
    }

    /// <summary>Retrieves paginated invoices for a tenant.</summary>
    [HttpGet("invoices")]
    [ProducesResponseType(typeof(PagedResult<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] Guid tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var paging = new PaginationParams
        {
            Page = page,
            PageSize = pageSize
        };

        var result = await _invoiceService.GetTenantInvoicesAsync(tenantId, paging, ct);
        return Ok(result);
    }

    /// <summary>Retrieves usage aggregates for a tenant within a date range.</summary>
    [HttpGet("usage")]
    [ProducesResponseType(typeof(IReadOnlyList<UsageAggregate>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsage(
        [FromQuery] Guid tenantId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken ct = default)
    {
        var aggregates = await _dbContext.UsageAggregates
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Date >= startDate && u.Date <= endDate)
            .OrderByDescending(u => u.Date)
            .ToListAsync(ct);

        return Ok(aggregates);
    }
}
