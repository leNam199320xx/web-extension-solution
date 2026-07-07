namespace PluginRuntime.Core.Interfaces;

public interface IManifestRepository
{
    Task<Entities.Manifest?> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken);
}
