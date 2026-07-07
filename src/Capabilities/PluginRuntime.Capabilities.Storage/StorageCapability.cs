using PluginRuntime.Capabilities.Abstractions;

namespace PluginRuntime.Capabilities.Storage;

/// <summary>
/// Provides controlled storage access for plugins with key namespacing,
/// size limits, quota enforcement, and path traversal protection.
/// </summary>
public class StorageCapability : IStorageCapability
{
    private readonly Guid _pluginId;
    private readonly IStorageBackend _backend;
    private readonly long _maxObjectSizeBytes;
    private readonly long _maxTotalQuotaBytes;

    public string Name => "storage";
    public string Version => "1.0";

    private const long DefaultMaxObjectSize = 50L * 1024 * 1024; // 50 MB
    private const long DefaultMaxQuota = 500L * 1024 * 1024; // 500 MB default quota

    public StorageCapability(
        Guid pluginId,
        IStorageBackend backend,
        long? maxObjectSizeBytes = null,
        long? maxTotalQuotaBytes = null)
    {
        _pluginId = pluginId;
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _maxObjectSizeBytes = maxObjectSizeBytes ?? DefaultMaxObjectSize;
        _maxTotalQuotaBytes = maxTotalQuotaBytes ?? DefaultMaxQuota;
    }

    public async Task StoreAsync(
        string key,
        ReadOnlyMemory<byte> data,
        StorageMetadata? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);

        if (data.Length > _maxObjectSizeBytes)
            throw new InvalidOperationException(
                $"Object size ({data.Length} bytes) exceeds maximum of {_maxObjectSizeBytes} bytes.");

        // Check total quota
        var prefix = GetPluginPrefix();
        var currentUsage = await _backend.GetTotalUsageAsync(prefix, cancellationToken);
        if (currentUsage + data.Length > _maxTotalQuotaBytes)
            throw new InvalidOperationException(
                $"Storage quota exceeded. Current: {currentUsage}, Adding: {data.Length}, Limit: {_maxTotalQuotaBytes}.");

        var namespacedKey = GetNamespacedKey(key);
        await _backend.StoreAsync(namespacedKey, data, cancellationToken);
    }

    public async Task<ReadOnlyMemory<byte>?> RetrieveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        var namespacedKey = GetNamespacedKey(key);
        return await _backend.RetrieveAsync(namespacedKey, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        var namespacedKey = GetNamespacedKey(key);
        return await _backend.DeleteAsync(namespacedKey, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        if (prefix is not null)
            ValidateKey(prefix);

        var namespacedPrefix = prefix is not null
            ? GetNamespacedKey(prefix)
            : GetPluginPrefix();

        var keys = await _backend.ListKeysAsync(namespacedPrefix, cancellationToken);

        // Strip the plugin prefix from returned keys for the caller
        var pluginPrefix = GetPluginPrefix();
        return keys
            .Select(k => k.StartsWith(pluginPrefix, StringComparison.Ordinal)
                ? k[pluginPrefix.Length..]
                : k)
            .ToList();
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Storage key must not be null or empty.", nameof(key));

        if (key.Contains("../", StringComparison.Ordinal) || key.Contains(@"..\", StringComparison.Ordinal))
            throw new InvalidOperationException(
                @"Path traversal sequences (../, ..\) are not allowed in storage keys.");
    }

    private string GetNamespacedKey(string key) => $"{_pluginId}/{key}";
    private string GetPluginPrefix() => $"{_pluginId}/";
}
