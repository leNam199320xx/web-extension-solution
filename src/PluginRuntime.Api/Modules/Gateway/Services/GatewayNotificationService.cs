using System.Text.Json;
using PluginRuntime.Api.Modules.Gateway.Domain;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Interfaces;
using StackExchange.Redis;

namespace PluginRuntime.Api.Modules.Gateway.Services;

/// <summary>
/// Publishes cache-invalidation notifications to Redis pub/sub channels.
/// Uses IRepository for persisting failed notifications.
/// </summary>
public sealed class GatewayNotificationService : IGatewayNotificationService
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly IRepository<FailedNotification> _failedNotifications;
    private readonly ILogger<GatewayNotificationService> _logger;

    public GatewayNotificationService(
        IConnectionMultiplexer redis,
        IRepository<FailedNotification> failedNotifications,
        ILogger<GatewayNotificationService> logger)
    {
        _redis = redis;
        _failedNotifications = failedNotifications;
        _logger = logger;
    }

    public async Task PublishPlanChangedAsync(PlanChanged evt, CancellationToken ct)
    {
        var payload = new { tenantId = evt.TenantId, planId = evt.NewPlanId, rateLimit = evt.NewRateLimit, dailyQuota = evt.NewDailyQuota, status = "active", version = evt.Version };
        await PublishWithRetryAsync("tenant:plan-changed", payload, ct);
    }

    public async Task PublishTenantStatusChangedAsync(TenantStatusChanged evt, CancellationToken ct)
    {
        var payload = new { tenantId = evt.TenantId, status = evt.NewStatus, version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
        await PublishWithRetryAsync("tenant:status-changed", payload, ct);
    }

    public async Task PublishKeyRevokedAsync(KeyRevoked evt, CancellationToken ct)
    {
        var payload = new { tenantId = evt.TenantId, keyId = evt.KeyId, keyHash = evt.KeyHash, version = evt.Version };
        await PublishWithRetryAsync("tenant:key-revoked", payload, ct);
    }

    public async Task PublishAccessChangedAsync(Guid tenantId, IReadOnlyList<Guid> pluginIds, long version, CancellationToken ct)
    {
        var payload = new { tenantId, pluginIds, version };
        await PublishWithRetryAsync("tenant:access-changed", payload, ct);
    }

    private async Task PublishWithRetryAsync(string channel, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var subscriber = _redis.GetSubscriber();
                await subscriber.PublishAsync(RedisChannel.Literal(channel), json);
                _logger.LogInformation("Published notification to {Channel}", channel);
                return;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis publish attempt {Attempt}/{MaxRetries} failed for channel {Channel}", attempt, MaxRetries, channel);
                if (attempt < MaxRetries) await Task.Delay(RetryInterval, ct);
            }
        }

        _logger.LogError("Redis unavailable after {MaxRetries} attempts for channel {Channel}. Persisting failed notification.", MaxRetries, channel);
        var failedNotification = FailedNotification.Create(channel, json, MaxRetries);
        await _failedNotifications.AddAsync(failedNotification, ct);
        await _failedNotifications.SaveChangesAsync(ct);
    }
}
