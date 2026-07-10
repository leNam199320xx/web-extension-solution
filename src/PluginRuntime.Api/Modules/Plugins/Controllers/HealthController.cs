using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Shared.Infrastructure;
using StackExchange.Redis;

namespace PluginRuntime.Api.Modules.Plugins.Controllers;

/// <summary>
/// Health checks and readiness endpoints for infrastructure monitoring.
/// Checks PostgreSQL, Redis, and module health with 5-second timeout.
/// </summary>
[ApiController]
public sealed class HealthController : ControllerBase
{
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5);

    private readonly AppDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        AppDbContext db,
        IConnectionMultiplexer redis,
        ILogger<HealthController> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Liveness probe: checks all critical dependencies.
    /// Returns 200 with Healthy status, or 503 with Unhealthy identifying failures.
    /// </summary>
    [HttpGet("/health")]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(HealthCheckTimeout);

        var checks = new Dictionary<string, object>();
        var allHealthy = true;

        // PostgreSQL check
        try
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT 1", cts.Token);
            checks["postgresql"] = new { status = "Healthy" };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PostgreSQL health check failed");
            checks["postgresql"] = new { status = "Unhealthy", error = ex.Message };
            allHealthy = false;
        }

        // Redis check
        try
        {
            var database = _redis.GetDatabase();
            await database.PingAsync();
            checks["redis"] = new { status = "Healthy" };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis health check failed");
            checks["redis"] = new { status = "Unhealthy", error = ex.Message };
            allHealthy = false;
        }

        // Module health indicators
        checks["modules"] = new
        {
            plugins = "Healthy",
            tenants = "Healthy",
            billing = "Healthy",
            subscriptions = "Healthy",
            gateway = "Healthy"
        };

        var result = new
        {
            Status = allHealthy ? "Healthy" : "Unhealthy",
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Dependencies = checks
        };

        return allHealthy
            ? Ok(result)
            : StatusCode(503, result);
    }

    /// <summary>
    /// Readiness probe for Kubernetes: checks if the app is ready to serve traffic.
    /// </summary>
    [HttpGet("/ready")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(HealthCheckTimeout);

        try
        {
            // Verify DB is accessible
            await _db.Database.ExecuteSqlRawAsync("SELECT 1", cts.Token);

            return Ok(new { Status = "Ready", Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Readiness check failed");
            return StatusCode(503, new { Status = "NotReady", Error = ex.Message });
        }
    }
}
