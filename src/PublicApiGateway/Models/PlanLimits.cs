namespace PublicApiGateway.Models;

/// <summary>
/// Rate limit and quota configuration for a plan.
/// Null values indicate unlimited.
/// </summary>
public sealed record PlanLimits(
    int? RateLimit,
    int? DailyQuota,
    int RateLimitWindowSeconds = 60);
