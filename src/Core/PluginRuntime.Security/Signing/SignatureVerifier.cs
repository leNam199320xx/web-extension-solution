using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;
using CoreManifest = PluginRuntime.Core.Entities.Manifest;

namespace PluginRuntime.Security.Signing;

public class SignatureVerifier : ISignatureVerifier
{
    private readonly IKeyProvider _keyProvider;

    public SignatureVerifier(IKeyProvider keyProvider)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    public async Task<VerificationResult> VerifyAsync(CoreManifest manifest, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        cancellationToken.ThrowIfCancellationRequested();

        // Get public key from provider
        var publicKeyBytes = await _keyProvider.GetPublicKeyAsync(manifest.PublicKeyId, cancellationToken);
        if (publicKeyBytes is null || publicKeyBytes.Length == 0)
        {
            return new VerificationResult(false, "KEY_NOT_FOUND",
                $"Public key '{manifest.PublicKeyId}' not found.");
        }

        // Decode signature from Base64
        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(manifest.Signature);
        }
        catch (FormatException)
        {
            return new VerificationResult(false, "INVALID_SIGNATURE_FORMAT",
                "Signature is not valid Base64.");
        }

        // Get canonical content (all manifest fields except signature)
        var canonicalContent = GetCanonicalContent(manifest);

        // Verify based on algorithm
        var algorithm = manifest.SignatureAlgorithm.ToUpperInvariant();
        bool isValid;

        switch (algorithm)
        {
            case "RSA-SHA256":
                isValid = VerifyRsa(canonicalContent, signatureBytes, publicKeyBytes);
                break;
            case "ECDSA-SHA256":
                isValid = VerifyEcdsa(canonicalContent, signatureBytes, publicKeyBytes);
                break;
            default:
                return new VerificationResult(false, "UNSUPPORTED_ALGORITHM",
                    $"Signature algorithm '{manifest.SignatureAlgorithm}' is not supported. Supported: RSA-SHA256, ECDSA-SHA256.");
        }

        if (!isValid)
        {
            return new VerificationResult(false, "SIGNATURE_INVALID",
                $"Signature verification failed using algorithm '{manifest.SignatureAlgorithm}'.");
        }

        return new VerificationResult(true, null, null);
    }

    internal static byte[] GetCanonicalContent(CoreManifest manifest)
    {
        // Serialize manifest fields (excluding signature) in deterministic order using snake_case
        var canonicalObject = new Dictionary<string, object?>
        {
            ["allow_parallel"] = manifest.AllowParallel,
            ["capabilities"] = manifest.Capabilities,
            ["execution_timeout_ms"] = manifest.ExecutionTimeoutMs,
            ["expires_at"] = manifest.ExpiresAt.ToString("O"),
            ["issued_at"] = manifest.IssuedAt.ToString("O"),
            ["manifest_id"] = manifest.ManifestId.ToString(),
            ["manifest_version"] = manifest.ManifestVersion,
            ["max_cpu_ms"] = manifest.MaxCpuMs,
            ["max_memory_mb"] = manifest.MaxMemoryMb,
            ["permissions"] = manifest.Permissions,
            ["public_key_id"] = manifest.PublicKeyId,
            ["signature_algorithm"] = manifest.SignatureAlgorithm,
            ["target_core_version"] = manifest.TargetCoreVersion,
            ["version_id"] = manifest.VersionId.ToString()
        };

        var json = JsonSerializer.Serialize(canonicalObject, CanonicalJsonOptions);
        return Encoding.UTF8.GetBytes(json);
    }

    private static readonly JsonSerializerOptions CanonicalJsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null // Keys are already in correct format
    };

    private static bool VerifyRsa(byte[] data, byte[] signature, byte[] publicKey)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static bool VerifyEcdsa(byte[] data, byte[] signature, byte[] publicKey)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
