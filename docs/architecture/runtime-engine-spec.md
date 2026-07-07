# ⚙️ Runtime Engine Specification (.NET 10)

---

# 1. PURPOSE

Runtime Engine is the core execution component responsible for orchestrating plugin execution from request to response. This document defines the internal components, their responsibilities, and contracts.

For the overall execution flow, see `docs/architecture/execution-flow.md`.
For security validation details, see `docs/security/security-enforcement-spec.md`.

---

# 2. CORE COMPONENTS

## 2.1 PluginExecutor

The top-level orchestrator that drives the execution pipeline.

```csharp
public interface IPluginExecutor
{
    Task<ExecutionResult> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken);
}

public record ExecutionRequest
{
    public string PluginId { get; init; } = "";
    public string? Version { get; init; }
    public JsonElement Input { get; init; }
    public string TraceId { get; init; } = "";
    public string? CorrelationId { get; init; }
    public string? UserId { get; init; }
    public string? TenantId { get; init; }
}

public record ExecutionResult
{
    public bool Success { get; init; }
    public JsonElement? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string TraceId { get; init; } = "";
    public string ExecutionId { get; init; } = "";
    public TimeSpan Duration { get; init; }
}
```

Responsibilities:
- Orchestrate the full execution pipeline
- Coordinate between validation, loading, and execution
- Handle top-level error mapping
- Enforce fail-closed behavior

---

## 2.2 PluginLoader

Manages AssemblyLoadContext lifecycle.

```csharp
public interface IPluginLoader
{
    Task<IPlugin> LoadAsync(
        PluginVersion version,
        Manifest manifest,
        CancellationToken cancellationToken);

    Task UnloadAsync(string pluginId, string version);
}
```

Responsibilities:
- Create isolated AssemblyLoadContext per plugin
- Resolve entry point class implementing `IPlugin`
- Manage assembly lifecycle (load/unload)
- Prevent assembly leakage

Design constraints:
- No shared mutable state between loaded plugins
- No static plugin caching across executions
- Support unloading for hot-reload scenarios

---

## 2.3 ExecutionPipeline

Staged pipeline that processes each execution request sequentially.

```csharp
public interface IExecutionPipeline
{
    Task<ExecutionResult> ProcessAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken);
}
```

Pipeline stages (order is fixed):
1. ManifestValidator (→ `docs/security/security-enforcement-spec.md`)
2. SignatureVerifier
3. HashVerifier
4. CapabilityResolver
5. PluginLoader
6. PluginExecutor (actual execution)
7. ObservabilityCollector

Each stage can short-circuit the pipeline on failure.

---

## 2.4 CapabilityResolver

Maps manifest permissions to runtime capability implementations.

```csharp
public interface ICapabilityResolver
{
    IReadOnlyDictionary<string, ICapability> Resolve(
        Manifest manifest,
        ExecutionContext executionContext);
}
```

For capability interface details, see `docs/implementation/capability-interfaces.md`.

---

## 2.5 ExecutionGovernor

Enforces resource limits during plugin execution.

```csharp
public interface IExecutionGovernor
{
    Task<T> ExecuteWithLimitsAsync<T>(
        Func<CancellationToken, Task<T>> action,
        ResourceLimits limits,
        CancellationToken cancellationToken);
}

public record ResourceLimits
{
    public int TimeoutMs { get; init; }
    public int MaxMemoryMb { get; init; }
    public int MaxCpuMs { get; init; }
}
```

For resource governance details, see `docs/runtime/resource-governance.md`.

---

# 3. ISOLATION MODEL

- AssemblyLoadContext per plugin (default — Level 1)
- Process isolation per plugin (Level 2 — recommended for production)
- No shared mutable state
- No static plugin caching

For full isolation strategy, see `docs/plugin/plugin-isolation.md`.

---

# 4. EXECUTION RULES

- MUST enforce timeout via CancellationToken
- MUST enforce memory cap via monitoring
- MUST enforce CPU guard via cooperative cancellation
- MUST propagate CancellationToken through all async calls
- MUST collect observability data regardless of success/failure

---

# 5. FAILURE MODEL

Any failure at any pipeline stage:
- Stops execution immediately (fail-closed)
- Does NOT fallback silently
- Logs with TraceId + ExecutionId
- Returns structured error (see `docs/implementation/error-handling.md`)

---

# 6. PERFORMANCE TARGETS

| Operation | Target |
|-----------|--------|
| Cold plugin load | < 500ms |
| Warm plugin load | < 100ms |
| Manifest validation | < 10ms |
| Signature verification | < 20ms |

---

# 7. DESIGN RULE

> Core never trusts plugin code under any condition.
> Every execution is a controlled, audited transaction.

---

# 🏁 END
