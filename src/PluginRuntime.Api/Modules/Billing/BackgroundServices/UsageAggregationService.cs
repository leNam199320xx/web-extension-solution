using PluginRuntime.Api.Shared.Infrastructure;

namespace PluginRuntime.Api.Modules.Billing.BackgroundServices;

/// <summary>
/// Background service that aggregates usage records into daily UsageAggregate entries.
/// Runs daily at 01:00 UTC. Since the usage_records table is managed by the Gateway,
/// this service is implemented as a stub until that table is available.
/// </summary>
public sealed class UsageAggregationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UsageAggregationService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(120),
        TimeSpan.FromSeconds(240)
    ];

    public UsageAggregationService(
        IServiceProvider serviceProvider,
        ILogger<UsageAggregationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UsageAggregationService started. Checking hourly for 01:00 UTC window.");

        using var timer = new PeriodicTimer(CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (DateTime.UtcNow.Hour == 1)
                {
                    await RunWithRetryAsync(stoppingToken);

                    // After aggregating, wait until the next hour to avoid re-triggering
                    var nextHour = DateTime.UtcNow.Date.AddHours(2) - DateTime.UtcNow;
                    if (nextHour > TimeSpan.Zero)
                    {
                        await Task.Delay(nextHour, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in UsageAggregationService loop");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("UsageAggregationService stopped.");
    }

    private async Task RunWithRetryAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxRetryAttempts; attempt++)
        {
            try
            {
                await AggregateUsageAsync(ct);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Usage aggregation attempt {Attempt}/{MaxAttempts} failed. Retrying in {Delay}s.",
                    attempt + 1,
                    MaxRetryAttempts,
                    RetryDelays[attempt].TotalSeconds);

                if (attempt < MaxRetryAttempts - 1)
                {
                    await Task.Delay(RetryDelays[attempt], ct);
                }
                else
                {
                    _logger.LogError(
                        ex,
                        "Usage aggregation failed after {MaxAttempts} attempts. Will retry at next scheduled window.",
                        MaxRetryAttempts);
                }
            }
        }
    }

    private async Task AggregateUsageAsync(CancellationToken ct)
    {
        var aggregationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        _logger.LogInformation(
            "Starting usage aggregation for date {AggregationDate}.",
            aggregationDate);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // TODO: Query usage_records table (created by Gateway) for the aggregation date.
        // The usage_records table does not exist yet in this context — it is populated
        // by the Public API Gateway on each request. Once available, the aggregation logic
        // should:
        //   1. SELECT tenant_id, COUNT(*) as total, SUM(CASE WHEN status < 400 THEN 1 ELSE 0 END) as successful,
        //      SUM(CASE WHEN status >= 400 THEN 1 ELSE 0 END) as failed, AVG(duration_ms)
        //      FROM usage_records WHERE date = @aggregationDate GROUP BY tenant_id
        //   2. For each tenant row, create a UsageAggregate via UsageAggregate.Create(...)
        //   3. Add to dbContext.Set<UsageAggregate>() and SaveChangesAsync

        _logger.LogInformation(
            "Usage aggregation stub: would query usage_records for {AggregationDate} and create UsageAggregate entries.",
            aggregationDate);

        // Placeholder: simulate async work
        await Task.CompletedTask;

        _logger.LogInformation(
            "Usage aggregation completed for date {AggregationDate}.",
            aggregationDate);
    }
}
