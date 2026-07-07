namespace PluginRuntime.Core.Interfaces;

public interface IRevocationChecker
{
    Task<bool> IsRevokedAsync(Guid versionId, CancellationToken cancellationToken);
}
