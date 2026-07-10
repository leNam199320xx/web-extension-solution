using PublicApiGateway.Models;

namespace PublicApiGateway.Services;

public interface IQuotaService
{
    Task<QuotaResult> IncrementAndCheckAsync(string tenantId, PlanLimits limits, CancellationToken ct);
}
