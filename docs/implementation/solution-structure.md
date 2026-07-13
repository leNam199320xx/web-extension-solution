# 🧱 Solution Structure - .NET 10

---

# 1. PURPOSE

Định nghĩa cấu trúc solution cụ thể để bắt đầu implementation.

---

# 2. SOLUTION LAYOUT

```
src/
├── PluginRuntime.slnx
│
├── Aspire/
│   ├── PluginRuntime.AppHost/               → .NET Aspire orchestrator (Dashboard + all services)
│   └── PluginRuntime.ServiceDefaults/       → Shared OpenTelemetry, health checks, service discovery
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
├── PluginRuntime.Api/                       → ASP.NET Core Web API host (port 6100)
├── PublicApiGateway/                        → API Gateway / reverse proxy (port 6200)
│
├── Marketplace/
│   ├── PluginRuntime.Marketplace/           → Blazor WASM Marketplace app
│   └── PluginRuntime.Marketplace.Server/    → ASP.NET Core host for WASM (port 6300)
│
├── ConsumerPortal/
│   ├── PluginRuntime.ConsumerPortal/        → Blazor WASM Consumer Portal app
│   └── PluginRuntime.ConsumerPortal.Server/ → ASP.NET Core host for WASM (port 6400)
│
├── Admin/
│   └── PluginRuntime.Admin/                 → Blazor Server Admin Portal (port 6500)
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
├── Plugins/
│   └── System.Auth/                         → Built-in authentication plugin
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
- Port 6100 (HTTP) / 6101 (HTTPS)
- Depends on: all projects

## PublicApiGateway

- API Gateway / reverse proxy
- Routes requests to PluginRuntime.Api
- Rate limiting, caching, auth forwarding
- Port 6200 (HTTP) / 6201 (HTTPS)
- Depends on: PluginRuntime.Api (upstream)

## PluginRuntime.Admin

- Blazor Server application (Interactive Server render mode)
- Admin Portal UI (MudBlazor)
- Pages: Dashboard, Extensions, Approvals, Monitoring, Audit, Marketplace
- SignalR hubs for real-time updates
- Port 6500 (HTTP) / 6501 (HTTPS)
- Depends on: typed HttpClient to PluginRuntime.Api

## PluginRuntime.Marketplace / PluginRuntime.Marketplace.Server

- **Marketplace** — Blazor WebAssembly standalone app (runs in browser)
- **Marketplace.Server** — ASP.NET Core host serving WASM static files
- Plugin discovery, upload, subscription management
- Port 6300 (HTTP) / 6301 (HTTPS)
- Depends on: API via HttpClient (configured at runtime)

## PluginRuntime.ConsumerPortal / PluginRuntime.ConsumerPortal.Server

- **ConsumerPortal** — Blazor WebAssembly standalone app (runs in browser)
- **ConsumerPortal.Server** — ASP.NET Core host serving WASM static files
- API key management, usage analytics, billing, subscription
- Port 6400 (HTTP) / 6401 (HTTPS)
- Depends on: API via HttpClient (configured at runtime)

## Aspire (PluginRuntime.AppHost + ServiceDefaults)

- **AppHost** — .NET Aspire orchestrator using `Aspire.AppHost.Sdk 13.2.4`
  - Registers all 5 service projects via `AddProject<T>()`
  - Aspire Dashboard for telemetry, logs, traces
  - Port 6000 (HTTP) / 6001 (HTTPS) for dashboard
  - Dependency graph: API → Gateway → (Marketplace, Consumer, Admin)
- **ServiceDefaults** — Shared Aspire configuration
  - OpenTelemetry (metrics, tracing, logging)
  - Health checks (`/health`, `/alive`)
  - Service discovery + HTTP resilience

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
     ↑
Gateway ← Api (upstream)
     ↑
Frontend Portals ← Gateway (via HTTP)
     ↑
AppHost (Aspire) ← All projects (orchestration only)
```

Rule: Dependencies flow inward. Core has zero external references.

---

# 6. ASPIRE ORCHESTRATION

## AppHost (`src/Aspire/PluginRuntime.AppHost`)

- SDK: `Aspire.AppHost.Sdk` version 13.2.4
- Pattern: `DistributedApplication.CreateBuilder(args)` + `AddProject<T>()`
- Dashboard: automatic Aspire Dashboard for telemetry, logs, distributed traces
- Proxy disabled for frontend portals (Blazor handles client-side routing)

## ServiceDefaults (`src/Aspire/PluginRuntime.ServiceDefaults`)

- OpenTelemetry: metrics (ASP.NET Core, HTTP, Runtime), tracing, structured logging
- OTLP exporter: auto-configured when `OTEL_EXPORTER_OTLP_ENDPOINT` is set
- Health checks: `/health` (all), `/alive` (liveness only)
- Service discovery + HTTP resilience (Polly)

## Port Allocation

| Service | HTTP | HTTPS | Type |
|---------|------|-------|------|
| Aspire Dashboard | 6000 | 6001 | Aspire built-in |
| API Backend | 6100 | 6101 | ASP.NET Core Web API |
| API Gateway | 6200 | 6201 | ASP.NET Core (YARP) |
| Marketplace | 6300 | 6301 | Blazor WASM + Server host |
| Consumer Portal | 6400 | 6401 | Blazor WASM + Server host |
| Admin Portal | 6500 | 6501 | Blazor Server (Interactive) |

## Frontend Architecture

| Portal | SDK | Render Mode | Notes |
|--------|-----|-------------|-------|
| Admin | `Microsoft.NET.Sdk.Web` | Interactive Server (SignalR) | Runs directly as ASP.NET Core |
| Consumer | `BlazorWebAssembly` | Client-side (WASM) | Needs `.Server` host project |
| Marketplace | `BlazorWebAssembly` | Client-side (WASM) | Needs `.Server` host project |

Blazor WASM apps require a thin Server wrapper (`UseBlazorFrameworkFiles()` + `UseStaticFiles()` + `MapFallbackToFile("index.html")`) because standalone WASM has no Kestrel server for Aspire to orchestrate.

---

# 7. LOCAL DEVELOPMENT

## Option 1: Aspire (recommended)

```bash
dotnet run --project src/Aspire/PluginRuntime.AppHost
```

- Aspire Dashboard opens automatically
- All 5 services start with dependency ordering (API → Gateway → Portals)
- Telemetry collected and viewable in dashboard

## Option 2: Batch file

```bash
run-all.bat
```

- Same ports, no Aspire Dashboard
- Simpler — uses `dotnet run` for each project

## Option 3: Individual service

```bash
cd src/PluginRuntime.Api
set ASPNETCORE_URLS=http://localhost:6100
dotnet run
```

---

# 🏁 END
