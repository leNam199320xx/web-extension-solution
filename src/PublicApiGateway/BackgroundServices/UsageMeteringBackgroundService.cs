using Npgsql;
using PublicApiGateway.Models;
using PublicApiGateway.Services;

namespace PublicApiGateway.BackgroundServices;

/// <summary>
/// Background consumer that reads UsageRecords from the channel
/// and batch-persists them to PostgreSQL with retry logic.
/// </summary>
public sealed class UsageMeteringBackgroundService : BackgroundService
{
    private const int BatchSize = 100;
    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays = [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    ];

    private readonly UsageMeteringService _meteringService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UsageMeteringBackgroundService> _logger;

    public UsageMeteringBackgroundService(
        UsageMeteringService meteringService,
        IConfiguration configuration,
        ILogger<UsageMeteringBackgroundService> logger)
    {
        _meteringService = meteringService;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UsageMeteringBackgroundService started");

        var batch = new List<UsageRecord>(BatchSize);

        await foreach (var record in _meteringService.Reader.ReadAllAsync(stoppingToken))
        {
            batch.Add(record);

            if (batch.Count >= BatchSize)
            {
                await PersistBatchAsync(batch, stoppingToken);
                batch.Clear();
            }
        }

        // Flush remaining
        if (batch.Count > 0)
        {
            await PersistBatchAsync(batch, stoppingToken);
        }
    }

    private async Task PersistBatchAsync(List<UsageRecord> batch, CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                await WriteToDatabaseAsync(batch, ct);
                return;
            }
            catch (Exception ex) when (attempt < MaxRetries - 1)
            {
                _logger.LogWarning(ex, "Usage metering persist attempt {Attempt} failed, retrying...", attempt + 1);
                await Task.Delay(RetryDelays[attempt], ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Usage metering failed after {MaxRetries} attempts. Dead-letter: {Count} records for tenants: {Tenants}",
                    MaxRetries,
                    batch.Count,
                    string.Join(", ", batch.Select(r => r.TenantId).Distinct()));
            }
        }
    }

    private async Task WriteToDatabaseAsync(List<UsageRecord> records, CancellationToken ct)
    {
        var connStr = _configuration.GetConnectionString("PostgreSQL") ?? "";
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync(ct);

        await using var transaction = await conn.BeginTransactionAsync(ct);

        foreach (var record in records)
        {
            await using var cmd = new NpgsqlCommand("""
                INSERT INTO usage_records (record_id, tenant_id, method, path, status_code, duration_ms, 
                    request_body_bytes, response_body_bytes, correlation_id, timestamp)
                VALUES (@id, @tenantId, @method, @path, @status, @duration, @reqBytes, @resBytes, @correlationId, @timestamp)
                """, conn, transaction);

            cmd.Parameters.AddWithValue("id", record.RecordId);
            cmd.Parameters.AddWithValue("tenantId", record.TenantId);
            cmd.Parameters.AddWithValue("method", record.Method);
            cmd.Parameters.AddWithValue("path", record.Path);
            cmd.Parameters.AddWithValue("status", record.StatusCode);
            cmd.Parameters.AddWithValue("duration", record.DurationMs);
            cmd.Parameters.AddWithValue("reqBytes", record.RequestBodyBytes);
            cmd.Parameters.AddWithValue("resBytes", record.ResponseBodyBytes);
            cmd.Parameters.AddWithValue("correlationId", record.CorrelationId);
            cmd.Parameters.AddWithValue("timestamp", record.Timestamp);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }
}
