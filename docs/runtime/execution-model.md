# ⚙️ Execution Model
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

Defines how plugins are executed inside the Core Runtime — lifecycle, guarantees, and boundaries.

For the full pipeline flow, see `docs/architecture/execution-flow.md`.
For scheduling, see `docs/runtime/scheduler.md`.
For resource enforcement, see `docs/runtime/resource-governance.md`.

---

# 2. CORE PRINCIPLE

> Execution is not a function call. It is a controlled, secured transaction.

Each execution MUST be:
- Isolated (no shared state)
- Measurable (full observability)
- Contained (resource limits enforced)
- Deterministic (same input → same behavior)

---

# 3. EXECUTION MODES

## 3.1 Synchronous Execution (Phase 1)

- Request → Execute → Wait → Response
- Must respect timeout
- Used for fast operations (< 5s typical)
- Default mode

## 3.2 Asynchronous Execution (Phase 2)

- Request → Enqueue → Return ExecutionId
- Client polls for result via `GET /api/v1/executions/{executionId}`
- Used for long-running operations

## 3.3 Streaming Execution (Future)

- Continuous output via WebSocket/SSE
- For real-time data processing plugins

---

# 4. EXECUTION LIFECYCLE STATES

```
Created → Validated → Scheduled → Running → Completed
                                          → Failed
                                          → Cancelled
                                          → Timeout
```

State transitions:
- `Created`: request received, context initialized
- `Validated`: security pipeline passed
- `Scheduled`: queued for execution
- `Running`: plugin code executing
- `Completed`: successful execution
- `Failed`: plugin threw exception
- `Cancelled`: caller or system cancelled
- `Timeout`: execution exceeded time limit

---

# 5. EXECUTION GUARANTEES

- **At-most-once execution**: each request executes zero or one time (no automatic retry by default)
- **No shared state**: each execution has its own context, capabilities, and memory scope
- **Deterministic input**: same request produces same plugin behavior (modulo external state)
- **Full observability**: every execution generates trace, metrics, and logs regardless of outcome
- **Isolation**: one plugin failure cannot affect another plugin or the core runtime

---

# 6. PLUGIN ENTRY POINT

```csharp
public interface IPlugin
{
    Task<PluginResult> Execute(IPluginExecutionContext context);
}

public record PluginResult
{
    public bool Success { get; init; }
    public JsonElement? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static PluginResult Ok(object? data = null) => new() { Success = true, Data = Serialize(data) };
    public static PluginResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
```

---

# 7. FAILURE MODEL

Any failure leads to:
1. Execution termination (immediate)
2. Resource cleanup (unload context if needed)
3. Audit log creation
4. Structured error returned to caller

No silent failures. No partial execution results.

---

# 8. RETRY POLICY

Default: no automatic retry.

If manifest specifies `lifecycle.max_retries > 0`:
- Retry only on transient failures (infrastructure errors)
- Never retry on security failures or plugin exceptions
- Each retry is a fresh execution (new context)

---

# 9. DESIGN PRINCIPLE

> Execution must always be explicit, isolated, and traceable.
> No implicit behavior. No hidden state. No surprise side effects.

---

# 🏁 END
