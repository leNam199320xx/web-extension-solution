namespace PluginRuntime.Api.Shared.Entities;

/// <summary>
/// Plan entity defining subscription tiers with rate limits, quotas, and pricing.
/// Plans are managed by Platform Admins and referenced by Tenants.
/// </summary>
public sealed class Plan
{
    public Guid PlanId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PlanType Type { get; private set; }
    public int? RateLimit { get; private set; }
    public int? DailyQuota { get; private set; }
    public int? MaxApiKeys { get; private set; }
    public int? MaxPluginsUpload { get; private set; }
    public int? MaxPackageSubscriptions { get; private set; }
    public decimal MonthlyPrice { get; private set; }
    public decimal? OverageRatePer1k { get; private set; }
    public string? StripePriceId { get; private set; }
    public bool IsBillable { get; private set; }
    public string? FeaturesJson { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private Plan() { }

    public Plan(
        Guid planId,
        string name,
        PlanType type,
        decimal monthlyPrice,
        bool isBillable,
        int? rateLimit = null,
        int? dailyQuota = null,
        int? maxApiKeys = null,
        int? maxPluginsUpload = null,
        int? maxPackageSubscriptions = null,
        decimal? overageRatePer1k = null,
        string? stripePriceId = null,
        string? featuresJson = null)
    {
        PlanId = planId;
        Name = name;
        Type = type;
        MonthlyPrice = monthlyPrice;
        IsBillable = isBillable;
        RateLimit = rateLimit;
        DailyQuota = dailyQuota;
        MaxApiKeys = maxApiKeys;
        MaxPluginsUpload = maxPluginsUpload;
        MaxPackageSubscriptions = maxPackageSubscriptions;
        OverageRatePer1k = overageRatePer1k;
        StripePriceId = stripePriceId;
        FeaturesJson = featuresJson;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks whether the plan has unlimited access for a given limit.
    /// Null values represent unlimited access.
    /// </summary>
    public bool HasUnlimitedRateLimit => RateLimit is null;
    public bool HasUnlimitedDailyQuota => DailyQuota is null;
    public bool HasUnlimitedApiKeys => MaxApiKeys is null;
    public bool HasUnlimitedPluginsUpload => MaxPluginsUpload is null;
    public bool HasUnlimitedPackageSubscriptions => MaxPackageSubscriptions is null;
}
