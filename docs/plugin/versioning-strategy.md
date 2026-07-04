# 🔄 Versioning Strategy - Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. 🎯 PURPOSE

This document defines the versioning strategy for all components within the
Metadata-Driven Plugin Runtime platform.

Objectives:

- Maintain backward compatibility whenever possible.
- Allow independent evolution of components.
- Enable safe plugin upgrades.
- Prevent runtime incompatibility.
- Provide deterministic compatibility validation.

---

# 2. VERSIONED COMPONENTS

The platform contains independent versioned artifacts.

| Component | Versioned |
|-----------|-----------|
| Core Runtime | ✅ |
| Plugin SDK | ✅ |
| Plugin | ✅ |
| Manifest Schema | ✅ |
| Capability Contracts | ✅ |
| REST API | ✅ |

Each component evolves independently.

---

# 3. VERSION FORMAT

Unless otherwise specified:

```
MAJOR.MINOR.PATCH
```

Example:

```
2.4.1
```

Where:

MAJOR

Breaking changes.

Examples:

- Removed interface
- Changed execution model
- Changed capability contract

---

MINOR

Backward compatible features.

Examples:

- New capability
- New optional manifest field
- New SDK helper

---

PATCH

Bug fixes only.

Examples:

- Performance improvements
- Security fixes
- Internal optimizations

---

# 4. COMPONENT VERSION RULES

## Core Runtime

Example:

```
3.2.0
```

Major version changes MAY introduce:

- new execution engine
- breaking API
- runtime behavior changes

Minor version changes MUST remain compatible.

Patch versions MUST NOT change behavior.

---

## Plugin SDK

Plugin developers compile against SDK.

SDK changes:

Major

Breaking interface changes.

Minor

New helper APIs.

Patch

Bug fixes.

---

## Plugin Package

Each plugin owns its own version.

Example

```
PaymentPlugin

1.0.0
1.1.0
1.2.0
2.0.0
```

Plugin version is independent of Core version.

---

## Manifest Schema

Manifest has dedicated version.

Example

```json
{
  "manifestVersion": "2.0"
}
```

Schema evolution MUST be backward compatible whenever possible.

---

## Capability Contract

Capabilities are versioned independently.

Example

```
DatabaseCapability

v1

ExecuteQuery()

v2

ExecuteQueryAsync()
BeginTransaction()
```

Old plugins MUST continue working.

---

# 5. COMPATIBILITY MATRIX

Compatibility is determined using multiple dimensions.

Example:

| Core | SDK | Manifest | Plugin |
|------|------|----------|--------|
| 2.x | 2.x | 2.x | ✅ |
| 2.x | 1.x | 1.x | Optional |
| 3.x | 1.x | 1.x | ❌ |
| 3.x | 2.x | 2.x | ✅ |

---

# 6. MANIFEST COMPATIBILITY

Manifest contains:

```json
{
  "targetCoreVersion": ">=2.0.0 <3.0.0"
}
```

Runtime validates:

- syntax
- compatibility
- supported schema

If incompatible:

Execution MUST be rejected.

---

# 7. SDK COMPATIBILITY

Plugin SDK defines:

```
SdkVersion

2.0
```

Core Runtime exposes supported SDK versions.

Example:

```
Supported:

2.0

2.1

2.2
```

Unsupported SDK versions MUST be rejected during plugin loading.

---

# 8. CAPABILITY EVOLUTION

Capabilities MUST evolve safely.

Preferred strategy:

Never remove.

Instead:

- Deprecate
- Introduce replacement
- Remove only in next MAJOR

Example:

```
DatabaseCapability

v1

Execute()

↓

Deprecated

↓

v2

ExecuteAsync()
```

---

# 9. PLUGIN UPGRADE

Upgrade path:

```
Plugin v1

↓

Upload

↓

Validation

↓

Approval

↓

Signing

↓

Repository

↓

Runtime Hot Reload

↓

Plugin v2
```

Rollback MUST remain possible.

---

# 10. HOT RELOAD VERSION RULES

Runtime may temporarily host:

Plugin

v1

and

Plugin

v2

during deployment.

Old requests finish on v1.

New requests start on v2.

No request interruption allowed.

---

# 11. DEPRECATION POLICY

Deprecation process:

```
Supported

↓

Deprecated

↓

Warning

↓

Removal
```

Minimum recommendation:

One major release before removal.

---

# 12. BREAKING CHANGE POLICY

Breaking changes MUST:

- increment MAJOR version
- include migration guide
- update compatibility matrix
- update manifest specification

---

# 13. DATABASE VERSIONING

Database schema MUST use migrations.

Rules:

- No destructive migration by default.
- Forward-only migration.
- Rollback tested before production.

Recommended:

EF Core Migrations.

---

# 14. API VERSIONING

REST API follows URI versioning.

Example:

```
/api/v1/execute

/api/v2/execute
```

Old API remains available during migration window.

---

# 15. PLUGIN STORAGE

Repository stores every version.

Example:

```
PaymentPlugin

1.0.0

1.1.0

2.0.0
```

No overwrite allowed.

Storage is immutable.

---

# 16. COMPATIBILITY VALIDATION PIPELINE

Before execution:

```
Manifest

↓

Schema Version

↓

Core Version

↓

SDK Version

↓

Capability Version

↓

Plugin Load
```

Any incompatibility rejects execution.

---

# 17. MIGRATION STRATEGY

Migration follows:

```
Current

↓

Validate

↓

Upgrade

↓

Compatibility Test

↓

Production

↓

Monitor
```

Rollback plan MUST exist.

---

# 18. VERSIONING PRINCIPLES

The platform follows:

- Semantic Versioning
- Backward Compatibility First
- Immutable Releases
- Explicit Compatibility
- Safe Migration
- No Silent Breaking Changes

---

# 19. FINAL PRINCIPLE

Version compatibility is enforced by Runtime.

Plugins never decide compatibility.

The Runtime is the single source of truth.

---

# 🏁 END OF VERSIONING STRATEGY