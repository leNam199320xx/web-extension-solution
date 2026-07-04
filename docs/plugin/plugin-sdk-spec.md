# 🧩 Plugin SDK Specification (.NET 10)

---

# 1. 🎯 PURPOSE

Defines how developers write plugins.

---

# 2. 🧱 BASE INTERFACE

```csharp
public interface IPlugin
{
    Task<object> Execute(PluginContext context);
}
```

---

# 3. 📦 PLUGIN CONTEXT

```csharp
public class PluginContext
{
    public string PluginId { get; set; }
    public string TraceId { get; set; }

    public IReadOnlyDictionary<string, ICapability> Capabilities { get; set; }
}
```

---

# 4. 🔑 CAPABILITY ACCESS

```csharp
var db = context.Capabilities["Database"] as IDatabaseCapability;
```

---

# 5. 🚫 RESTRICTIONS

Plugin MUST NOT:

- Access DB directly
- Use HttpClient directly (must use capability)
- Access file system
- Use reflection to escape sandbox

---

# 6. 📤 OUTPUT FORMAT

Plugin returns:

```json
{
  "success": true,
  "data": {}
}
```

or

```json
{
  "success": false,
  "error": "message"
}
```

---

# 7. 🧠 VERSION COMPATIBILITY

Each plugin must declare compatibility via manifest:

- target_core_version
- sdk_version (optional future extension)

---

# 🏁 END SPEC