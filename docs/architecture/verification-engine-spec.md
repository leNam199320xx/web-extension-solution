# 🔍 Extension Verification Engine Specification

---

# 1. PURPOSE

Defines the automated verification pipeline that validates extensions on upload. This engine enforces the Extension Development Standard (see `docs/standards/extension-development-standard.md`) programmatically — no manual review needed for pass/fail determination.

For the plugin lifecycle context, see `docs/plugin/plugin-lifecycle.md` (Phase 2 - Validation).
For error codes, see `docs/implementation/error-handling.md`.

---

# 2. CORE PRINCIPLE

> Every extension is guilty until proven compliant.
> Verification is automated, deterministic, and non-bypassable.

---

# 3. HIGH-LEVEL FLOW

```
Upload API
    ↓
Package Extraction
    ↓
┌─────────────────────────────────────────┐
│       Verification Engine               │
│                                         │
│  Stage 1: Structure Validation          │
│  Stage 2: Manifest Validation           │
│  Stage 3: Static Analysis (IL Scan)     │
│  Stage 4: Dependency Audit              │
│  Stage 5: Security Scan                 │
│  Stage 6: Standards Compliance          │
│  Stage 7: Sandbox Execution (optional)  │
│                                         │
└──────────────┬──────────────────────────┘
               │
       All stages pass?
      ┌────────┴────────┐
      ▼                  ▼
 → Approval Queue    → Rejected (detailed report)
```

---

# 4. VERIFICATION RESULT MODEL

```csharp
public record VerificationResult
{
    public string PluginId { get; init; } = "";
    public string Version { get; init; } = "";
    public VerificationStatus Status { get; init; }
    public IReadOnlyList<VerificationStageResult> Stages { get; init; } = [];
    public DateTime VerifiedAt { get; init; }
    public TimeSpan TotalDuration { get; init; }
}

public enum VerificationStatus
{
    Passed,         // All stages passed
    PassedWithWarnings,  // Passed but has non-blocking issues
    Failed          // One or more stages failed
}

public record VerificationStageResult
{
    public string StageName { get; init; } = "";
    public StageStatus Status { get; init; }
    public IReadOnlyList<VerificationIssue> Issues { get; init; } = [];
    public TimeSpan Duration { get; init; }
}

public enum StageStatus { Passed, Warning, Failed, Skipped }

public record VerificationIssue
{
    public IssueSeverity Severity { get; init; }
    public string RuleId { get; init; } = "";      // e.g., "EXT-101"
    public string Message { get; init; } = "";
    public string? File { get; init; }
    public int? Line { get; init; }
    public string? SuggestedFix { get; init; }
}

public enum IssueSeverity { Error, Warning, Info }
```

---

# 5. STAGE 1: STRUCTURE VALIDATION

**Purpose**: Verify the package has correct file structure.

**Checks:**

| Rule ID | Check | Severity |
|---------|-------|----------|
| STR-001 | ZIP is valid and extractable | Error |
| STR-002 | `manifest.json` exists at root | Error |
| STR-003 | Entry point DLL exists (from manifest `entry_point`) | Error |
| STR-004 | Total package size ≤ 100 MB | Error |
| STR-005 | Single DLL size ≤ 50 MB | Error |
| STR-006 | Total DLL count ≤ 20 | Error |
| STR-007 | `README.md` exists | Warning |
| STR-008 | No unexpected executable files (.exe, .bat, .sh, .ps1) | Error |

**Implementation:**

```csharp
public interface IStructureValidator
{
    Task<VerificationStageResult> ValidateAsync(
        ExtractedPackage package,
        CancellationToken cancellationToken);
}
```

---

# 6. STAGE 2: MANIFEST VALIDATION

**Purpose**: Validate manifest schema and field values.

**Checks:**

| Rule ID | Check | Severity |
|---------|-------|----------|
| MNF-001 | JSON is valid | Error |
| MNF-002 | `extension_id` format valid (lowercase, hyphens, 3-50 chars) | Error |
| MNF-003 | `version` is valid SemVer | Error |
| MNF-004 | `display_name` present, ≤ 100 chars | Error |
| MNF-005 | `description` present, ≤ 500 chars | Error |
| MNF-006 | `author` is valid email | Error |
| MNF-007 | `entry_point` references existing DLL | Error |
| MNF-008 | `entry_class` is fully qualified | Error |
| MNF-009 | `target_core_version` is valid range | Error |
| MNF-010 | `permissions` contains only known strings | Error |
| MNF-011 | `execution_policy.timeout_ms` in range [100, 30000] | Error |
| MNF-012 | `execution_policy.max_memory_mb` in range [16, 512] | Error |
| MNF-013 | All declared capabilities have matching permissions | Warning |
| MNF-014 | Version not already used for this plugin | Error |

**Implementation:**

```csharp
public interface IManifestValidator
{
    Task<VerificationStageResult> ValidateAsync(
        Manifest manifest,
        ExtractedPackage package,
        CancellationToken cancellationToken);
}
```

---

# 7. STAGE 3: STATIC ANALYSIS (IL SCAN)

**Purpose**: Scan compiled assemblies for forbidden API usage using IL analysis.

**Technology**: Mono.Cecil (or System.Reflection.Metadata for read-only analysis)

**Checks:**

| Rule ID | Check | Severity |
|---------|-------|----------|
| ILS-001 | No `System.Data.Common.DbConnection` usage | Error |
| ILS-002 | No `System.Net.Http.HttpClient` constructor calls | Error |
| ILS-003 | No `System.IO.File` / `System.IO.Directory` usage | Error |
| ILS-004 | No `System.Diagnostics.Process` usage | Error |
| ILS-005 | No `System.Reflection.Assembly.Load*` calls | Error |
| ILS-006 | No `System.Reflection.Emit` namespace usage | Error |
| ILS-007 | No `System.Runtime.InteropServices` P/Invoke | Error |
| ILS-008 | No `BindingFlags.NonPublic` reflection | Error |
| ILS-009 | No `System.Net.Sockets` usage | Error |
| ILS-010 | No `System.Threading.Thread` constructor | Error |
| ILS-011 | No `System.Environment` sensitive methods | Error |
| ILS-012 | No `Activator.CreateInstance` with string type name | Warning |
| ILS-013 | Entry class implements `IPlugin` interface | Error |
| ILS-014 | `Execute` method is async and returns `Task<PluginResult>` | Error |
| ILS-015 | No static mutable fields in any class | Error |

**Implementation:**

```csharp
public interface IStaticAnalyzer
{
    Task<VerificationStageResult> AnalyzeAsync(
        IReadOnlyList<AssemblyInfo> assemblies,
        CancellationToken cancellationToken);
}

public record AssemblyInfo
{
    public string Path { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsEntryAssembly { get; init; }
}
```

**Scan approach:**

```csharp
// Pseudo-code using Mono.Cecil
var module = ModuleDefinition.ReadModule(dllPath);
foreach (var type in module.Types)
{
    foreach (var method in type.Methods)
    {
        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.Operand is MethodReference methodRef)
            {
                CheckForbiddenCall(methodRef, type, method);
            }
        }
    }
}
```

---

# 8. STAGE 4: DEPENDENCY AUDIT

**Purpose**: Verify all dependencies are safe and allowed.

**Checks:**

| Rule ID | Check | Severity |
|---------|-------|----------|
| DEP-001 | No blacklisted packages (see Extension Standard §5.5) | Error |
| DEP-002 | No known critical vulnerabilities (CVE) | Error |
| DEP-003 | No known high vulnerabilities | Warning |
| DEP-004 | All packages from trusted sources (nuget.org) | Error |
| DEP-005 | No pre-release packages in production plugins | Warning |
| DEP-006 | Total dependency count ≤ 50 | Warning |
| DEP-007 | No dependency version conflicts | Warning |

**Implementation:**

```csharp
public interface IDependencyAuditor
{
    Task<VerificationStageResult> AuditAsync(
        DepsManifest dependencies,
        CancellationToken cancellationToken);
}
```

**Data source:**
- Parse `plugin.deps.json` for dependency tree
- Check against NuGet vulnerability API
- Check against internal blacklist

---

# 9. STAGE 5: SECURITY SCAN

**Purpose**: Detect secrets, credentials, and security anti-patterns.

**Checks:**

| Rule ID | Check | Severity |
|---------|-------|----------|
| SEC-101 | No hardcoded API keys (regex patterns) | Error |
| SEC-102 | No connection strings in code | Error |
| SEC-103 | No private keys (PEM/PFX) in package | Error |
| SEC-104 | No JWT tokens | Error |
| SEC-105 | No password-like string literals | Warning |
| SEC-106 | No base64-encoded long strings (potential embedded secrets) | Warning |
| SEC-107 | No URLs with embedded credentials | Error |

**Secret patterns (regex):**

```
# API Keys
(sk|pk|api[_-]?key)[_-]?[a-zA-Z0-9]{20,}

# AWS Keys
AKIA[0-9A-Z]{16}

# Connection strings
(Server|Data Source|Host)=.*(Password|Pwd)=

# Private keys
-----BEGIN (RSA |EC )?PRIVATE KEY-----

# Generic secrets
(password|secret|token|credential)\s*[:=]\s*["'][^"']{8,}["']
```

**Implementation:**

```csharp
public interface ISecurityScanner
{
    Task<VerificationStageResult> ScanAsync(
        ExtractedPackage package,
        CancellationToken cancellationToken);
}
```

Scans:
- All source files (if included)
- All DLLs (embedded strings via IL)
- manifest.json
- README.md
- Any .json/.xml/.config files in package

---

# 10. STAGE 6: STANDARDS COMPLIANCE

**Purpose**: Verify non-security quality standards.

**Checks:**

| Rule ID | Check | Severity |
|---------|-------|----------|
| STD-001 | README.md contains required sections | Warning |
| STD-002 | Namespace follows convention | Warning |
| STD-003 | Entry class name follows convention | Warning |
| STD-004 | No compiler warnings in metadata | Warning |
| STD-005 | Nullable reference types enabled | Warning |
| STD-006 | Test results present (if included) | Info |
| STD-007 | Test coverage ≥ 70% (if report included) | Warning |

**Implementation:**

```csharp
public interface IStandardsChecker
{
    Task<VerificationStageResult> CheckAsync(
        ExtractedPackage package,
        Manifest manifest,
        CancellationToken cancellationToken);
}
```

---

# 11. STAGE 7: SANDBOX EXECUTION (Optional)

**Purpose**: Load and execute the plugin in an isolated sandbox to verify it actually works.

**Checks:**

| Rule ID | Check | Severity |
|---------|-------|----------|
| SBX-001 | Plugin loads without error | Error |
| SBX-002 | `Execute()` returns within timeout | Error |
| SBX-003 | No unhandled exceptions thrown | Error |
| SBX-004 | Memory usage within declared limit | Warning |
| SBX-005 | Plugin respects CancellationToken | Warning |

**Implementation:**

```csharp
public interface ISandboxExecutor
{
    Task<VerificationStageResult> ExecuteAsync(
        ExtractedPackage package,
        Manifest manifest,
        CancellationToken cancellationToken);
}
```

**Sandbox environment:**
- Isolated process (L2 isolation minimum)
- Mock capabilities injected (no real infrastructure)
- Timeout: 2x declared timeout
- Memory limit: declared limit + 10% buffer
- Network: completely disabled

---

# 12. VERIFICATION ENGINE INTERFACE

```csharp
public interface IVerificationEngine
{
    /// Run all verification stages on an uploaded package.
    Task<VerificationResult> VerifyAsync(
        Stream packageStream,
        CancellationToken cancellationToken);
}
```

---

# 13. PIPELINE BEHAVIOR

- Stages run **sequentially** (Stage 1 failure blocks Stage 2, etc.)
- Exception: Stage 6 (Standards) and Stage 7 (Sandbox) can be skipped if earlier stages fail
- Each stage has its own timeout (default: 60 seconds per stage)
- Total pipeline timeout: 5 minutes
- Results are persisted in database for audit

---

# 14. VERIFICATION REPORT (Returned to Developer)

```json
{
  "pluginId": "payment-service",
  "version": "1.0.0",
  "status": "Failed",
  "verifiedAt": "2026-01-15T10:00:00Z",
  "totalDurationMs": 3200,
  "stages": [
    {
      "name": "StructureValidation",
      "status": "Passed",
      "issues": [],
      "durationMs": 50
    },
    {
      "name": "ManifestValidation",
      "status": "Passed",
      "issues": [],
      "durationMs": 20
    },
    {
      "name": "StaticAnalysis",
      "status": "Failed",
      "issues": [
        {
          "severity": "Error",
          "ruleId": "ILS-002",
          "message": "Direct HttpClient usage detected",
          "file": "PaymentPlugin.dll",
          "line": null,
          "suggestedFix": "Use INetworkCapability.SendAsync() instead"
        },
        {
          "severity": "Error",
          "ruleId": "ILS-015",
          "message": "Static mutable field detected: '_cache'",
          "file": "PaymentPlugin.dll",
          "line": null,
          "suggestedFix": "Use ICacheCapability instead of static state"
        }
      ],
      "durationMs": 1500
    }
  ],
  "summary": "2 errors in StaticAnalysis stage. Fix and re-upload."
}
```

---

# 15. EXTENSIBILITY

New verification rules can be added by:
1. Adding rule ID + check to the appropriate stage
2. Implementing the check in the stage handler
3. Updating the Extension Development Standard document
4. No SDK change needed (verification is server-side)

---

# 16. PERFORMANCE TARGETS

| Stage | Target |
|-------|--------|
| Structure validation | < 1s |
| Manifest validation | < 500ms |
| Static analysis (50MB max) | < 30s |
| Dependency audit | < 10s |
| Security scan | < 10s |
| Standards compliance | < 2s |
| Sandbox execution | < 60s |
| **Total pipeline** | **< 2 minutes** |

---

# 17. INTEGRATION WITH LIFECYCLE

```
Upload API → VerificationEngine.VerifyAsync()
                    │
              ┌─────┴─────┐
              ▼            ▼
         Passed       Failed
              │            │
              ▼            ▼
    Approval Queue    Return report to developer
              │
              ▼
    Manual/Auto Approval
              │
              ▼
    Manifest Signing (KMS)
              │
              ▼
    Plugin Repository
```

---

# 18. DESIGN PRINCIPLE

> The Verification Engine is the gatekeeper.
> Nothing enters the system without passing through it.
> It is deterministic, auditable, and non-bypassable.

---

# 🏁 END
