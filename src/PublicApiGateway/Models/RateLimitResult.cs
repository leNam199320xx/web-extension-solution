namespace PublicApiGateway.Models;

/// <summary>
/// Result of a rate limit check.
/// </summary>
public sealed record RateLimitResult(
    bool IsAllowed,
    int Limit,
    int Remaining,
    long ResetAtUnixSeconds);
