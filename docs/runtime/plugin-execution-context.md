# 🧩 Plugin Execution Context
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

Defines the execution context injected into every plugin at runtime.

---

# 2. CORE PRINCIPLE

> Plugins never access system resources directly.

They only interact through ExecutionContext.

---

# 3. CONTEXT STRUCTURE

```
PluginExecutionContext
 ├── CorrelationId
 ├── ExecutionId
 ├── TenantId
 ├── PluginId
 ├── UserIdentity
 ├── Capabilities
 ├── Logger
 ├── CancellationToken
 ├── ResourceLimits
```

---

# 4. CAPABILITY ACCESS

All external operations MUST go through:

```
context.Capabilities.*
```

Examples:

- DatabaseCapability
- StorageCapability
- NetworkCapability

---

# 5. LOGGING MODEL

Plugins use:

```
context.Logger
```

Rules:

- Structured logging only
- No console access
- No direct file logging

---

# 6. CANCELLATION MODEL

Each execution includes:

- CancellationToken
- Timeout enforcement
- Cooperative cancellation only

---

# 7. CONTEXT IMMUTABILITY

ExecutionContext is:

- Read-only
- Immutable during execution
- Recreated per execution

---

# 8. SECURITY RULE

Plugins MUST NOT:

- Modify context state
- Escape context boundaries
- Store references globally

---

# 9. ISOLATION GUARANTEE

Each context is:

- Unique per execution
- Isolated per plugin
- Not shared across requests

---

# 10. DESIGN PRINCIPLE

> ExecutionContext is the ONLY bridge between plugin and runtime.

---

# 🏁 END OF EXECUTION CONTEXT