using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Core.Interfaces;

public interface IHashVerifier
{
    Task<VerificationResult> VerifyAsync(byte[] dllBytes, string expectedHash, CancellationToken cancellationToken);
}
