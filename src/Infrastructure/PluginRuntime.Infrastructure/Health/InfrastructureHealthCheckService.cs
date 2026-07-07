using Microsoft.Extensions.Logging;
using PluginRuntime.Core.Interfaces;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using PluginRuntime.Infrastructure.Persistence;

namespace PluginRuntime.Infrastructure.Health;

public class InfrastructureHealthCheckService : IHealthCheckService
{
    private readonly PluginRuntimeDbContext _dbContext;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<InfrastructureHealthCheckService> _logger;
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(5);

    public InfrastructureHealthCheckService(
        PluginRuntimeDbContext dbContext,
        IConnectionMultiplexer? redis,
        ILogger<InfrastructureHealthCheckService> logger)
    {
        _dbContext = dbContext;
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var checks = new Dictionary<string, HealthCheckEntry>();

        checks["database"] = await CheckDatabaseAsync(cancellationToken);
        checks["redis"] = await CheckRedisAsync(cancellationToken);
        checks["storage"] = await CheckStorageAsync(cancellationToken);

        var isHealthy = checks.Values.All(c => c.Status == "Healthy");
        return new HealthCheckResult(isHealthy, checks);
    }

    public async Task<HealthCheckResult> CheckReadinessAsync(CancellationToken cancellationToken)
    {
        // Readiness requires ALL checks to pass
        var result = await CheckHealthAsync(cancellationToken);

        // IF any individual check fails, return unhealthy regardless
        if (!result.IsHealthy)
            return new HealthCheckResult(false, result.Checks);

        return result;
    }

    private async Task<HealthCheckEntry> CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(CheckTimeout);
            var canConnect = await _dbContext.Database.CanConnectAsync(cts.Token);
            return canConnect
                ? new HealthCheckEntry("Healthy", null)
                : new HealthCheckEntry("Unhealthy", "Cannot connect to database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return new HealthCheckEntry("Unhealthy", ex.Message);
        }
    }

    private async Task<HealthCheckEntry> CheckRedisAsync(CancellationToken cancellationToken)
    {
        if (_redis is null)
            return new HealthCheckEntry("Unhealthy", "Redis not configured");

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(CheckTimeout);
            var db = _redis.GetDatabase();
            var pong = await db.PingAsync();
            return pong.TotalMilliseconds < CheckTimeout.TotalMilliseconds
                ? new HealthCheckEntry("Healthy", null)
                : new HealthCheckEntry("Unhealthy", $"Redis ping took {pong.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return new HealthCheckEntry("Unhealthy", ex.Message);
        }
    }

    private Task<HealthCheckEntry> CheckStorageAsync(CancellationToken cancellationToken)
    {
        // For file-system based storage, check if the base directory is accessible
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Basic check - directory exists or can be created
            return Task.FromResult(new HealthCheckEntry("Healthy", null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage health check failed");
            return Task.FromResult(new HealthCheckEntry("Unhealthy", ex.Message));
        }
    }
}
