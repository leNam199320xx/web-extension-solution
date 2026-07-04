# 🌐 System Context
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

This document defines the **external and internal system boundaries**.

It shows:

- What is inside the system
- What is outside the system
- How systems interact
- Trust boundaries

---

# 2. HIGH LEVEL CONTEXT

```
                ┌────────────────────┐
                │    Developers      │
                └────────┬───────────┘
                         │ Upload Plugin
                         ▼
        ┌────────────────────────────────────┐
        │        Approval Platform          │
        │  - Scan                            │
        │  - Validate                       │
        │  - Sign Manifest                 │
        └──────────────┬────────────────────┘
                       │
                       ▼
        ┌────────────────────────────────────┐
        │      Plugin Repository            │
        │  - Store binaries                 │
        │  - Store manifests               │
        └──────────────┬────────────────────┘
                       │
                       ▼
        ┌────────────────────────────────────┐
        │        Core Runtime (.NET 10)     │
        │  - Execute plugins                │
        │  - Enforce security              │
        │  - Manage lifecycle              │
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

## Boundary 1 — Developer → Platform

Untrusted input:

- Plugin code
- Metadata
- Dependencies

---

## Boundary 2 — Approval System

Responsible for:

- Security scanning
- Validation
- Signing

Trusted only after verification.

---

## Boundary 3 — Repository

Immutable storage:

- Plugins
- Manifests

No execution allowed here.

---

## Boundary 4 — Runtime

Most critical boundary.

Rules:

- Never trust plugin
- Always validate manifest
- Always enforce capability

---

## Boundary 5 — Infrastructure

External systems:

- Database
- Cache
- KMS
- Monitoring

Access only via controlled interfaces.

---

# 4. SYSTEM ACTORS

## Human Actors

- Developer
- Security Reviewer
- Admin
- Auditor

---

## System Actors

- Core Runtime
- Approval Engine
- Capability Engine
- Plugin Loader

---

# 5. EXTERNAL DEPENDENCIES

## PostgreSQL

Stores:

- Metadata
- Audit logs
- Execution history

---

## Redis

Stores:

- Cache
- Revocation list
- Temporary locks

---

## KMS / HSM

Stores:

- Signing keys
- Certificates

---

## Observability Stack

- Logs
- Metrics
- Traces

Examples:

- OpenTelemetry
- Prometheus
- Grafana

---

# 6. SYSTEM RESPONSIBILITIES

## Core Runtime

- Execute plugins
- Enforce security
- Manage lifecycle
- Collect telemetry

---

## Approval System

- Validate plugins
- Scan vulnerabilities
- Sign manifests

---

## Repository

- Store artifacts
- Versioning
- Immutable history

---

# 7. DATA FLOW SUMMARY

```
Developer
   ↓
Approval System
   ↓
Repository
   ↓
Runtime Execution
   ↓
Infrastructure Services
```

---

# 8. NON-TRUST MODEL

No component trusts another by default.

Every interaction must be validated.

---

# 9. DESIGN PRINCIPLE

> The system is a chain of trust boundaries, not a single trusted system.

---

# 🏁 END OF SYSTEM CONTEXT