# 🧩 Plugin Execution Context
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

Defines the execution context injected into every plugin at runtime. This is the ONLY interface between plugin code and the Core Runtime.

For capability interfaces, see `docs/implementation/capability-interfaces.md`.
For resource governance, see `docs/runtime/resource-governance.md`.

---

# 2. CORE PRINCIPLE

> Plugins never access system resources directly.
> They only interact through PluginExecutionContext.

---

# 3. CONTEXT INTERFACE

```csharp
public interface IPluginExecutionContext
{
    /// Unique execution identifier
    string ExecutionId { get; }

    /// Distributed trace identifier
    string TraceId { get; }

    /// Request correlation identifier
    string? CorrelationId { get; }

    /// Plugin identifier
    string PluginId { get; }

    /// Plugin version being executed
    string Version { get; }

    /// Tenant identifier (if multi-tenant)
    string? TenantId { get; }

    /// Resolved capabilities based on manifest permissions
    IReadOnlyDictionary<string, ICapability> Capabilities { get; }

    /// Structured logger scoped to this execution
    IPluginLogger Logger { get; }

    /// Cancellation token (enforced timeout)
    CancellationToken CancellationToken { get; }

    /// Resource limits for this execution
    ResourceLimits Limits { get; }

    /// Input data from the request
    JsonElement Input { get; }
}
```

---

# 4. PLUGIN LOGGER

```csharp
public interface IPluginLogger
{
    void Info(string message, params object[] args);
    void Warn(string message, params object[] args);
    void Error(string message, params object[] args);
}
```

Rules:
- Structured logging only (JSON output)
- No console access
- No direct file logging
- All log entries automatically enriched with ExecutionId, TraceId, PluginId

---

# 5. RESOURCE LIMITS

```csharp
public record ResourceLimits
{
    public int TimeoutMs { get; init; }
    public int MaxMemoryMb { get; init; }
    public int MaxCpuMs { get; init; }
    public bool AllowParallel { get; init; }
}
```

Sourced from manifest `execution_policy`.

---

# 6. CAPABILITY ACCESS

Plugin accesses capabilities through the context:

```csharp
public async Task<PluginResult> Execute(IPluginExecutionContext context)
{
    var db = context.Capabilities["Database"] as IDatabaseCapability;
    var users = await db.QueryAsync<User>(
        "SELECT * FROM users WHERE active = @active",
        new { active = true },
        context.CancellationToken);

    return PluginResult.Ok(users);
}
```

Only capabilities declared in the manifest are present in the dictionary. Attempting to access an undeclared capability will result in a `KeyNotFoundException`.

---

# 7. CONTEXT LIFECYCLE

- Created fresh for EACH execution (never reused)
- Immutable during execution (read-only)
- Disposed after execution completes
- No references retained between executions

---

# 8. SECURITY RULES

Plugins MUST NOT:
- Modify context state
- Escape context boundaries via reflection
- Store context references in static fields
- Pass context to background threads that outlive execution
- Access internal .NET APIs to bypass capability checks

---

# 9. ISOLATION GUARANTEE

Each context is:
- Unique per execution
- Isolated per plugin
- Not shared across concurrent requests
- Scoped to the execution's AssemblyLoadContext

---

# 10. DESIGN PRINCIPLE

> PluginExecutionContext is the ONLY bridge between plugin and runtime.
> Everything a plugin needs is in the context. Everything else is forbidden.

---

# 🏁 END
