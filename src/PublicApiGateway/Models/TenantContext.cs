namespace PublicApiGateway.Models;

/// <summary>
/// Resolved tenant context stored in HttpContext.Items after authentication.
/// </summary>
public sealed record TenantContext(
    Guid TenantId,
    string TenantName,
    PlanType PlanType,
    PlanLimits Limits);
