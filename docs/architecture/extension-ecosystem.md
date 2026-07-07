# 🌐 Extension Ecosystem Architecture

---

# 1. PURPOSE

Defines the overall ecosystem model for extensions — how they are developed, stored, distributed, and how they relate to the Core Platform.

---

# 2. ECOSYSTEM OVERVIEW

```
┌─────────────────────────────────────────────────────────┐
│                   Platform Repos (Core Team)             │
│                                                         │
│  web-extension-solution/    → Core Runtime + Admin Portal│
│  plugin-runtime-sdk/        → SDK NuGet package         │
│  extension-template/        → Starter template for devs │
│                                                         │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│              Extension Repos (Per Extension)             │
│                                                         │
│  payment-extension/         → 1 extension = 1 repo     │
│  order-extension/                                       │
│  shipping-extension/                                    │
│  analytics-extension/                                   │
│  (internal or third-party)                              │
│                                                         │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│              Shared Infrastructure                       │
│                                                         │
│  Extension Registry (DB)    → Tracks all extensions     │
│  Plugin Repository (Storage)→ Stores .plugin.zip        │
│  Extension Marketplace (UI) → Browse & subscribe        │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

# 3. DESIGN DECISIONS

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Extension per repo | ✅ Yes | Zero Trust: untrusted code stays separate from platform |
| Monorepo for extensions | ❌ No | Third parties can't access platform repo |
| SDK distribution | NuGet package | Standard .NET distribution mechanism |
| Template distribution | GitHub template repo or `dotnet new` | Easy bootstrapping |
| Upload model | Package upload (.plugin.zip) | Decoupled from source control |

---

# 4. EXTENSION DEVELOPER WORKFLOW

```
1. Clone template or `dotnet new plugin`
       ↓
2. Develop extension (own repo, own CI/CD)
       ↓
3. Follow Extension Development Standard
       ↓
4. Run local verification (SDK CLI: `plugin verify`)
       ↓
5. Build package: `plugin pack` → my-extension-1.0.0.plugin.zip
       ↓
6. Upload to platform (Admin Portal or CLI: `plugin upload`)
       ↓
7. Verification Engine runs (automated)
       ↓
8. Permission Review by admin
       ↓
9. Manifest signed (KMS)
       ↓
10. Available in Extension Registry
```

---

# 5. PLATFORM REPOSITORIES

## 5.1 web-extension-solution (this repo)

Contains:
- Core Runtime Engine
- Security Pipeline
- Capability Implementations
- Verification Engine
- Admin Portal (Blazor)
- Database + Infrastructure

## 5.2 plugin-runtime-sdk

Published as NuGet: `PluginRuntime.Sdk`

Contains:
- `IPlugin` interface
- `IPluginExecutionContext`
- `ICapability` interfaces (Database, Network, Storage, Cache, Extension)
- `PluginResult` record
- `IPluginLogger`

## 5.3 extension-template

Template repo for developers:
- Pre-configured project structure
- manifest.json template
- permissions.json template
- Sample tests
- CI/CD pipeline template
- README template

---

# 6. EXTENSION REGISTRY

Central database tracking all published extensions:

```sql
CREATE TABLE extension_registry (
    extension_id    VARCHAR(200)    PRIMARY KEY,
    display_name    VARCHAR(500)    NOT NULL,
    description     TEXT,
    author_id       UUID            NOT NULL,
    visibility      VARCHAR(50)     NOT NULL DEFAULT 'Private',
    category        VARCHAR(100),
    latest_version  VARCHAR(50),
    total_versions  INT             DEFAULT 0,
    subscribers     INT             DEFAULT 0,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- visibility: Public, Private, Subscription
```

---

# 7. EXTENSION VISIBILITY MODEL

See `docs/architecture/inter-extension-spec.md` for full inter-extension communication spec.

| Visibility | Who can invoke | Who can see |
|-----------|---------------|-------------|
| **Private** | Only the owner/team | Only owner in registry |
| **Public** | Any extension (with permission declared) | Everyone in marketplace |
| **Subscription** | Only approved subscribers | Everyone can see, must request access |

---

# 8. SDK CLI TOOLING (Future)

```bash
# Create new extension from template
dotnet new plugin -n MyExtension

# Run local verification
plugin verify ./my-extension-1.0.0.plugin.zip

# Build package
plugin pack -o ./dist/

# Upload to platform
plugin upload ./dist/my-extension-1.0.0.plugin.zip --api https://runtime.company.com

# Check status
plugin status my-extension --version 1.0.0
```

---

# 9. EXTENSION LIFECYCLE (Ecosystem View)

```
Developer creates repo
       ↓
Develops + tests locally
       ↓
Builds .plugin.zip package
       ↓
Uploads to platform
       ↓
Verified automatically (7 stages)
       ↓
Reviewed by admin (permissions)
       ↓
Signed + stored in Plugin Repository
       ↓
Registered in Extension Registry
       ↓
Available for execution (and for other extensions if Public/Subscribed)
       ↓
Monitoring + audit
       ↓
Updates via new version upload (same flow)
       ↓
Revocation (if security issue)
```

---

# 10. MULTI-TENANT CONSIDERATIONS

If platform is multi-tenant:
- Each tenant has own Extension Registry view
- Private extensions scoped to tenant
- Public extensions visible across tenants (if platform-wide marketplace)
- Subscription requests are per-tenant

---

# 11. DESIGN PRINCIPLE

> Extensions are independent, untrusted software units.
> They live in separate repositories, follow published standards,
> and enter the system only through the verification pipeline.
> No shortcut exists.

---

# 🏁 END
