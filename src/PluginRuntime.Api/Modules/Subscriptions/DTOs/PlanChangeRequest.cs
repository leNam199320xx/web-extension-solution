namespace PluginRuntime.Api.Modules.Subscriptions.DTOs;

/// <summary>
/// Request to change the tenant's current plan.
/// </summary>
public sealed record PlanChangeRequest(Guid NewPlanId);
