# 🧠 Resource Governance
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

Defines how system resources are controlled and limited during plugin execution.

For execution model overview, see `docs/runtime/execution-model.md`.
For manifest execution policy, see `docs/plugin/manifest-spec.md`.

---

# 2. CORE PRINCIPLE

> No plugin is allowed to exceed its allocated resource budget.

Resources are not shared — they are allocated per-execution and enforced.

---

# 3. GOVERNED RESOURCES

| Resource | Enforcement | Mechanism |
|----------|-------------|-----------|
| CPU time | Mandatory | CancellationToken + timeout |
| Memory | Monitoring | GC pressure tracking, hard kill on threshold |
| Execution time | Mandatory | CancellationToken with timeout |
| Network | Controlled | Via NetworkCapability only |
| Storage | Scoped | Via StorageCapability only |
| Threads | Cooperative | No background thread persistence |

---

# 4. ENFORCEMENT INTERFACE

```csharp
public interface IExecutionGovernor
{
    /// Execute an action with resource limits enforced.
    /// Throws PluginTimeoutException or PluginMemoryLimitException on violation.
    Task<T> ExecuteWithLimitsAsync<T>(
        Func<CancellationToken, Task<T>> action,
        ResourceLimits limits,
        CancellationToken cancellationToken);
}
```

---

# 5. TIMEOUT ENFORCEMENT

```csharp
// Implementation approach:
public async Task<T> ExecuteWithLimitsAsync<T>(
    Func<CancellationToken, Task<T>> action,
    ResourceLimits limits,
    CancellationToken cancellationToken)
{
    using var timeoutCts = CancellationTokenSource
        .CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(limits.TimeoutMs);

    try
    {
        return await action(timeoutCts.Token);
    }
    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
    {
        throw new PluginTimeoutException(limits.TimeoutMs);
    }
}
```

---

# 6. MEMORY MONITORING

Strategy: **Soft enforcement with hard kill**

- Monitor GC memory pressure during execution
- If allocated memory exceeds `MaxMemoryMb`:
  1. Log warning
  2. If continues to grow: cancel execution
  3. Unload AssemblyLoadContext

Note: .NET does not provide per-assembly memory isolation. Memory monitoring is best-effort in L1 isolation. For true memory isolation, use L2 (process) or L3 (container).

---

# 7. CPU ENFORCEMENT

Strategy: **Cooperative via CancellationToken**

- CPU time is indirectly controlled by execution timeout
- `MaxCpuMs` in manifest serves as guidance for timeout calculation
- Infinite loops are caught by timeout mechanism

---

# 8. VIOLATION HANDLING

| Violation | Action |
|-----------|--------|
| Timeout exceeded | Cancel execution, throw `PluginTimeoutException`, log event |
| Memory threshold reached | Cancel execution, throw `PluginMemoryLimitException`, log event |
| Network abuse attempt | Denied by NetworkCapability, log security event |
| Storage quota exceeded | Denied by StorageCapability, return error |

All violations:
- Are logged with TraceId + PluginId
- Generate an audit event
- Result in execution status = `Failed`

---

# 9. BACKPRESSURE MODEL

When system is under load:

```
Request arrives
  → Check concurrent execution count
  → If above MaxConcurrentExecutions:
      → Reject with 429 (Too Many Requests)
      → Or queue with priority (if queue enabled)
```

---

# 10. MULTI-TENANT GOVERNANCE

If multi-tenant enabled, each tenant has:
- Per-tenant concurrent execution limit
- Per-tenant total memory budget
- Per-tenant rate limit

Enforcement at API gateway level + runtime level.

---

# 11. CONFIGURATION

Limits are sourced from (in priority order):
1. Plugin manifest `execution_policy` (per-plugin)
2. System-wide `RuntimeOptions` defaults (fallback)

```json
// From manifest
"execution_policy": {
  "timeout_ms": 5000,
  "max_memory_mb": 256,
  "max_cpu_ms": 2000,
  "allow_parallel": false
}
```

---

# 12. DESIGN PRINCIPLE

> Resources are allocated, monitored, and enforced.
> No plugin can affect system stability by consuming excessive resources.

---

# 🏁 END
