using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Api.Modules.Billing.Domain;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Portal;

/// <summary>
/// Unified controller for Consumer Portal frontend.
/// All endpoints resolve tenant from JWT (ICurrentTenantContext).
/// </summary>
[ApiController]
[Route("api")]
public sealed class ConsumerPortalController : ControllerBase
{
    private readonly IRepository<Tenant> _tenants;
    private readonly IRepository<Plan> _plans;
    private readonly IRepository<ApiKey> _keys;
    private readonly IRepository<Invoice> _invoices;
    private readonly IRepository<UsageAggregate> _usage;
    private readonly ICurrentTenantContext _ctx;

    public ConsumerPortalController(
        IRepository<Tenant> tenants,
        IRepository<Plan> plans,
        IRepository<ApiKey> keys,
        IRepository<Invoice> invoices,
        IRepository<UsageAggregate> usage,
        ICurrentTenantContext ctx)
    {
        _tenants = tenants;
        _plans = plans;
        _keys = keys;
        _invoices = invoices;
        _usage = usage;
        _ctx = ctx;
    }

    // ── Dashboard ────────────────────────────────────────────────────

    [HttpGet("usage/dashboard")]
    public async Task<IActionResult> GetDashboardUsage(CancellationToken ct)
    {
        var tenantId = _ctx.TenantId ?? Guid.Empty;
        var tenant = await _tenants.GetByIdAsync(tenantId, ct);
        var plan = await _plans.GetByIdAsync(tenant.PlanId, ct);

        var keys = await _keys.FindAsync(k => k.TenantId == tenantId, ct);
        var activeKeys = keys.Where(k => k.Status == ApiKeyStatus.Active).ToList();
        var expiringKeys = activeKeys.Where(k => k.ExpiresAt.HasValue && k.ExpiresAt.Value < DateTime.UtcNow.AddDays(14)).ToList();

        var allUsage = await _usage.FindAsync(u => u.TenantId == tenantId, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayUsage = allUsage.FirstOrDefault(u => u.Date == today);

        var recent = allUsage
            .OrderByDescending(u => u.Date)
            .Take(5)
            .Select(u => new
            {
                u.Date,
                u.TotalRequests,
                SuccessRate = u.TotalRequests > 0 ? Math.Round((double)u.SuccessfulRequests / u.TotalRequests * 100, 1) : 0
            })
            .ToList();

        return Ok(new
        {
            PlanName = plan?.Name ?? "Unknown",
            plan?.RateLimit,
            plan?.DailyQuota,
            TodayRequests = todayUsage?.TotalRequests ?? 0L,
            TodaySuccessful = todayUsage?.SuccessfulRequests ?? 0L,
            TodayFailed = todayUsage?.FailedRequests ?? 0L,
            ActiveKeyCount = activeKeys.Count,
            ExpiringKeyCount = expiringKeys.Count,
            RecentActivity = recent
        });
    }

    // ── API Keys ─────────────────────────────────────────────────────

    [HttpGet("keys")]
    public async Task<IActionResult> GetKeys(CancellationToken ct)
    {
        var tenantId = _ctx.TenantId ?? Guid.Empty;
        var keys = await _keys.FindAsync(k => k.TenantId == tenantId, ct);
        var items = keys.OrderByDescending(k => k.CreatedAt).Select(k => new
        {
            k.KeyId,
            Name = (string?)null,
            Prefix = k.KeyPrefix,
            Suffix = k.KeySuffix,
            Status = k.Status.ToString(),
            k.CreatedAt,
            k.ExpiresAt,
            LastUsedAt = (DateTime?)null
        }).ToList();

        return Ok(new { Keys = items, TotalCount = items.Count });
    }

    [HttpPost("keys")]
    public async Task<IActionResult> GenerateKey([FromBody] GenerateKeyRequest request, CancellationToken ct)
    {
        var tenantId = _ctx.TenantId ?? Guid.Empty;
        var plaintext = $"pk_{Guid.NewGuid():N}";
        var keyId = Guid.NewGuid();
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plaintext))).ToLower();

        var key = new ApiKey(
            keyId,
            tenantId,
            hash,
            plaintext[..8],
            plaintext[^4..],
            request.ExpirationDays.HasValue ? DateTime.UtcNow.AddDays(request.ExpirationDays.Value) : null);

        await _keys.AddAsync(key, ct);
        await _keys.SaveChangesAsync(ct);

        return Ok(new
        {
            KeyId = keyId,
            PlaintextKey = plaintext,
            Prefix = key.KeyPrefix,
            CreatedAt = key.CreatedAt
        });
    }

    [HttpDelete("keys/{id:guid}")]
    public async Task<IActionResult> RevokeKey(Guid id, CancellationToken ct)
    {
        var key = await _keys.GetByIdAsync(id, ct);
        if (key is null) return NotFound();

        key.Revoke();
        await _keys.UpdateAsync(key, ct);
        await _keys.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("keys/{id:guid}/rotate")]
    public async Task<IActionResult> RotateKey(Guid id, CancellationToken ct)
    {
        var old = await _keys.GetByIdAsync(id, ct);
        if (old is null) return NotFound();

        old.Revoke();
        await _keys.UpdateAsync(old, ct);

        var plaintext = $"pk_{Guid.NewGuid():N}";
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plaintext))).ToLower();
        var newKey = new ApiKey(Guid.NewGuid(), old.TenantId, hash, plaintext[..8], plaintext[^4..]);

        await _keys.AddAsync(newKey, ct);
        await _keys.SaveChangesAsync(ct);

        return Ok(new { newKey.KeyId, PlaintextKey = plaintext, Prefix = newKey.KeyPrefix, newKey.CreatedAt });
    }

    // ── Usage ────────────────────────────────────────────────────────

    [HttpGet("usage/daily")]
    public async Task<IActionResult> GetDailyUsage([FromQuery] string from, [FromQuery] string to, CancellationToken ct)
    {
        var tenantId = _ctx.TenantId ?? Guid.Empty;
        var fromDate = DateOnly.Parse(from);
        var toDate = DateOnly.Parse(to);

        var all = await _usage.FindAsync(u => u.TenantId == tenantId, ct);
        var filtered = all
            .Where(u => u.Date >= fromDate && u.Date <= toDate)
            .OrderBy(u => u.Date)
            .Select(u => new
            {
                u.Date,
                u.TotalRequests,
                u.SuccessfulRequests,
                u.FailedRequests,
                u.AvgDurationMs
            })
            .ToList();

        return Ok(filtered);
    }

    [HttpGet("usage/summary")]
    public async Task<IActionResult> GetUsageSummary([FromQuery] string from, [FromQuery] string to, CancellationToken ct)
    {
        var tenantId = _ctx.TenantId ?? Guid.Empty;
        var fromDate = DateOnly.Parse(from);
        var toDate = DateOnly.Parse(to);

        var all = await _usage.FindAsync(u => u.TenantId == tenantId, ct);
        var filtered = all.Where(u => u.Date >= fromDate && u.Date <= toDate).ToList();

        var totalRequests = filtered.Sum(u => u.TotalRequests);
        var totalSuccessful = filtered.Sum(u => u.SuccessfulRequests);
        var totalFailed = filtered.Sum(u => u.FailedRequests);
        var days = filtered.Count > 0 ? filtered.Count : 1;

        return Ok(new
        {
            TotalRequests = totalRequests,
            AvgDaily = Math.Round((double)totalRequests / days),
            TotalSuccessful = totalSuccessful,
            TotalFailed = totalFailed,
            AvgResponseTimeMs = filtered.Count > 0 ? Math.Round(filtered.Average(u => u.AvgDurationMs), 1) : 0
        });
    }

    // ── Plans ────────────────────────────────────────────────────────

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
    {
        var plans = await _plans.GetAllAsync(ct);
        var result = plans
            .Where(p => p.Type != PlanType.Internal)
            .OrderBy(p => p.MonthlyPrice)
            .Select(p => new
            {
                p.PlanId,
                p.Name,
                p.RateLimit,
                p.DailyQuota,
                p.MaxApiKeys,
                p.MonthlyPrice,
                p.OverageRatePer1k,
                Features = p.FeaturesJson
            })
            .ToList();

        return Ok(result);
    }

    [HttpPost("plans/change")]
    public async Task<IActionResult> ChangePlan([FromBody] PlanChangeRequest request, CancellationToken ct)
    {
        var tenantId = _ctx.TenantId ?? Guid.Empty;
        var tenant = await _tenants.GetByIdAsync(tenantId, ct);
        if (tenant is null) return NotFound();

        var newPlan = await _plans.GetByIdAsync(request.NewPlanId, ct);
        if (newPlan is null) return BadRequest(new { error = "Plan not found" });

        var currentPlan = await _plans.GetByIdAsync(tenant.PlanId, ct);
        var isUpgrade = newPlan.MonthlyPrice > (currentPlan?.MonthlyPrice ?? 0);

        tenant.AssignPlan(newPlan.PlanId);
        await _tenants.UpdateAsync(tenant, ct);
        await _tenants.SaveChangesAsync(ct);

        return Ok(new
        {
            Type = isUpgrade ? "Upgrade" : "Downgrade",
            EffectiveAt = isUpgrade ? DateTime.UtcNow : DateTime.UtcNow.AddMonths(1)
        });
    }

    // ── Billing ──────────────────────────────────────────────────────

    [HttpGet("billing/summary")]
    public async Task<IActionResult> GetBillingSummary(CancellationToken ct)
    {
        var tenantId = _ctx.TenantId ?? Guid.Empty;
        var tenant = await _tenants.GetByIdAsync(tenantId, ct);
        var plan = await _plans.GetByIdAsync(tenant.PlanId, ct);
        var invoices = await _invoices.FindAsync(i => i.TenantId == tenantId, ct);
        var latest = invoices.OrderByDescending(i => i.CreatedAt).FirstOrDefault();

        return Ok(new
        {
            CurrentPlan = plan?.Name ?? "Unknown",
            CurrentMonthCharges = latest?.TotalAmount ?? 0m,
            NextInvoiceDate = (DateOnly?)DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1).Date),
            PaymentMethodLast4 = "4242"
        });
    }

    [HttpGet("billing/invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var tenantId = _ctx.TenantId ?? Guid.Empty;
        var all = await _invoices.FindAsync(i => i.TenantId == tenantId, ct);
        var sorted = all.OrderByDescending(i => i.CreatedAt).ToList();
        var paged = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var items = paged.Select(i => new
        {
            i.InvoiceId,
            PeriodStart = i.BillingPeriodStart,
            PeriodEnd = i.BillingPeriodEnd,
            i.BaseAmount,
            i.OverageAmount,
            i.TotalAmount,
            i.Status,
            i.CreatedAt
        }).ToList();

        return Ok(new { Items = items, TotalCount = sorted.Count, Page = page, PageSize = pageSize });
    }

    // ── Tenant Profile ───────────────────────────────────────────────

    [HttpGet("tenants/me")]
    public async Task<IActionResult> GetCurrentTenant(CancellationToken ct)
    {
        var tenantId = _ctx.TenantId ?? Guid.Empty;
        var tenant = await _tenants.GetByIdAsync(tenantId, ct);
        if (tenant is null) return NotFound();

        var plan = await _plans.GetByIdAsync(tenant.PlanId, ct);

        return Ok(new
        {
            tenant.TenantId,
            tenant.Name,
            ContactEmail = tenant.ContactEmail?.Value ?? "",
            CompanyName = tenant.CompanyName,
            PlanName = plan?.Name ?? "Unknown",
            Status = tenant.Status.ToString(),
            tenant.CreatedAt
        });
    }

    [HttpPut("tenants/me/profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        // Profile update simplified for demo — actual impl would use domain methods
        return NoContent();
    }

    [HttpPut("tenants/me/notifications")]
    public async Task<IActionResult> UpdateNotifications([FromBody] object request, CancellationToken ct)
    {
        // Notifications preferences stored per-tenant (simplified for demo)
        return NoContent();
    }

    // ── Support ──────────────────────────────────────────────────────

    [HttpPost("support/tickets")]
    public IActionResult SubmitTicket([FromBody] object request)
    {
        return Ok(new { TicketReference = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}" });
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────

public sealed record GenerateKeyRequest(string? Name, int? ExpirationDays);
public sealed record PlanChangeRequest(Guid NewPlanId);
public sealed record UpdateProfileRequest(string Name, string ContactEmail, string? CompanyName);
