using PluginRuntime.Api.Modules.Subscriptions.Domain;

namespace PluginRuntime.Api.Modules.Subscriptions.DTOs;

/// <summary>
/// Read model for a package subscription.
/// </summary>
public sealed record PackageSubscriptionDto
{
    public Guid SubscriptionId { get; init; }
    public Guid TenantId { get; init; }
    public Guid PackageId { get; init; }
    public SubscriptionStatus Status { get; init; }
    public string? StripeSubscriptionItemId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static PackageSubscriptionDto FromEntity(PackageSubscription entity) => new()
    {
        SubscriptionId = entity.SubscriptionId,
        TenantId = entity.TenantId,
        PackageId = entity.PackageId,
        Status = entity.Status,
        StripeSubscriptionItemId = entity.StripeSubscriptionItemId,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
