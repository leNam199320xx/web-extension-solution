namespace PluginRuntime.Api.Modules.Subscriptions.DTOs;

/// <summary>
/// Represents the tenant's current subscription details including plan info and any pending change.
/// </summary>
public sealed record CurrentSubscriptionDto(
    Guid PlanId,
    string PlanName,
    int? RateLimit,
    int? DailyQuota,
    decimal MonthlyPrice,
    Guid? PendingPlanId);
