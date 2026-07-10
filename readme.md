# Plugin Runtime Platform

A secure, metadata-driven plugin runtime system that enables dynamic loading and execution of plugins at runtime — governed by signed manifests and capability-based access control.

## What It Does

This platform allows organizations to safely run untrusted plugin code in isolated environments. Every plugin must pass cryptographic verification before execution, and can only access resources explicitly declared in its manifest.

## System Components

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Plugin Runtime Platform                       │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────────┐   ┌──────────────────┐   ┌────────────────┐  │
│  │ Marketplace      │   │ Consumer Portal  │   │ Admin Portal   │  │
│  │ Portal           │   │                  │   │                │  │
│  └────────┬─────────┘   └────────┬─────────┘   └───────┬────────┘  │
│           │                      │                      │           │
│           └──────────────────────┼──────────────────────┘           │
│                                  │                                   │
│                    ┌─────────────▼─────────────┐                    │
│                    │   Public API Gateway       │                    │
│                    └─────────────┬─────────────┘                    │
│                                  │                                   │
│                    ┌─────────────▼─────────────┐                    │
│                    │   Unified API (Backend)    │                    │
│                    └───────────────────────────┘                    │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

## Projects

| # | Project | Description |
|---|---------|-------------|
| 1 | **Unified API** | The core backend. Manages tenants, billing, subscriptions, plugin packages, and gateway synchronization. All business logic lives here. |
| 2 | **Public API Gateway** | The public entry point for API consumers. Authenticates requests via API keys, enforces rate limits and daily quotas, meters usage, and forwards requests to the backend. |
| 3 | **Plugin Runtime Engine** | The execution engine. Loads plugin DLLs into isolated sandboxes, verifies signatures, resolves capabilities, and executes plugins safely. |
| 4 | **Marketplace Portal** | A web frontend for plugin developers. Browse, search, upload, and manage extensions. Review permissions and subscribe to other extensions. |
| 5 | **Consumer Portal** | A web frontend for API consumers. View usage analytics, manage API keys, handle billing, change plans, and access documentation. |
| 6 | **Admin Portal** | An internal management interface for platform operators. Approve plugins, manage tenants, review security scans, and monitor the system. |

## Key Features

**For Plugin Developers**
- Upload and publish plugins through the Marketplace
- Declare capabilities and permissions in a manifest
- Subscribe to other extensions for inter-plugin communication
- Track plugin usage and subscriber counts

**For API Consumers**
- Self-service registration with plan selection (Free / Pro / Enterprise)
- API key management with rotation and expiration
- Real-time usage analytics with charts
- Automated billing with Stripe integration

**For Platform Operators**
- Zero-trust security model — every plugin is untrusted until verified
- Cryptographic signature verification before execution
- Capability-based access control — plugins can only use declared resources
- Multi-tenant isolation with per-tenant rate limits and quotas
- Full audit trail of all administrative actions

**Infrastructure**
- Multi-database support: PostgreSQL, SQLite, or JSON files
- Redis for caching, rate limiting, and real-time notifications
- OpenTelemetry for distributed tracing and metrics
- .NET Aspire orchestration for one-command startup

## Getting Started

```bash
# Run the entire platform with Aspire (requires Docker)
cd src/Aspire/PluginRuntime.AppHost
dotnet run

# Or run individual projects
cd src/PluginRuntime.Api
dotnet run

cd src/PublicApiGateway
dotnet run
```

## Project Structure

```
src/
├── Aspire/                    → Orchestration (runs everything together)
├── PluginRuntime.Api/         → Unified backend API (modular monolith)
├── PublicApiGateway/          → Public-facing API gateway
├── Core/                      → Plugin runtime engine
├── Marketplace/               → Developer marketplace (web frontend)
├── ConsumerPortal/            → API consumer portal (web frontend)
├── Admin/                     → Admin management portal
├── SDK/                       → Plugin development SDK
├── Capabilities/              → Infrastructure access layers for plugins
├── Infrastructure/            → Database and external service integrations
└── Tests/                     → All test projects
```

## License

Proprietary. All rights reserved.
