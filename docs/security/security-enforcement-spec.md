# 🔐 Security Enforcement Specification

---

# 1. PURPOSE

Converts the security model (see `docs/security/security-model.md`) into enforceable runtime logic with concrete implementation guidance.

---

# 2. VALIDATION PIPELINE

Order is FIXED and MUST NOT be changed:

```
1. Schema Validation        → MFT-001 on failure
2. Expiration Check         → MFT-003 on failure
3. SHA-256 Verification     → SEC-002 on failure
4. Signature Verification   → SEC-001 on failure
5. Revocation Check         → SEC-003 on failure
6. Version Compatibility    → MFT-004 on failure
7. Capability Mapping       → CAP-002 on failure
```

For error codes, see `docs/implementation/error-handling.md`.

---

# 3. ENFORCEMENT INTERFACE

```csharp
public interface ISecurityPipeline
{
    Task<SecurityValidationResult> ValidateAsync(
        Manifest manifest,
        byte[] pluginBinary,
        CancellationToken cancellationToken);
}

public record SecurityValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? FailedStep { get; init; }
}
```

---

# 4. STAGE DETAILS

## 4.1 Schema Validation

```csharp
public interface IManifestSchemaValidator
{
    Task<ValidationResult> ValidateSchemaAsync(
        Manifest manifest,
        CancellationToken cancellationToken);
}
```

Validates:
- All required fields present
- Field types correct
- Permissions are known strings
- Capabilities are registered names
- execution_policy values within bounds

---

## 4.2 Expiration Check

```csharp
// Check: manifest.ExpiresAt > DateTime.UtcNow + buffer
// Buffer defined in SecurityOptions.ManifestExpirationBufferMinutes
```

---

## 4.3 SHA-256 Verification

```csharp
public interface IHashVerifier
{
    Task<bool> VerifyAsync(
        ReadOnlyMemory<byte> pluginBinary,
        string expectedHash,
        CancellationToken cancellationToken);
}
```

Steps:
1. Compute SHA-256 of plugin binary
2. Compare with `manifest.sha256`
3. Reject if mismatch

---

## 4.4 Signature Verification

```csharp
public interface ISignatureVerifier
{
    Task<bool> VerifyAsync(
        string manifestJson,
        string signature,
        string publicKeyId,
        string algorithm,
        CancellationToken cancellationToken);
}
```

Steps:
1. Deserialize manifest to canonical JSON (sorted keys, no whitespace)
2. Retrieve public key from KMS/HSM by `publicKeyId`
3. Hash canonical JSON with SHA-256
4. Verify signature using RSA-SHA256 or ECDSA
5. Reject if mismatch

Supported algorithms:
- `RSA-SHA256` (default)
- `ECDSA-SHA256` (optional)

---

## 4.5 Revocation Check

```csharp
public interface IRevocationChecker
{
    Task<bool> IsRevokedAsync(
        string pluginId,
        string version,
        CancellationToken cancellationToken);
}
```

Implementation:
- Check Redis cache first (fast path)
- Fallback to database if cache miss
- Cache revocation status with TTL (e.g., 60s)

---

## 4.6 Version Compatibility

```csharp
// Parse manifest.target_core_version as SemVer range
// Compare against current Core Runtime version
// Reject if incompatible
```

---

## 4.7 Capability Mapping

```csharp
// For each capability in manifest.capabilities:
//   1. Check if registered in system
//   2. Check if permission grants access
//   3. Reject if any capability is unknown or denied
```

---

# 5. FAIL-CLOSED POLICY

Any error at any stage:
- MUST block execution
- MUST NOT fallback to unsafe mode
- MUST NOT partially execute
- MUST log the failure with TraceId, PluginId, and FailedStep
- MUST return structured error to caller

---

# 6. MIDDLEWARE INTEGRATION

Security pipeline runs BEFORE plugin loading:

```
Request → Authentication → SecurityPipeline → PluginLoader → Execution
```

If `SecurityPipeline.ValidateAsync()` returns `IsValid = false`:
- Return immediately with appropriate HTTP status
- Do NOT proceed to loading or execution

---

# 7. THREAT MITIGATION MAPPING

| Threat | Enforcement Stage | Mitigation |
|--------|-------------------|------------|
| Tampered plugin | SHA-256 Verification | Binary hash mismatch → reject |
| Fake manifest | Signature Verification | Invalid signature → reject |
| Expired contract | Expiration Check | Past expiry → reject |
| Revoked plugin | Revocation Check | Found in revocation list → reject |
| Privilege escalation | Capability Mapping | Unknown capability → reject |
| Replay attack | Revocation + Expiration | Old version blocked |
| Version mismatch | Version Compatibility | Incompatible range → reject |

---

# 8. LOGGING REQUIREMENTS

Every validation attempt MUST log:
- TraceId
- PluginId
- Version
- Step name
- Result (Pass/Fail)
- Failure reason (if applicable)
- Duration of validation step

Security failures (SEC-*) MUST additionally trigger a security event for monitoring/alerting.

---

# 9. PERFORMANCE TARGETS

| Stage | Target |
|-------|--------|
| Schema validation | < 2ms |
| Expiration check | < 1ms |
| SHA-256 verification | < 5ms (for 50MB binary) |
| Signature verification | < 20ms |
| Revocation check | < 5ms (cached) |
| Version compatibility | < 1ms |
| Capability mapping | < 2ms |
| **Total pipeline** | **< 35ms** |

---

# 10. DESIGN PRINCIPLE

> No validation = no execution.
> No exception to this rule exists.

---

# 🏁 END
