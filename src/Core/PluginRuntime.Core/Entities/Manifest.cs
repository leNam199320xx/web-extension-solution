using System.Text.Json;

namespace PluginRuntime.Core.Entities;

public class Manifest
{
    public Guid ManifestId { get; init; }
    public Guid VersionId { get; init; }
    public string ManifestVersion { get; init; }
    public string TargetCoreVersion { get; init; }
    public JsonElement Permissions { get; init; }
    public JsonElement Capabilities { get; init; }
    public int ExecutionTimeoutMs { get; init; }
    public int MaxMemoryMb { get; init; }
    public int MaxCpuMs { get; init; }
    public bool AllowParallel { get; init; }
    public string Signature { get; init; }
    public string SignatureAlgorithm { get; init; }
    public string PublicKeyId { get; init; }
    public DateTime IssuedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }

    public Manifest(
        Guid manifestId,
        Guid versionId,
        string targetCoreVersion,
        string signature,
        string publicKeyId,
        JsonElement permissions = default,
        JsonElement capabilities = default,
        string manifestVersion = "1.0",
        int executionTimeoutMs = 5000,
        int maxMemoryMb = 256,
        int maxCpuMs = 2000,
        bool allowParallel = false,
        string signatureAlgorithm = "RSA-SHA256",
        DateTime? issuedAt = null,
        DateTime? expiresAt = null,
        DateTime? createdAt = null)
    {
        if (manifestId == Guid.Empty)
            throw new ArgumentException("ManifestId must not be empty.", nameof(manifestId));
        if (versionId == Guid.Empty)
            throw new ArgumentException("VersionId must not be empty.", nameof(versionId));
        if (string.IsNullOrWhiteSpace(targetCoreVersion))
            throw new ArgumentException("TargetCoreVersion must not be null or empty.", nameof(targetCoreVersion));
        if (string.IsNullOrWhiteSpace(signature))
            throw new ArgumentException("Signature must not be null or empty.", nameof(signature));
        if (string.IsNullOrWhiteSpace(publicKeyId))
            throw new ArgumentException("PublicKeyId must not be null or empty.", nameof(publicKeyId));

        ManifestId = manifestId;
        VersionId = versionId;
        ManifestVersion = manifestVersion;
        TargetCoreVersion = targetCoreVersion;
        Permissions = permissions;
        Capabilities = capabilities;
        ExecutionTimeoutMs = executionTimeoutMs;
        MaxMemoryMb = maxMemoryMb;
        MaxCpuMs = maxCpuMs;
        AllowParallel = allowParallel;
        Signature = signature;
        SignatureAlgorithm = signatureAlgorithm;
        PublicKeyId = publicKeyId;
        IssuedAt = issuedAt ?? DateTime.UtcNow;
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddYears(1);
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}
