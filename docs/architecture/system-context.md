# 🌐 System Context
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

Defines external and internal system boundaries, actors, and trust zones.

---

# 2. SYSTEM CONTEXT DIAGRAM

```
                ┌────────────────────┐
                │    Developers      │
                └────────┬───────────┘
                         │ Upload Plugin
                         ▼
        ┌────────────────────────────────────┐
        │        Approval Platform          │
        │  - Scan, Validate, Sign           │
        └──────────────┬────────────────────┘
                       │
                       ▼
        ┌────────────────────────────────────┐
        │      Plugin Repository            │
        │  - Immutable binary + manifest    │
        └──────────────┬────────────────────┘
                       │
                       ▼
        ┌────────────────────────────────────┐
        │        Core Runtime (.NET 10)     │
        │  - Execute, Enforce, Observe      │
        └──────────────┬────────────────────┘
                       │
       ┌───────────────┼──────────────────────┐
       ▼               ▼                      ▼
 ┌──────────┐   ┌──────────────┐   ┌──────────────┐
 │ PostgreSQL│   │ Redis Cache  │   │ Observability│
 └──────────┘   └──────────────┘   └──────────────┘
                       │
                       ▼
                ┌──────────────┐
                │   KMS / HSM  │
                └──────────────┘
```

---

# 3. TRUST BOUNDARIES

| Boundary | Between | Trust Level |
|----------|---------|-------------|
| B1 | Developer → Approval Platform | Untrusted input (code, metadata) |
| B2 | Approval → Repository | Trusted only after scan + sign |
| B3 | Repository → Runtime | Validated at every load (re-verified) |
| B4 | Runtime → Infrastructure | Controlled via capabilities only |
| B5 | Plugin → Runtime | Zero Trust (always) |

Key rule: trust is NEVER carried across boundaries. Each component re-validates.

---

# 4. ACTORS

## Human Actors

| Actor | Role |
|-------|------|
| Developer | Uploads plugins |
| Security Reviewer | Approves/rejects plugins |
| Admin | Manages platform, revokes plugins |
| Auditor | Reviews audit logs (read-only) |

## System Actors

| Actor | Responsibility |
|-------|---------------|
| Core Runtime | Executes plugins, enforces security |
| Approval Engine | Scans, validates, signs |
| Capability Engine | Controls resource access |
| Plugin Loader | Manages assembly lifecycle |
| Scheduler | Controls execution dispatch |

---

# 5. EXTERNAL DEPENDENCIES

| System | Purpose | Access Pattern |
|--------|---------|---------------|
| PostgreSQL | Metadata, audit, execution history | Direct from Core (EF Core) |
| Redis | Cache, revocation list, locks | Direct from Core (StackExchange.Redis) |
| KMS / HSM | Signing keys, certificates | Core → KMS API |
| Object Storage | Plugin binaries | Core reads at load time |
| OpenTelemetry Collector | Logs, metrics, traces | Core pushes via OTLP |

---

# 6. DATA FLOW

```
Developer → Approval Platform → Repository → Runtime → Infrastructure
                                                    → Observability
```

---

# 7. DESIGN PRINCIPLE

> The system is a chain of trust boundaries, not a single trusted system.
> No component trusts another by default. Every interaction is validated.

---

# 🏁 END
