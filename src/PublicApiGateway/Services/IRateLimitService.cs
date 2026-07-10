using PublicApiGateway.Models;

namespace PublicApiGateway.Services;

public interface IRateLimitService
{
    Task<RateLimitResult> CheckAsync(string tenantId, PlanLimits limits, CancellationToken ct);
}
