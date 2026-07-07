# 🧩 Plugin SDK Specification (.NET 10)

---

# 1. PURPOSE

Defines how developers write plugins for the runtime. The SDK is published as a NuGet package (`PluginRuntime.Sdk`).

For execution context details, see `docs/runtime/plugin-execution-context.md`.
For capability interfaces, see `docs/implementation/capability-interfaces.md`.
For packaging format, see `docs/implementation/plugin-packaging.md`.

---

# 2. PLUGIN INTERFACE

```csharp
namespace PluginRuntime.Sdk;

/// <summary>
/// Base interface that all plugins must implement.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Execute the plugin with the provided context.
    /// </summary>
    Task<PluginResult> Execute(IPluginExecutionContext context);
}
```

---

# 3. PLUGIN RESULT

```csharp
namespace PluginRuntime.Sdk;

public record PluginResult
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static PluginResult Ok(object? data = null)
        => new() { Success = true, Data = data };

    public static PluginResult Fail(string message)
        => new() { Success = false, ErrorMessage = message };
}
```

---

# 4. EXECUTION CONTEXT (available to plugins)

```csharp
namespace PluginRuntime.Sdk;

public interface IPluginExecutionContext
{
    string ExecutionId { get; }
    string TraceId { get; }
    string? CorrelationId { get; }
    string PluginId { get; }
    string Version { get; }
    string? TenantId { get; }
    IReadOnlyDictionary<string, ICapability> Capabilities { get; }
    IPluginLogger Logger { get; }
    CancellationToken CancellationToken { get; }
    JsonElement Input { get; }
}
```

---

# 5. LOGGER

```csharp
namespace PluginRuntime.Sdk;

public interface IPluginLogger
{
    void Info(string message, params object[] args);
    void Warn(string message, params object[] args);
    void Error(string message, params object[] args);
}
```

---

# 6. CAPABILITY ACCESS

```csharp
// Access database capability
var db = context.Capabilities["Database"] as IDatabaseCapability;
var users = await db.QueryAsync<User>(
    "SELECT * FROM users WHERE active = @active",
    new { active = true },
    context.CancellationToken);

// Access cache capability
var cache = context.Capabilities["Cache"] as ICacheCapability;
await cache.SetAsync("key", value, TimeSpan.FromMinutes(5), context.CancellationToken);
```

---

# 7. COMPLETE PLUGIN EXAMPLE

```csharp
using PluginRuntime.Sdk;

namespace MyPlugin;

public class PaymentPlugin : IPlugin
{
    public async Task<PluginResult> Execute(IPluginExecutionContext context)
    {
        context.Logger.Info("Payment plugin started");

        // Access database
        var db = context.Capabilities["Database"] as IDatabaseCapability;
        if (db == null)
            return PluginResult.Fail("Database capability not available");

        var orders = await db.QueryAsync<Order>(
            "SELECT * FROM orders WHERE status = @status",
            new { status = "pending" },
            context.CancellationToken);

        context.Logger.Info("Found {Count} pending orders", orders.Count);

        return PluginResult.Ok(new { processedCount = orders.Count });
    }
}
```

---

# 8. RESTRICTIONS

Plugin MUST NOT:
- Access database directly (use IDatabaseCapability)
- Use HttpClient directly (use INetworkCapability)
- Access file system (use IStorageCapability)
- Use reflection to escape sandbox
- Persist state in static fields
- Spawn background threads that outlive execution
- Reference internal runtime assemblies

---

# 9. VERSION COMPATIBILITY

Each plugin declares compatibility via manifest:

```json
{
  "target_core_version": ">=1.0.0 <2.0.0"
}
```

Runtime validates compatibility before loading. Incompatible plugins are rejected.

---

# 10. SDK PACKAGE CONTENTS

The `PluginRuntime.Sdk` NuGet package contains:
- `IPlugin` interface
- `IPluginExecutionContext` interface
- `IPluginLogger` interface
- `PluginResult` record
- `ICapability` base interface
- `IDatabaseCapability`, `INetworkCapability`, `IStorageCapability`, `ICacheCapability`

No implementation code. Interfaces only.

---

# 🏁 END
