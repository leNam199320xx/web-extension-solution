using Microsoft.Extensions.Options;
using PublicApiGateway.Configuration;
using StackExchange.Redis;

namespace PublicApiGateway.Services;

/// <summary>
/// IP blocking via Redis. Gracefully allows requests when Redis is unavailable.
/// </summary>
public sealed class IpBlockingService : IIpBlockingService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly GatewayOptions _options;
    private readonly ILogger<IpBlockingService> _logger;

    public IpBlockingService(IConnectionMultiplexer redis, IOptions<GatewayOptions> options, ILogger<IpBlockingService> logger)
    {
        _redis = redis;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> IsBlockedAsync(string ipAddress, CancellationToken ct)
    {
        try
        {
            if (!_redis.IsConnected) return false;
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync($"gw:ipblock:{ipAddress}");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Redis unavailable for IP block check — allowing request");
            return false;
        }
    }

    public async Task RecordFailedAttemptAsync(string ipAddress, CancellationToken ct)
    {
        try
        {
            if (!_redis.IsConnected) return;
            var db = _redis.GetDatabase();
            var key = $"gw:ipattempts:{ipAddress}";
            var count = await db.StringIncrementAsync(key);
            if (count == 1)
                await db.KeyExpireAsync(key, TimeSpan.FromSeconds(_options.IpBlockWindowSeconds));
            if (count > _options.IpBlockThreshold)
                await db.StringSetAsync($"gw:ipblock:{ipAddress}", "blocked", TimeSpan.FromSeconds(_options.IpBlockDurationSeconds));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Redis unavailable for recording failed attempt — skipping");
        }
    }
}
