using System.Security.Cryptography;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Security.Hashing;

public class HashVerifier : IHashVerifier
{
    public Task<VerificationResult> VerifyAsync(byte[] dllBytes, string expectedHash, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dllBytes);

        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            return Task.FromResult(new VerificationResult(false, "HASH_MISSING", "Expected hash is missing or empty."));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var computedHashBytes = SHA256.HashData(dllBytes);
        var computedHash = Convert.ToHexStringLower(computedHashBytes);

        var normalizedExpected = expectedHash.Trim().ToLowerInvariant();

        if (!string.Equals(computedHash, normalizedExpected, StringComparison.Ordinal))
        {
            return Task.FromResult(new VerificationResult(false, "HASH_MISMATCH",
                $"DLL hash mismatch. Expected: {normalizedExpected}, Computed: {computedHash}."));
        }

        return Task.FromResult(new VerificationResult(true, null, null));
    }
}
