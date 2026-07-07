using System.Text.Json;
using PluginRuntime.Capabilities.Abstractions;

namespace PluginRuntime.Capabilities.Cache;

/// <summary>
/// Provides controlled cache access for plugins with key namespacing,
/// key count limits, value size limits, and System.Text.Json serialization.
/// </summary>
public class CacheCapability : ICacheCapability
{
    private readonly Guid _pluginId;
    private readonly ICacheBackend _backend;
    private readonly int _maxKeyCount;
    private const int MaxValueSizeBytes = 1 * 1024 * 1024; // 1 MB

    public string Name => "cache";
    public string Version => "1.0";

    public CacheCapability(Guid pluginId, ICacheBackend backend, int maxKeyCount = 10000)
    {
        _pluginId = pluginId;
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _maxKeyCount = maxKeyCount;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        var namespacedKey = GetNamespacedKey(key);
        var bytes = await _backend.GetAsync(namespacedKey, cancellationToken);
        if (bytes is null || bytes.Length == 0)
            return default;

        return JsonSerializer.Deserialize<T>(bytes);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);

        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);

        if (bytes.Length > MaxValueSizeBytes)
            throw new InvalidOperationException(
                $"Serialized value size ({bytes.Length} bytes) exceeds maximum of {MaxValueSizeBytes} bytes (1 MB).");

        var namespacedKey = GetNamespacedKey(key);
        var pluginPrefix = GetPluginPrefix();

        // Only check key count limit if this is a new key (not overwriting existing)
        var exists = await _backend.ExistsAsync(namespacedKey, cancellationToken);
        if (!exists)
        {
            var currentCount = await _backend.GetKeyCountAsync(pluginPrefix, cancellationToken);
            if (currentCount >= _maxKeyCount)
                throw new InvalidOperationException(
                    $"Cache key count ({currentCount}) has reached the maximum limit of {_maxKeyCount} for plugin '{_pluginId}'.");
        }

        await _backend.SetAsync(namespacedKey, bytes, expiration, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        var namespacedKey = GetNamespacedKey(key);
        await _backend.RemoveAsync(namespacedKey, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        var namespacedKey = GetNamespacedKey(key);
        return await _backend.ExistsAsync(namespacedKey, cancellationToken);
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key must not be null or empty.", nameof(key));
    }

    private string GetNamespacedKey(string key) => $"{_pluginId}:{key}";
    private string GetPluginPrefix() => $"{_pluginId}:";
}
