# Extension Development Guide

This guide explains how to design, build, and publish extensions for the Plugin Runtime Platform.

## What Is an Extension?

An extension is a self-contained unit of functionality that runs inside the platform's isolated sandbox. It receives input, performs work using declared capabilities, and returns output — without direct access to the host system.

Extensions can:
- Read/write data through the Database capability
- Make HTTP calls through the Network capability
- Store/retrieve files through the Storage capability
- Use cached values through the Cache capability
- Call other extensions through the Extension capability

## Extension Lifecycle

```
You build it → Upload it → Platform scans it → Admin approves it → 
Platform signs it → Users can invoke it → Runtime verifies + executes it
```

1. **Build** — Write your extension code, create a manifest, package as `.plugin.zip`
2. **Upload** — Upload through the Marketplace Portal or CLI
3. **Security Scan** — Platform automatically scans for vulnerabilities
4. **Approval** — Platform admin reviews and approves
5. **Signing** — Platform signs the manifest with its private key
6. **Published** — Extension is available for invocation
7. **Execution** — Runtime verifies signature → resolves capabilities → executes in sandbox

## Project Structure

```
my-extension/
├── manifest.json          → Declares identity, version, capabilities, permissions
├── MyExtension.dll        → Compiled extension code
└── (optional dependencies)
```

## The Manifest

The manifest is the most important file. It tells the platform what your extension is and what it needs.

```json
{
  "extension_id": "com.acme.data-processor",
  "version": "1.2.0",
  "name": "Acme Data Processor",
  "description": "Processes and transforms incoming data records",
  "author": "Acme Corp",
  "entry_point": "AcmeDataProcessor.dll",
  "entry_class": "Acme.DataProcessor.Plugin",
  
  "capabilities": [
    "database",
    "network",
    "cache"
  ],
  
  "permissions": [
    {
      "capability": "database",
      "scope": "read_write",
      "justification": "Stores processed results in tenant database"
    },
    {
      "capability": "network",
      "scope": "outbound_https",
      "justification": "Fetches data from external API for enrichment"
    },
    {
      "capability": "cache",
      "scope": "read_write",
      "justification": "Caches external API responses for 5 minutes"
    }
  ],

  "visibility": "public",
  "resource_limits": {
    "max_memory_mb": 256,
    "max_execution_seconds": 30,
    "max_cpu_percent": 50
  }
}
```

## Writing Extension Code

Your extension implements the `IPlugin` interface from the SDK:

```csharp
using PluginRuntime.Sdk;

public class Plugin : IPlugin
{
    public async Task<PluginResult> ExecuteAsync(
        PluginContext context, 
        CancellationToken ct)
    {
        // Access declared capabilities through the context
        var db = context.GetCapability<IDatabaseCapability>();
        var cache = context.GetCapability<ICacheCapability>();
        
        // Read input
        var input = context.Input;
        
        // Do work...
        var cachedResult = await cache.GetAsync<string>("my-key", ct);
        if (cachedResult is null)
        {
            var data = await db.QueryAsync("SELECT ...", ct);
            await cache.SetAsync("my-key", data, TimeSpan.FromMinutes(5), ct);
            cachedResult = data;
        }
        
        // Return output
        return PluginResult.Success(new { processed = true, data = cachedResult });
    }
}
```

## Available Capabilities

| Capability | What it provides | Example use |
|-----------|-----------------|-------------|
| **Database** | Read/write to a scoped database | Store processing results, query records |
| **Network** | Make outbound HTTP/HTTPS calls | Call external APIs, fetch data |
| **Storage** | Read/write files to object storage | Store reports, import/export files |
| **Cache** | Read/write temporary cached values | Cache expensive computations |
| **Extension** | Invoke other extensions | Chain workflows, share data between plugins |

## Capability Rules

- You can only use capabilities declared in your manifest
- Attempting to use an undeclared capability will be blocked at runtime
- Each capability has a scope (read_only, read_write, outbound_https, etc.)
- You must provide a justification for each permission — admins review these

## Inter-Extension Communication

Extensions can call other extensions using the Extension capability:

```csharp
var ext = context.GetCapability<IExtensionCapability>();

var result = await ext.InvokeAsync(
    extensionId: "com.partner.enrichment-service",
    input: new { recordId = "abc123" },
    ct);
```

Visibility rules control who can call whom:
- **Public** — Any extension can invoke
- **Private** — Only extensions from the same publisher
- **Subscription** — Requires an approved subscription request

## Packaging

Package your extension as a zip file with the `.plugin.zip` extension:

```
my-extension.plugin.zip
├── manifest.json
├── MyExtension.dll
└── (any dependency DLLs)
```

## Upload & Publishing

**Via Marketplace Portal:**
1. Go to Marketplace Portal → Upload
2. Drag and drop your `.plugin.zip` file
3. Review the parsed manifest and permissions
4. Submit for review

**Via API:**
```bash
curl -X POST https://api.example.com/api/plugins/upload \
  -H "Authorization: Bearer <token>" \
  -F "file=@my-extension.plugin.zip"
```

## Versioning

- Each upload creates a new version
- Version numbers follow semver (1.0.0, 1.1.0, 2.0.0)
- Multiple versions can coexist — consumers specify which version to invoke
- Old versions can be deprecated but remain callable until revoked

## Resource Limits

Every extension runs within resource boundaries:

| Limit | Default | Purpose |
|-------|---------|---------|
| Memory | 256 MB | Prevents runaway memory usage |
| Execution time | 30 seconds | Prevents hanging extensions |
| CPU | 50% of one core | Prevents CPU starvation |

Exceeding a limit cancels the execution immediately.

## Security Model

The platform follows a zero-trust approach:

1. **No trust by default** — Your extension cannot do anything until explicitly permitted
2. **Manifest is law** — What you declare is the maximum you can access
3. **Signed manifests** — Tampered manifests are rejected at runtime
4. **Hash verification** — Modified binaries are rejected at runtime
5. **Isolated execution** — Each execution runs in its own sandbox, isolated from other extensions

## Tips for Extension Developers

- Request only the capabilities you actually need
- Write clear justifications — they speed up the approval process
- Handle timeouts gracefully — always respect the CancellationToken
- Return structured output — makes it easier for other extensions to consume
- Version your extensions properly — breaking changes should bump the major version
- Keep dependency count low — fewer DLLs means faster loading and smaller attack surface
