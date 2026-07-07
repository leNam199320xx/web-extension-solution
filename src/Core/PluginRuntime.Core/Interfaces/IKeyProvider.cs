namespace PluginRuntime.Core.Interfaces;

public interface IKeyProvider
{
    Task<byte[]?> GetPublicKeyAsync(string keyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListKeyIdsAsync(CancellationToken cancellationToken);
}
