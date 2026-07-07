# 🧱 Plugin Isolation Model
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

Defines how plugins are isolated from the host system, other plugins, and infrastructure.

For plugin loading mechanics, see `docs/plugin/plugin-loading.md`.
For resource enforcement, see `docs/runtime/resource-governance.md`.

---

# 2. CORE PRINCIPLE

> Plugins are hostile by default. Every plugin runs as if it is potentially malicious.

---

# 3. ISOLATION LEVELS

| Level | Mechanism | Security | Performance | Phase |
|-------|-----------|----------|-------------|-------|
| L1 | AssemblyLoadContext | Low | High | Phase 1 (default) |
| L2 | Process Isolation | Medium | Medium | Phase 2 (recommended for prod) |
| L3 | Container Isolation | High | Lower | Phase 3 (high-security) |
| L4 | WASM Sandbox | Very High | TBD | Future |

**Phase 1 decision**: Start with L1 (AssemblyLoadContext). Acceptable for development and staging. Production should target L2 minimum.

---

# 4. L1 — ASSEMBLLYLOADCONTEXT ISOLATION

What it provides:
- Separate assembly loading (no DLL conflicts)
- Collectible contexts (unloading support)
- Dependency isolation per plugin
- No shared static state between plugins

What it does NOT provide:
- Memory isolation (shared process heap)
- True security sandbox
- Protection against reflection abuse
- OS-level resource isolation

```csharp
// Each plugin gets its own collectible ALC
var context = new PluginLoadContext(pluginPath);
var assembly = context.LoadFromAssemblyPath(dllPath);
// ... execute ...
context.Unload(); // releases assemblies
```

---

# 5. L2 — PROCESS ISOLATION

What it provides:
- Separate OS process per plugin execution
- Memory isolation (separate address space)
- Crash isolation (plugin crash doesn't affect host)
- OS-level resource limits (via Job Objects on Windows, cgroups on Linux)

Implementation approach:
- Host launches child process for each plugin execution
- Communication via stdin/stdout (JSON-RPC) or named pipes
- Timeout enforcement at process level (kill on timeout)

---

# 6. L3 — CONTAINER ISOLATION

What it provides:
- Full filesystem isolation
- Network policy enforcement
- Resource limits via cgroups
- Minimal attack surface (read-only rootfs)

Implementation approach:
- Each plugin runs in a minimal container image
- Network disabled by default (enabled per capability)
- Container killed on timeout

---

# 7. ISOLATION BOUNDARIES

Plugins MUST NOT access:
- Operating System APIs directly
- File system outside sandbox
- Network without NetworkCapability
- Database without DatabaseCapability
- Runtime internals (private types, fields)
- Other plugins' memory or state

---

# 8. MEMORY ISOLATION

Rules:
- No shared static memory between plugins
- No global singletons across plugins
- No cross-plugin references
- Each execution has isolated scope

Note: In L1, memory isolation is enforced by convention (no shared statics). In L2+, it is enforced by the OS.

---

# 9. THREAD ISOLATION

- No shared thread pools between plugins
- No background threads that outlive execution
- No cross-plugin thread communication
- CancellationToken propagation required

---

# 10. DATA ISOLATION

- Plugins cannot access other plugin's data
- Storage is namespaced: `{pluginId}/{key}`
- Database queries are scoped per plugin (where applicable)
- Cache keys are prefixed: `{pluginId}:{key}`

---

# 11. FAILURE MODE

If isolation is violated:
1. Execution terminated immediately
2. AssemblyLoadContext unloaded (L1) / Process killed (L2) / Container destroyed (L3)
3. Audit event generated (SecurityViolationEvent)
4. Plugin marked for review

---

# 12. DESIGN PRINCIPLE

> Isolation is the foundation of trustlessness.
> Without isolation, Zero Trust cannot exist.
> Choose the isolation level that matches your threat model.

---

# 🏁 END
