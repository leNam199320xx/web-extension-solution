using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Infrastructure.KeyVault;

/// <summary>
/// KMS/HSM-based key provider for production use.
/// Delegates to external key management service.
/// </summary>
public class KmsKeyProvider : IKeyProvider
{
    // TODO: Inject KMS client (Azure Key Vault, AWS KMS, etc.) via constructor

    public Task<byte[]?> GetPublicKeyAsync(string keyId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // TODO: Implement actual KMS/HSM call
        throw new NotImplementedException("KMS key provider not yet configured. Use InMemoryKeyProvider for testing.");
    }

    public Task<IReadOnlyList<string>> ListKeyIdsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // TODO: Implement actual KMS/HSM call
        throw new NotImplementedException("KMS key provider not yet configured. Use InMemoryKeyProvider for testing.");
    }
}
