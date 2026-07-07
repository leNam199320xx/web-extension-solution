namespace PluginRuntime.Core.Interfaces;

public record HealthCheckResult(bool IsHealthy, IReadOnlyDictionary<string, HealthCheckEntry> Checks);
public record HealthCheckEntry(string Status, string? Error);

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken);
    Task<HealthCheckResult> CheckReadinessAsync(CancellationToken cancellationToken);
}
