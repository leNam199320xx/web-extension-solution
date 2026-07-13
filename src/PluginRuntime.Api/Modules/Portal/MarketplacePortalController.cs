using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Portal;

/// <summary>
/// Unified controller for Marketplace Portal frontend.
/// </summary>
[ApiController]
[Route("api")]
public sealed class MarketplacePortalController : ControllerBase
{
    private readonly IRepository<Extension> _extensions;
    private readonly IRepository<Subscription> _subscriptions;
    private readonly IRepository<EcosystemStats> _stats;
    private readonly ICurrentTenantContext _ctx;

    public MarketplacePortalController(
        IRepository<Extension> extensions,
        IRepository<Subscription> subscriptions,
        IRepository<EcosystemStats> stats,
        ICurrentTenantContext ctx)
    {
        _extensions = extensions;
        _subscriptions = subscriptions;
        _stats = stats;
        _ctx = ctx;
    }

    // ── Extensions ───────────────────────────────────────────────────

    [HttpGet("extensions")]
    public async Task<IActionResult> GetExtensions(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? riskLevel,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var all = await _extensions.GetAllAsync(ct);
        var filtered = all.Where(e => e.Status == "Active").AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            filtered = filtered.Where(e =>
                e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.ShortDescription.Contains(q, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(category))
            filtered = filtered.Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(riskLevel))
            filtered = filtered.Where(e => string.Equals(e.RiskLevel, riskLevel, StringComparison.OrdinalIgnoreCase));

        var list = filtered.ToList();
        var paged = list.Skip((page - 1) * pageSize).Take(pageSize).Select(e => new
        {
            e.ExtensionId, e.Name, e.Author, e.Category, e.LatestVersion,
            e.RiskLevel, e.ShortDescription, e.SubscriberCount
        }).ToList();

        return Ok(new { Items = paged, TotalCount = list.Count, Page = page, PageSize = pageSize });
    }

    [HttpGet("extensions/featured")]
    public async Task<IActionResult> GetFeatured(CancellationToken ct)
    {
        var all = await _extensions.GetAllAsync(ct);
        var featured = all
            .Where(e => e.Status == "Active")
            .OrderByDescending(e => e.SubscriberCount)
            .Take(6)
            .Select(e => new
            {
                e.ExtensionId, e.Name, e.Author, e.Category, e.LatestVersion,
                e.RiskLevel, e.ShortDescription, e.SubscriberCount
            })
            .ToList();

        return Ok(featured);
    }

    [HttpGet("extensions/stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var all = await _extensions.GetAllAsync(ct);
        var subs = await _subscriptions.GetAllAsync(ct);

        return Ok(new
        {
            TotalExtensions = all.Count(e => e.Status == "Active"),
            TotalPublishers = all.Select(e => e.PublisherId).Distinct().Count(),
            TotalSubscriptions = subs.Count(s => s.Status == "Approved")
        });
    }

    [HttpGet("extensions/{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        var ext = await _extensions.GetByIdAsync(id, ct);
        if (ext is null) return NotFound();

        return Ok(new
        {
            ext.ExtensionId,
            ext.Name,
            ext.Author,
            ext.Category,
            ext.LatestVersion,
            ext.RiskLevel,
            Description = ext.ShortDescription + "\n\nThis extension provides powerful capabilities for your applications. Full documentation available in the SDK reference.",
            ReadmeContent = (string?)null,
            Permissions = new
            {
                Groups = new[]
                {
                    new
                    {
                        RiskLevel = ext.RiskLevel,
                        Permissions = new[]
                        {
                            new { Scope = "database.read", MappedCapability = "IDatabaseCapability", RiskLevel = "Low", Justification = "Read operational data" },
                            new { Scope = "network.http", MappedCapability = "INetworkCapability", RiskLevel = "Medium", Justification = "Call external APIs" }
                        }
                    }
                }
            },
            Versions = new[]
            {
                new { Version = ext.LatestVersion, Status = "Approved", CreatedAt = ext.CreatedAt, RejectionReason = (string?)null },
                new { Version = "1.0.0", Status = "Approved", CreatedAt = ext.CreatedAt.AddMonths(-3), RejectionReason = (string?)null }
            },
            ext.Visibility,
            ext.SubscriberCount,
            ext.CreatedAt
        });
    }

    [HttpGet("extensions/{id:guid}/versions")]
    public async Task<IActionResult> GetVersions(Guid id, CancellationToken ct)
    {
        var ext = await _extensions.GetByIdAsync(id, ct);
        if (ext is null) return NotFound();

        return Ok(new[]
        {
            new { Version = ext.LatestVersion, Status = "Approved", CreatedAt = ext.CreatedAt, RejectionReason = (string?)null },
            new { Version = "1.0.0", Status = "Approved", CreatedAt = ext.CreatedAt.AddMonths(-3), RejectionReason = (string?)null }
        });
    }

    [HttpGet("extensions/mine")]
    public async Task<IActionResult> GetMyExtensions(CancellationToken ct)
    {
        var tenantId = _ctx.TenantId ?? Guid.Empty;
        var all = await _extensions.GetAllAsync(ct);
        var mine = all
            .Where(e => e.PublisherId == tenantId)
            .Select(e => new
            {
                e.ExtensionId, e.Name, e.Author, e.Category, e.LatestVersion,
                e.RiskLevel, e.ShortDescription, e.SubscriberCount
            })
            .ToList();

        return Ok(mine);
    }

    // ── Subscriptions ────────────────────────────────────────────────

    [HttpGet("subscriptions/outgoing")]
    public async Task<IActionResult> GetOutgoing(CancellationToken ct)
    {
        var tenantId = _ctx.TenantId ?? Guid.Empty;
        var all = await _subscriptions.GetAllAsync(ct);
        var outgoing = all
            .Where(s => s.RequesterId == tenantId)
            .Select(s => new
            {
                s.SubscriptionId,
                s.TargetExtensionId,
                s.TargetExtensionName,
                s.Status,
                s.RequestedAt,
                s.Reason,
                s.ExpectedUsage
            })
            .ToList();

        return Ok(outgoing);
    }

    [HttpGet("subscriptions/incoming")]
    public async Task<IActionResult> GetIncoming(CancellationToken ct)
    {
        var tenantId = _ctx.TenantId ?? Guid.Empty;
        var all = await _extensions.GetAllAsync(ct);
        var myExtIds = all.Where(e => e.PublisherId == tenantId).Select(e => e.ExtensionId).ToHashSet();

        var subs = await _subscriptions.GetAllAsync(ct);
        var incoming = subs
            .Where(s => myExtIds.Contains(s.TargetExtensionId))
            .Select(s => new
            {
                s.SubscriptionId,
                s.TargetExtensionId,
                s.TargetExtensionName,
                s.Status,
                s.RequestedAt,
                s.Reason,
                s.ExpectedUsage
            })
            .ToList();

        return Ok(incoming);
    }

    [HttpPost("subscriptions")]
    public async Task<IActionResult> RequestSubscription([FromBody] SubscriptionRequest request, CancellationToken ct)
    {
        var sub = new Subscription
        {
            SubscriptionId = Guid.NewGuid(),
            RequesterId = _ctx.TenantId ?? Guid.Empty,
            RequesterName = _ctx.TenantName ?? "Unknown",
            TargetExtensionId = request.TargetExtensionId,
            TargetExtensionName = "Extension",
            Status = "Pending",
            RequestedAt = DateTime.UtcNow,
            Reason = request.Reason,
            ExpectedUsage = request.ExpectedUsage
        };

        // Resolve target name
        var ext = await _extensions.GetByIdAsync(request.TargetExtensionId, ct);
        if (ext is not null) sub.TargetExtensionName = ext.Name;

        await _subscriptions.AddAsync(sub, ct);
        await _subscriptions.SaveChangesAsync(ct);

        return Ok(new { sub.SubscriptionId, Status = "Pending" });
    }

    [HttpPost("subscriptions/{id:guid}/decide")]
    public async Task<IActionResult> DecideSubscription(Guid id, [FromBody] SubscriptionDecision decision, CancellationToken ct)
    {
        var sub = await _subscriptions.GetByIdAsync(id, ct);
        if (sub is null) return NotFound();

        sub.Status = decision.Approved ? "Approved" : "Rejected";
        sub.DecidedAt = DateTime.UtcNow;
        await _subscriptions.UpdateAsync(sub, ct);
        await _subscriptions.SaveChangesAsync(ct);

        return Ok(new { sub.SubscriptionId, sub.Status });
    }

    // ── Plugin Upload ────────────────────────────────────────────────

    [HttpPost("plugins/upload")]
    [RequestSizeLimit(52_428_800)] // 50MB
    public IActionResult UploadPlugin()
    {
        // In demo mode, just return success
        return Ok(new
        {
            PluginVersionId = Guid.NewGuid(),
            Status = "PendingReview",
            Message = "Plugin uploaded successfully. Awaiting admin review."
        });
    }

    [HttpGet("plugins/mine")]
    public async Task<IActionResult> GetMyPlugins(CancellationToken ct)
    {
        // Alias for extensions/mine
        return await GetMyExtensions(ct);
    }

    // ── Profile ──────────────────────────────────────────────────────

    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        return Ok(new
        {
            DisplayName = _ctx.TenantName ?? "User",
            Email = "user@example.com",
            PublisherDescription = "Plugin developer and API consumer."
        });
    }

    [HttpPut("profile")]
    public IActionResult UpdateProfile([FromBody] object request)
    {
        return NoContent();
    }

    [HttpGet("profile/keys")]
    public IActionResult GetProfileKeys()
    {
        return Ok(new { Keys = new List<object>() });
    }

    [HttpPost("profile/keys")]
    public IActionResult GenerateProfileKey([FromBody] object request)
    {
        return Ok(new
        {
            KeyId = Guid.NewGuid(),
            Name = "Marketplace Key",
            Prefix = $"mk_{Random.Shared.Next(1000, 9999)}",
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = (DateTime?)null
        });
    }

    [HttpDelete("profile/keys/{id:guid}")]
    public IActionResult RevokeProfileKey(Guid id)
    {
        return NoContent();
    }

    // ── Publishers ───────────────────────────────────────────────────

    [HttpGet("publishers/{id:guid}")]
    public IActionResult GetPublisher(Guid id)
    {
        return Ok(new
        {
            PublisherId = id,
            DisplayName = "Publisher",
            Description = "Extension developer",
            JoinedAt = DateTime.UtcNow.AddYears(-1),
            Extensions = new List<object>()
        });
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────

public sealed record SubscriptionRequest(Guid TargetExtensionId, string Reason, ExpectedUsageDto ExpectedUsage);
public sealed record ExpectedUsageDto(int RequestsPerDay, string UsagePattern);
public sealed record SubscriptionDecision(Guid SubscriptionId, bool Approved, string? Reason, DateTime? ExpiresAt);
