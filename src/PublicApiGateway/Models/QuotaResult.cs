namespace PublicApiGateway.Models;

/// <summary>
/// Result of a daily quota check.
/// </summary>
public sealed record QuotaResult(
    bool IsAllowed,
    long CurrentCount,
    int DailyLimit,
    int RetryAfterSeconds);
