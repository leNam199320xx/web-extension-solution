# 📦 Plugin Packaging Format

---

# 1. PURPOSE

Định nghĩa format đóng gói plugin để upload vào hệ thống.

---

# 2. PACKAGE FORMAT

Plugin được đóng gói dưới dạng **ZIP archive** với extension `.plugin.zip`

---

# 3. PACKAGE STRUCTURE

```
my-plugin.plugin.zip
├── manifest.json          → Unsigned manifest (signed by system after approval)
├── plugin.dll             → Main plugin assembly
├── plugin.deps.json       → .NET dependency file
├── lib/                   → Additional dependencies (optional)
│   ├── dependency1.dll
│   └── dependency2.dll
└── README.md              → Plugin documentation (optional)
```

---

# 4. MANIFEST.JSON (Upload version — unsigned)

```json
{
  "extension_id": "my-plugin",
  "version": "1.0.0",
  "display_name": "My Plugin",
  "description": "Mô tả chức năng",
  "author": "developer@company.com",
  "entry_point": "MyPlugin.dll",
  "entry_class": "MyPlugin.PluginEntry",
  "target_core_version": ">=1.0.0",
  "permissions": [
    "db:read",
    "cache:read",
    "cache:write"
  ],
  "execution_policy": {
    "timeout_ms": 5000,
    "max_memory_mb": 128,
    "allow_parallel": false
  }
}
```

Note: `sha256`, `signature`, `public_key_id` sẽ được system thêm vào sau khi approve + sign.

---

# 5. ENTRY POINT RULES

Plugin MUST export a class implementing `IPlugin`:

```csharp
namespace MyPlugin;

public class PluginEntry : IPlugin
{
    public async Task<PluginResult> Execute(PluginContext context)
    {
        // implementation
    }
}
```

- `entry_point` → tên file DLL chứa plugin
- `entry_class` → fully-qualified class name implementing IPlugin

---

# 6. DEPENDENCY RULES

## Allowed:

- .NET Standard 2.1 libraries
- Pure logic libraries (JSON parsing, string utils, etc.)

## Forbidden:

- System.IO.FileSystem direct usage
- System.Net.Http direct usage
- Database drivers (SqlClient, Npgsql, etc.)
- Any assembly that accesses OS resources

## Scanning:

System will reject packages containing forbidden assemblies during validation phase.

---

# 7. SIZE LIMITS

| Constraint | Limit |
|-----------|-------|
| Total package size | 100 MB |
| Single DLL size | 50 MB |
| Number of DLLs | 20 |
| manifest.json size | 64 KB |

---

# 8. UPLOAD API

```
POST /api/v1/plugins/upload
Content-Type: multipart/form-data

Body:
- file: my-plugin.plugin.zip
```

Response:

```json
{
  "pluginVersionId": "guid",
  "status": "Scanning",
  "message": "Plugin accepted for validation"
}
```

---

# 9. VALIDATION PIPELINE (Post-Upload)

```
1. Extract ZIP
2. Validate structure (manifest.json + entry DLL exist)
3. Validate manifest schema
4. Compute SHA-256 of plugin.dll
5. Run SAST scan
6. Check dependency vulnerabilities
7. Verify entry class implements IPlugin
8. If PASS → move to Approval queue
9. If FAIL → reject with error details
```

---

# 10. NAMING CONVENTION

- Package file: `{extension_id}-{version}.plugin.zip`
- Example: `payment-service-1.0.0.plugin.zip`

---

# 🏁 END
