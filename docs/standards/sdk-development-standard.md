# 📋 SDK Development Standard

---

# 1. PURPOSE

Defines standards for the **platform team** when developing and evolving the `PluginRuntime.Sdk` NuGet package. The SDK is the contract between the platform and all extension developers — changes have ecosystem-wide impact.

For SDK spec, see `docs/plugin/plugin-sdk-spec.md`.
For versioning strategy, see `docs/plugin/versioning-strategy.md`.

---

# 2. WHO THIS APPLIES TO

- Core platform team members
- Anyone modifying `PluginRuntime.Sdk` or `PluginRuntime.Capabilities.Abstractions`
- Anyone proposing new capability interfaces

---

# 3. CORE PRINCIPLES

1. **Stability first** — SDK changes affect ALL plugin developers
2. **Minimal surface** — expose only what plugins truly need
3. **Security by design** — SDK design must make insecure patterns impossible
4. **Backward compatible** — breaking changes require MAJOR version + migration guide
5. **Testable** — all SDK interfaces must be mockable

---

# 4. API DESIGN STANDARDS

## 4.1 Interface Design

```csharp
// ✅ CORRECT — minimal, clear, async, cancellable
public interface IDatabaseCapability : ICapability
{
    Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);
}

// ❌ WRONG — too broad, leaks implementation
public interface IDatabaseCapability : ICapability
{
    DbConnection GetConnection();
    Task<DataSet> ExecuteDataSet(string sql);
}
```

## 4.2 Rules

| Rule | Rationale |
|------|-----------|
| All methods async (return `Task<T>`) | Non-blocking execution |
| All methods accept `CancellationToken` | Cooperative cancellation |
| Return `IReadOnlyList<T>` not `List<T>` | Immutability signal |
| Use `record` for DTOs | Immutable by default |
| No concrete classes in public API | Testability, flexibility |
| No infrastructure types exposed | No `SqlConnection`, `HttpClient`, etc. |
| Parameters use `object?` not `Dictionary` | Developer ergonomics |
| Generic types where appropriate | Type safety for plugins |

## 4.3 Naming

| Element | Convention | Example |
|---------|-----------|---------|
| Interface | `I{Name}Capability` | `IDatabaseCapability` |
| Method | Verb + Async suffix | `QueryAsync`, `SendAsync` |
| Parameters | camelCase | `cancellationToken` |
| Records/DTOs | PascalCase, descriptive | `NetworkRequest`, `StorageMetadata` |
| Namespace | `PluginRuntime.Sdk` or `PluginRuntime.Sdk.Capabilities` | — |

---

# 5. SECURITY STANDARDS

## 5.1 Attack Surface Minimization

SDK MUST NOT expose:
- Connection strings or credentials
- Internal service references
- File system paths
- Process or thread management
- Reflection utilities
- Assembly loading mechanisms
- Serialization of arbitrary types

## 5.2 Input Validation in SDK Design

SDK interfaces should be designed so that:
- SQL injection is prevented by parameterized-only query APIs
- Path traversal is prevented by key-based (not path-based) storage APIs
- SSRF is prevented by controlled network APIs (Core proxies all calls)
- Resource exhaustion is prevented by built-in limits in capability implementations

## 5.3 No Leaking Internals

```csharp
// ❌ WRONG — leaks internal type
public interface IPluginExecutionContext
{
    IServiceProvider Services { get; }  // NEVER expose DI container
}

// ✅ CORRECT — explicit, controlled surface
public interface IPluginExecutionContext
{
    IReadOnlyDictionary<string, ICapability> Capabilities { get; }
}
```

---

# 6. BACKWARD COMPATIBILITY STANDARDS

## 6.1 Rules

| Change Type | Allowed in MINOR? | Allowed in PATCH? |
|------------|-------------------|-------------------|
| Add new interface | ✅ Yes | ❌ No |
| Add method to existing interface | ❌ No (breaking!) | ❌ No |
| Add optional parameter with default | ✅ Yes | ❌ No |
| Remove method | ❌ No (MAJOR only) | ❌ No |
| Change return type | ❌ No (MAJOR only) | ❌ No |
| Rename parameter | ❌ No (MAJOR only) | ❌ No |
| Add new record/DTO | ✅ Yes | ❌ No |
| Add property to record | ⚠️ Only with `init` default | ❌ No |

## 6.2 Breaking Change Process

1. Write ADR explaining why the break is necessary
2. Increment MAJOR version
3. Provide migration guide document
4. Support old version for at least 1 major release cycle
5. Mark old version as deprecated with clear timeline
6. Announce to all extension developers

## 6.3 Deprecation Pattern

```csharp
[Obsolete("Use QueryAsync<T> instead. Will be removed in SDK 3.0.")]
Task<object> ExecuteQuery(string sql);
```

---

# 7. TESTING STANDARDS

## 7.1 SDK Package Tests

| Test Category | Requirement |
|--------------|-------------|
| Interface contract tests | Verify all methods are async + cancellable |
| Serialization tests | Verify records serialize/deserialize correctly |
| Backward compat tests | Load old plugin against new SDK (no break) |
| Documentation tests | Verify XML docs exist on all public members |

## 7.2 Integration Tests

Before releasing new SDK version:
- Load 3+ existing plugins with new SDK
- Verify zero compilation errors
- Verify zero runtime behavior changes
- Run plugin test suites against new SDK

## 7.3 Minimum Coverage

- SDK project: 90% coverage (it's small, interfaces + records only)
- Capability abstractions: 100% (they're just interfaces)

---

# 8. DOCUMENTATION STANDARDS

## 8.1 XML Documentation

Every public member MUST have XML docs:

```csharp
/// <summary>
/// Execute a parameterized SQL query and return typed results.
/// </summary>
/// <typeparam name="T">The result type to deserialize into.</typeparam>
/// <param name="sql">Parameterized SQL query string.</param>
/// <param name="parameters">Query parameters (anonymous object).</param>
/// <param name="cancellationToken">Cancellation token for timeout enforcement.</param>
/// <returns>Read-only list of query results.</returns>
/// <exception cref="CapabilityDeniedException">
/// Thrown when plugin does not have database permission.
/// </exception>
Task<IReadOnlyList<T>> QueryAsync<T>(
    string sql,
    object? parameters = null,
    CancellationToken cancellationToken = default);
```

## 8.2 Release Notes

Every SDK release MUST include:
- Version number
- Changes list (added, changed, deprecated, removed)
- Migration steps (if any)
- Compatibility matrix update

---

# 9. RELEASE PROCESS

```
Code change → PR review → Tests pass → Compat tests pass → Version bump → NuGet publish → Announce
```

## 9.1 Pre-Release Checklist

- [ ] All public APIs have XML documentation
- [ ] No breaking changes in MINOR/PATCH
- [ ] Backward compatibility tests pass
- [ ] 3+ existing plugins compile against new version
- [ ] Version number follows SemVer
- [ ] CHANGELOG updated
- [ ] ADR written (if architectural change)

## 9.2 NuGet Package Metadata

```xml
<PackageId>PluginRuntime.Sdk</PackageId>
<Description>SDK for building plugins for the Metadata-Driven Secure Plugin Runtime</Description>
<PackageTags>plugin;runtime;sdk;extension</PackageTags>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<RepositoryUrl>https://...</RepositoryUrl>
<PackageReadmeFile>README.md</PackageReadmeFile>
```

---

# 10. PROHIBITED SDK PATTERNS

| Pattern | Why |
|---------|-----|
| Exposing `IServiceProvider` | Plugins could resolve internal services |
| Returning mutable collections | Plugins could modify runtime state |
| Accepting `Action`/`Func` callbacks | Could capture runtime references |
| Using `dynamic` type | No compile-time safety |
| Exposing `ConcurrentDictionary` | Implies shared state |
| Generic constraints on internal types | Leaks internal implementation |
| Public `virtual` methods | Uncontrolled override in plugins |

---

# 11. CAPABILITY EXTENSION PROCESS

When adding a new capability to the SDK:

1. Write design proposal (1 page: what, why, interface, security implications)
2. Get security review approval
3. Add interface to `PluginRuntime.Capabilities.Abstractions`
4. Add implementation in dedicated project (`PluginRuntime.Capabilities.{Name}`)
5. Register in DI + CapabilityResolver
6. Update manifest schema to support new permission strings
7. Update verification engine to recognize new capability
8. Add SDK documentation + examples
9. Release SDK MINOR version
10. Announce to extension developers

---

# 12. DESIGN PRINCIPLE

> The SDK must make it **easy to do the right thing** and **impossible to do the wrong thing**.
>
> If a plugin can bypass security through the SDK API, the SDK is broken — not the plugin.

---

# 🏁 END
