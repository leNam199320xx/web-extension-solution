namespace PluginRuntime.Api.Modules.Subscriptions.DTOs;

/// <summary>
/// Result of a plan change operation indicating type, effective date, and optional proration.
/// </summary>
public sealed record PlanChangeResult(string Type, DateTime EffectiveAt, decimal? ProratedAmount);
