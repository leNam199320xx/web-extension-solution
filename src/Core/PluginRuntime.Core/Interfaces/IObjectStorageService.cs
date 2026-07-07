namespace PluginRuntime.Core.Interfaces;

public interface IObjectStorageService
{
    Task<byte[]?> GetPluginBinaryAsync(Guid pluginId, Guid versionId, CancellationToken cancellationToken);
}
