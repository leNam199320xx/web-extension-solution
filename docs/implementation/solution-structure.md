# 🧱 Solution Structure - .NET 10

---

# 1. PURPOSE

Định nghĩa cấu trúc solution cụ thể để bắt đầu implementation.

---

# 2. SOLUTION LAYOUT

```
src/
├── PluginRuntime.sln
│
├── Core/
│   ├── PluginRuntime.Core/                  → Domain logic, interfaces, contracts
│   ├── PluginRuntime.Runtime/               → Execution engine, pipeline, loader
│   └── PluginRuntime.Security/              → Manifest validation, signing, crypto
│
├── Infrastructure/
│   ├── PluginRuntime.Infrastructure/        → EF Core, PostgreSQL, Redis, Storage
│   └── PluginRuntime.Infrastructure.KeyVault/ → KMS/HSM integration
│
├── API/
│   └── PluginRuntime.Api/                   → ASP.NET Core Web API host
│
├── Admin/
│   └── PluginRuntime.Admin/                 → Blazor Server Admin Portal
│
├── Capabilities/
│   ├── PluginRuntime.Capabilities.Abstractions/ → ICapability interfaces
│   ├── PluginRuntime.Capabilities.Database/     → IDatabaseCapability impl
│   ├── PluginRuntime.Capabilities.Network/      → INetworkCapability impl
│   ├── PluginRuntime.Capabilities.Storage/      → IStorageCapability impl
│   ├── PluginRuntime.Capabilities.Cache/        → ICacheCapability impl
│   └── PluginRuntime.Capabilities.Extension/    → IExtensionCapability impl
│
├── SDK/
│   └── PluginRuntime.Sdk/                   → Plugin developer SDK (IPlugin, PluginContext)
│
└── Tests/
    ├── PluginRuntime.Core.Tests/
    ├── PluginRuntime.Runtime.Tests/
    ├── PluginRuntime.Security.Tests/
    ├── PluginRuntime.Api.Tests/
    ├── PluginRuntime.Infrastructure.Tests/
    └── PluginRuntime.IntegrationTests/
```

---

# 3. PROJECT RESPONSIBILITIES

## PluginRuntime.Core

- Domain entities (Plugin, Manifest, Execution, etc.)
- Interface definitions (IPluginExecutor, IManifestValidator, etc.)
- Value objects, enums, constants
- No external dependencies

## PluginRuntime.Runtime

- PluginExecutor (orchestrator)
- ExecutionPipeline (staged execution)
- PluginLoader (AssemblyLoadContext management)
- CapabilityResolver
- Timeout/resource enforcement
- Depends on: Core, Capabilities.Abstractions

## PluginRuntime.Security

- ManifestValidator
- SignatureVerifier (RSA/ECDSA)
- HashVerifier (SHA-256)
- RevocationChecker
- Depends on: Core

## PluginRuntime.Infrastructure

- EF Core DbContext
- Repository implementations
- Redis cache client
- Object storage client
- Depends on: Core

## PluginRuntime.Api

- ASP.NET Core host
- Controllers (Plugin, Execute, Admin)
- Middleware (Auth, Tracing, Error handling)
- DI registration
- Depends on: all projects

## PluginRuntime.Admin

- Blazor Server application
- Admin Portal UI (MudBlazor)
- Pages: Dashboard, Extensions, Approvals, Monitoring, Audit, Marketplace
- SignalR hubs for real-time updates
- Depends on: typed HttpClient to PluginRuntime.Api

## PluginRuntime.Capabilities.Abstractions

- ICapability base interface
- IDatabaseCapability
- INetworkCapability
- IStorageCapability
- ICacheCapability
- No implementation — contracts only

## PluginRuntime.Sdk

- IPlugin interface
- PluginContext
- PluginResult
- Published as NuGet package for plugin developers

---

# 4. NAMESPACE CONVENTIONS

```
PluginRuntime.Core.Entities
PluginRuntime.Core.Interfaces
PluginRuntime.Core.ValueObjects
PluginRuntime.Core.Enums

PluginRuntime.Runtime.Execution
PluginRuntime.Runtime.Loading
PluginRuntime.Runtime.Pipeline

PluginRuntime.Security.Manifest
PluginRuntime.Security.Signing
PluginRuntime.Security.Hashing

PluginRuntime.Infrastructure.Persistence
PluginRuntime.Infrastructure.Repositories
PluginRuntime.Infrastructure.Caching

PluginRuntime.Api.Controllers
PluginRuntime.Api.Middleware
PluginRuntime.Api.Configuration
```

---

# 5. DEPENDENCY FLOW

```
SDK (no deps)
     ↑
Core (no external deps)
     ↑
Capabilities.Abstractions
     ↑
Security ← Core
     ↑
Runtime ← Core + Capabilities.Abstractions
     ↑
Infrastructure ← Core
     ↑
Api ← All
```

Rule: Dependencies flow inward. Core has zero external references.

---

# 🏁 END
