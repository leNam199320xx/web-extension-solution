namespace PluginRuntime.Api.Modules.Subscriptions.Domain;

/// <summary>
/// A tenant's subscription to a plugin package.
/// Tracks status, Stripe billing item, and lifecycle dates.
/// </summary>
public sealed class PackageSubscription
{
    public Guid SubscriptionId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid PackageId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public string? StripeSubscriptionItemId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private PackageSubscription() { }

    public static PackageSubscription Create(Guid tenantId, Guid packageId, string? stripeItemId)
    {
        var now = DateTime.UtcNow;
        return new PackageSubscription
        {
            SubscriptionId = Guid.NewGuid(),
            TenantId = tenantId,
            PackageId = packageId,
            Status = SubscriptionStatus.Active,
            StripeSubscriptionItemId = stripeItemId,
            StartDate = now,
            EndDate = null,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Cancel()
    {
        Status = SubscriptionStatus.Cancelled;
        EndDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStripeItemId(string itemId)
    {
        StripeSubscriptionItemId = itemId;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum SubscriptionStatus
{
    Active,
    Cancelled,
    Expired
}
