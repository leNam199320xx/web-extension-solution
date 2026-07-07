namespace PluginRuntime.Core.Interfaces;

/// <summary>
/// Abstraction for retrieving plugin binary (DLL) bytes from storage.
/// Implemented by the Infrastructure layer (object storage client).
/// </summary>
public interface IPluginBinaryStore
{
    /// <summary>
    /// Retrieves the plugin DLL bytes from storage for the given plugin and version.
    /// </summary>
    /// <param name="pluginId">The plugin identifier.</param>
    /// <param name="versionId">The plugin version identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The DLL bytes, or null if not found.</returns>
    Task<byte[]?> GetPluginBinaryAsync(Guid pluginId, Guid versionId, CancellationToken cancellationToken);
}
