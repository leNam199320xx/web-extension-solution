using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Capabilities.Abstractions;

public interface IStorageCapability : ICapability
{
    /// <summary>
    /// Store a blob with a given key.
    /// </summary>
    Task StoreAsync(
        string key,
        ReadOnlyMemory<byte> data,
        StorageMetadata? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a blob by key.
    /// </summary>
    Task<ReadOnlyMemory<byte>?> RetrieveAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a blob by key.
    /// </summary>
    Task<bool> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List keys with optional prefix filter.
    /// </summary>
    Task<IReadOnlyList<string>> ListKeysAsync(
        string? prefix = null,
        CancellationToken cancellationToken = default);
}
