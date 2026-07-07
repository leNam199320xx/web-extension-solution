namespace PluginRuntime.Capabilities.Storage;

/// <summary>
/// Abstraction for the underlying storage implementation.
/// Enables pluggable backends (file system, object storage, etc.)
/// while keeping StorageCapability focused on policy enforcement.
/// </summary>
public interface IStorageBackend
{
    /// <summary>
    /// Store data at the given namespaced key.
    /// </summary>
    Task StoreAsync(string namespacedKey, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve data by namespaced key. Returns null if not found.
    /// </summary>
    Task<ReadOnlyMemory<byte>?> RetrieveAsync(string namespacedKey, CancellationToken cancellationToken);

    /// <summary>
    /// Delete data by namespaced key. Returns true if deleted, false if not found.
    /// </summary>
    Task<bool> DeleteAsync(string namespacedKey, CancellationToken cancellationToken);

    /// <summary>
    /// List all keys matching the given prefix.
    /// </summary>
    Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken);

    /// <summary>
    /// Get the total bytes used by all objects under the given plugin prefix.
    /// </summary>
    Task<long> GetTotalUsageAsync(string pluginPrefix, CancellationToken cancellationToken);
}
