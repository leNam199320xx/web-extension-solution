using System.Text.Json;
using Microsoft.Extensions.Options;
using PluginRuntime.Core.Interfaces;
using StackExchange.Redis;

namespace PluginRuntime.Infrastructure.Caching;

public class RedisCacheOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public int DefaultTtlSeconds { get; set; } = 300;
    public int MinTtlSeconds { get; set; } = 10;
    public int MaxTtlSeconds { get; set; } = 86400;
}

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisCacheOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IConnectionMultiplexer redis, IOptions<RedisCacheOptions> options)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(key);

        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken cancellationToken) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(value);

        var ttl = expiration ?? TimeSpan.FromSeconds(_options.DefaultTtlSeconds);

        // Clamp TTL to configured range
        var totalSeconds = (int)ttl.TotalSeconds;
        totalSeconds = Math.Clamp(totalSeconds, _options.MinTtlSeconds, _options.MaxTtlSeconds);
        ttl = TimeSpan.FromSeconds(totalSeconds);

        var json = JsonSerializer.Serialize(value, _jsonOptions);
        var db = _redis.GetDatabase();
        await db.StringSetAsync(key, json, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);
    }
}
