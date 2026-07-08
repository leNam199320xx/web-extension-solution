namespace PluginRuntime.Api.Modules.Billing.Domain;

/// <summary>
/// Daily usage aggregate per tenant. Summarizes total/successful/failed requests and average duration.
/// </summary>
public sealed class UsageAggregate
{
    public Guid AggregateId { get; private set; }
    public Guid TenantId { get; private set; }
    public DateOnly Date { get; private set; }
    public long TotalRequests { get; private set; }
    public long SuccessfulRequests { get; private set; }
    public long FailedRequests { get; private set; }
    public double AvgDurationMs { get; private set; }
    public DateTime AggregatedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private UsageAggregate() { }

    public static UsageAggregate Create(
        Guid tenantId,
        DateOnly date,
        long totalRequests,
        long successfulRequests,
        long failedRequests,
        double avgDurationMs)
    {
        return new UsageAggregate
        {
            AggregateId = Guid.NewGuid(),
            TenantId = tenantId,
            Date = date,
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests,
            AvgDurationMs = avgDurationMs,
            AggregatedAt = DateTime.UtcNow
        };
    }
}
