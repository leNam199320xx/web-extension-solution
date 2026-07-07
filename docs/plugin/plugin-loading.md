# 🔌 Plugin Loading Model
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

Defines how plugins are loaded into the Core Runtime.

For security validation before loading, see `docs/security/security-enforcement-spec.md`.
For isolation strategy, see `docs/plugin/plugin-isolation.md`.

---

# 2. CORE PRINCIPLE

> Plugins are NEVER trusted at load time.
> Loading does NOT grant execution permission.
> Every plugin MUST pass full validation before being loaded.

---

# 3. LOADING PIPELINE

```
Request → Manifest Resolution → Security Validation → Isolation Setup → Assembly Load → Entry Point Resolution → Ready
```

---

# 4. STEP-BY-STEP FLOW

## Step 1 — Plugin Request

Runtime receives: PluginId, Version, ExecutionContext.

## Step 2 — Manifest Resolution

- Load signed manifest from database/cache
- Load plugin version metadata
- If not found → reject with `API-004`

## Step 3 — Security Validation

Full security pipeline (see `docs/security/security-enforcement-spec.md`):
- Signature verification
- SHA-256 hash validation
- Version compatibility
- Revocation check

Failure → abort loading.

## Step 4 — Isolation Preparation

- Create isolated AssemblyLoadContext (or process/container depending on level)
- Configure memory monitoring
- Set up CancellationToken scope

## Step 5 — Assembly Loading

```csharp
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }
}
```

Rules:
- `isCollectible: true` enables unloading
- No global assembly injection
- No shared static state across plugins
- Dependency resolution scoped to plugin directory

## Step 6 — Entry Point Resolution

```csharp
var assembly = loadContext.LoadFromAssemblyPath(pluginDllPath);
var entryType = assembly.GetType(manifest.EntryClass)
    ?? throw new PluginLoadException("EXE-006", "Entry point class not found");

var plugin = Activator.CreateInstance(entryType) as IPlugin
    ?? throw new PluginLoadException("EXE-006", "Class does not implement IPlugin");
```

## Step 7 — Ready State

Plugin registered as loaded. Not yet executed.

---

# 5. LOADING RULES

- No execution during loading phase
- No network access during load
- No capability access during load
- Load is deterministic and repeatable
- Failed loads leave no residual state

---

# 6. CACHING STRATEGY

Loaded plugins MAY be cached in memory for warm starts:
- Cache key: `{pluginId}:{version}`
- Invalidate on: revocation, reload request, version change
- TTL: configurable (default: no expiry, explicit invalidation only)

Cache MUST be invalidated before security decisions rely on it.

---

# 7. UNLOADING

```csharp
public async Task UnloadAsync(string pluginId, string version)
{
    // 1. Remove from cache/registry
    // 2. Wait for active executions to complete (with timeout)
    // 3. Unload AssemblyLoadContext
    // 4. Force GC if needed for collectible context
}
```

---

# 8. ERROR HANDLING

If any step fails:
- Plugin is NOT loaded
- No cached state remains
- Audit log written
- Structured error returned (see `docs/implementation/error-handling.md`)

---

# 9. PERFORMANCE TARGETS

| Operation | Target |
|-----------|--------|
| Cold load (disk → memory) | < 500ms |
| Warm load (cached context) | < 100ms |
| Unload | < 200ms |

---

# 10. DESIGN PRINCIPLE

> Loading a plugin is a privileged operation, but NOT a trusted operation.
> Trust is established only through the security pipeline, never through loading alone.

---

# 🏁 END
