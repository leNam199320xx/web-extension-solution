using PluginRuntime.Core.Entities;
using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Core.Interfaces;

public interface ISignatureVerifier
{
    Task<VerificationResult> VerifyAsync(Manifest manifest, CancellationToken cancellationToken);
}
