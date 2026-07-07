using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Infrastructure.Storage;

/// <summary>
/// Configuration options for the file-system based object storage.
/// </summary>
public class ObjectStorageOptions
{
    /// <summary>
    /// Base path for plugin binary storage.
    /// </summary>
    public string BasePath { get; set; } = "./plugin-storage";

    /// <summary>
    /// Maximum file size in bytes (default: 50 MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 50L * 1024 * 1024;
}

/// <summary>
/// File-system based object storage for plugin binaries.
/// Stores at {basePath}/{pluginId}/{versionId}/ path prefix.
/// Production deployments should replace with cloud object storage (S3, Azure Blob, etc.)
/// </summary>
public class ObjectStorageService : IObjectStorageService, IPluginBinaryStore
{
    private readonly ObjectStorageOptions _options;

    public ObjectStorageService(ObjectStorageOptions? options = null)
    {
        _options = options ?? new ObjectStorageOptions();
    }

    /// <summary>
    /// Retrieves the plugin binary (DLL or ZIP) from storage.
    /// Looks for .dll files first, then falls back to .zip files.
    /// </summary>
    public async Task<byte[]?> GetPluginBinaryAsync(Guid pluginId, Guid versionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = GetPluginDirectory(pluginId, versionId);
        if (!Directory.Exists(directory))
            return null;

        // Look for DLL files first
        var files = Directory.GetFiles(directory, "*.dll");
        if (files.Length == 0)
        {
            // Fall back to ZIP files
            files = Directory.GetFiles(directory, "*.zip");
        }

        if (files.Length == 0)
            return null;

        return await File.ReadAllBytesAsync(files[0], cancellationToken);
    }

    /// <summary>
    /// Stores a plugin binary file (ZIP or DLL) in storage.
    /// Enforces 50 MB maximum file size per object.
    /// Write access is restricted to the application service identity only
    /// (enforced at deployment level via file system permissions or cloud IAM).
    /// </summary>
    public async Task StoreAsync(Guid pluginId, Guid versionId, string fileName, byte[] data, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (data.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File size ({data.Length} bytes) exceeds maximum allowed size of {_options.MaxFileSizeBytes} bytes (50 MB).");
        }

        var directory = GetPluginDirectory(pluginId, versionId);
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, fileName);
        await File.WriteAllBytesAsync(filePath, data, cancellationToken);
    }

    /// <summary>
    /// Deletes all stored files for a specific plugin version.
    /// </summary>
    public Task<bool> DeleteAsync(Guid pluginId, Guid versionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = GetPluginDirectory(pluginId, versionId);
        if (!Directory.Exists(directory))
            return Task.FromResult(false);

        Directory.Delete(directory, recursive: true);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Checks whether any binary file exists for a specific plugin version.
    /// </summary>
    public Task<bool> ExistsAsync(Guid pluginId, Guid versionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = GetPluginDirectory(pluginId, versionId);
        var exists = Directory.Exists(directory) && Directory.GetFiles(directory).Length > 0;
        return Task.FromResult(exists);
    }

    /// <summary>
    /// Constructs the storage directory path for a plugin version.
    /// Path format: {basePath}/{pluginId}/{versionId}/
    /// </summary>
    private string GetPluginDirectory(Guid pluginId, Guid versionId)
    {
        return Path.Combine(_options.BasePath, pluginId.ToString(), versionId.ToString());
    }
}
