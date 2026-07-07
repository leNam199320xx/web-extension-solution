using System.Text.Json;
using PluginRuntime.Core.Entities;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Security.Manifest;

/// <summary>
/// Validates manifest schema compliance, required fields, version compatibility,
/// expiration, signature algorithm, and resource limits.
/// </summary>
public class ManifestValidator : IManifestValidator
{
    // Current core version for compatibility checking
    private const string CurrentCoreVersion = "10.0";

    private static readonly string[] ValidSignatureAlgorithms = ["RSA-SHA256", "ECDSA-SHA256"];

    public Task<ValidationResult> ValidateAsync(Core.Entities.Manifest manifest, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        ValidateRequiredFields(manifest, errors);
        ValidateExpiration(manifest, errors);
        ValidateVersionCompatibility(manifest, errors);
        ValidateSignatureAlgorithm(manifest, errors);
        ValidateResourceLimits(manifest, errors);

        var result = new ValidationResult(errors.Count == 0, errors);
        return Task.FromResult(result);
    }

    private static void ValidateRequiredFields(Core.Entities.Manifest manifest, List<ValidationError> errors)
    {
        if (manifest.ManifestId == Guid.Empty)
            errors.Add(new ValidationError("manifest_id", "REQUIRED_FIELD", "ManifestId is required."));

        if (manifest.VersionId == Guid.Empty)
            errors.Add(new ValidationError("version_id", "REQUIRED_FIELD", "VersionId is required."));

        if (string.IsNullOrWhiteSpace(manifest.Signature))
            errors.Add(new ValidationError("signature", "REQUIRED_FIELD", "Signature is required."));

        if (string.IsNullOrWhiteSpace(manifest.PublicKeyId))
            errors.Add(new ValidationError("public_key_id", "REQUIRED_FIELD", "PublicKeyId is required."));

        if (string.IsNullOrWhiteSpace(manifest.TargetCoreVersion))
            errors.Add(new ValidationError("target_core_version", "REQUIRED_FIELD", "TargetCoreVersion is required."));

        if (manifest.Permissions.ValueKind == JsonValueKind.Undefined)
            errors.Add(new ValidationError("permissions", "REQUIRED_FIELD", "Permissions must be defined."));

        if (manifest.Capabilities.ValueKind == JsonValueKind.Undefined)
            errors.Add(new ValidationError("capabilities", "REQUIRED_FIELD", "Capabilities must be defined."));

        if (manifest.IssuedAt == default)
            errors.Add(new ValidationError("issued_at", "REQUIRED_FIELD", "IssuedAt is required."));

        if (manifest.ExpiresAt == default)
            errors.Add(new ValidationError("expires_at", "REQUIRED_FIELD", "ExpiresAt is required."));
    }

    private static void ValidateExpiration(Core.Entities.Manifest manifest, List<ValidationError> errors)
    {
        if (manifest.ExpiresAt != default && manifest.ExpiresAt <= DateTime.UtcNow)
        {
            errors.Add(new ValidationError("expires_at", "MANIFEST_EXPIRED",
                $"Manifest has expired (expires_at: {manifest.ExpiresAt:O})."));
        }
    }

    private static void ValidateVersionCompatibility(Core.Entities.Manifest manifest, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(manifest.TargetCoreVersion))
            return; // Already caught by required field validation

        if (!manifest.TargetCoreVersion.StartsWith("10."))
        {
            errors.Add(new ValidationError("target_core_version", "VERSION_INCOMPATIBLE",
                $"Target core version '{manifest.TargetCoreVersion}' is not compatible with current version '{CurrentCoreVersion}'."));
        }
    }

    private static void ValidateSignatureAlgorithm(Core.Entities.Manifest manifest, List<ValidationError> errors)
    {
        if (!string.IsNullOrWhiteSpace(manifest.SignatureAlgorithm) &&
            !ValidSignatureAlgorithms.Contains(manifest.SignatureAlgorithm, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add(new ValidationError("signature_algorithm", "INVALID_ALGORITHM",
                $"Signature algorithm '{manifest.SignatureAlgorithm}' is not supported. Valid: RSA-SHA256, ECDSA-SHA256."));
        }
    }

    private static void ValidateResourceLimits(Core.Entities.Manifest manifest, List<ValidationError> errors)
    {
        if (manifest.ExecutionTimeoutMs <= 0)
            errors.Add(new ValidationError("execution_timeout_ms", "INVALID_VALUE", "ExecutionTimeoutMs must be positive."));

        if (manifest.MaxMemoryMb <= 0)
            errors.Add(new ValidationError("max_memory_mb", "INVALID_VALUE", "MaxMemoryMb must be positive."));

        if (manifest.MaxCpuMs <= 0)
            errors.Add(new ValidationError("max_cpu_ms", "INVALID_VALUE", "MaxCpuMs must be positive."));
    }
}
