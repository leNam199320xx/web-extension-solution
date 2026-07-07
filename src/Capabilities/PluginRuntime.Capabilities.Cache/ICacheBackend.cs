namespace PluginRuntime.Capabilities.Cache;

/// <summary>
/// Backend abstraction for cache storage operations.
/// Implementations may use Redis, in-memory, or other cache providers.
/// </summary>
public interface ICacheBackend
{
    /// <summary>
    /// Get raw bytes for a namespaced key.
    /// </summary>
    Task<byte[]?> GetAsync(string namespacedKey, CancellationToken cancellationToken);

    /// <summary>
    /// Set raw bytes for a namespaced key with optional expiration.
    /// </summary>
    Task SetAsync(string namespacedKey, byte[] value, TimeSpan? expiration, CancellationToken cancellationToken);

    /// <summary>
    /// Remove a namespaced key from cache.
    /// </summary>
    Task RemoveAsync(string namespacedKey, CancellationToken cancellationToken);

    /// <summary>
    /// Check if a namespaced key exists in cache.
    /// </summary>
    Task<bool> ExistsAsync(string namespacedKey, CancellationToken cancellationToken);

    /// <summary>
    /// Get the count of keys for a given plugin prefix.
    /// </summary>
    Task<int> GetKeyCountAsync(string pluginPrefix, CancellationToken cancellationToken);
}
