namespace PublicApiGateway.Models;

/// <summary>
/// Validated API key information retrieved from cache or database.
/// </summary>
public sealed record ApiKeyInfo(
    Guid KeyId,
    Guid TenantId,
    string TenantName,
    PlanType PlanType,
    ApiKeyStatus Status,
    DateTime? ExpiresAt,
    PlanLimits Limits);
