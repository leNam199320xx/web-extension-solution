using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Capabilities.Abstractions;

public interface ICacheCapability : ICapability
{
    /// <summary>
    /// Get a cached value by key.
    /// </summary>
    Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Set a cached value with optional expiration.
    /// </summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a cached value.
    /// </summary>
    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a key exists in cache.
    /// </summary>
    Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default);
}
