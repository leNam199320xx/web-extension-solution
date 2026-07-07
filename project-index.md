# 🧠 PROJECT INDEX
## Metadata-Driven Secure Plugin Runtime (.NET 10)

> **Single Source of Navigation**
>
> This document is the primary entry point for understanding the repository.
> Read this document first before exploring other documentation.

---

# 1. PROJECT VISION

The Metadata-Driven Secure Plugin Runtime is an enterprise-grade platform that enables
dynamic deployment and execution of plugins without restarting the Core Runtime.

Designed around:
- Zero Trust
- Signed Manifest
- Capability-Based Security
- Stateless Runtime
- AI-Assisted Development
- Enterprise Observability

---

# 2. DOCUMENT HIERARCHY

```
README.md                → What the project is
    ↓
project-index.md         → How the repository is organized (you are here)
    ↓
docs/index.md            → How the system works (technical docs)
    ↓
ai/index.md              → AI behavior rules
```

---

# 3. RECOMMENDED READING ORDER

## Step 1 — Product Overview

- `readme.md`

## Step 2 — Repository Navigation

- `project-index.md`

## Step 3 — Architecture

- `docs/architecture/architecture.md`
- `docs/architecture/execution-flow.md`
- `docs/architecture/runtime-engine-spec.md`
- `docs/architecture/runtime-api-spec.md`

## Step 4 — Security

- `docs/security/security-model.md`
- `docs/security/security-enforcement-spec.md`
- `docs/security/capability-system.md`
- `docs/security/threat-model.md`

## Step 5 — Plugin System

- `docs/plugin/plugin-lifecycle.md`
- `docs/plugin/plugin-sdk-spec.md`
- `docs/plugin/manifest-spec.md`
- `docs/plugin/plugin-isolation.md`
- `docs/plugin/versioning-strategy.md`
- `docs/architecture/verification-engine-spec.md`
- `docs/architecture/permission-review-spec.md`
- `docs/architecture/extension-ecosystem.md`
- `docs/architecture/inter-extension-spec.md`

## Step 6 — Runtime

- `docs/runtime/execution-model.md`
- `docs/runtime/plugin-execution-context.md`
- `docs/runtime/resource-governance.md`
- `docs/runtime/scheduler.md`

## Step 7 — Data

- `docs/data/data-model.md`
- `docs/data/database-schema.md`
- `docs/data/event-model.md`

## Step 8 — Infrastructure

- `docs/infrastructure/deployment-model.md`
- `docs/infrastructure/observability.md`
- `docs/infrastructure/non-functional-requirements.md`

## Step 9 — Implementation (for coding)

- `docs/implementation/solution-structure.md`
- `docs/implementation/dependency-manifest.md`
- `docs/implementation/capability-interfaces.md`
- `docs/implementation/error-handling.md`
- `docs/implementation/configuration-model.md`
- `docs/implementation/testing-strategy.md`

## Step 10 — Requirements

- `docs/requirements/catalog/Requirements-Catalog.md`
- `docs/requirements/traceability-matrix.md`

## Step 11 — Standards

- `docs/standards/extension-development-standard.md`
- `docs/standards/sdk-development-standard.md`

## Step 12 — Architecture Decisions

- `docs/adr/`

## Step 13 — AI Layer

- `ai/index.md`
- `.github/copilot-instructions.md`

---

# 4. ARCHITECTURE LAYERS

```
+----------------------------------------------------+
|                 Product Layer                      |
| README / Roadmap                                  |
+----------------------------------------------------+

+----------------------------------------------------+
|             Architecture Layer                     |
| docs/architecture/ + docs/security/               |
+----------------------------------------------------+

+----------------------------------------------------+
|              Plugin Layer                          |
| docs/plugin/ + docs/runtime/                      |
+----------------------------------------------------+

+----------------------------------------------------+
|             Data & Infrastructure Layer            |
| docs/data/ + docs/infrastructure/                 |
+----------------------------------------------------+

+----------------------------------------------------+
|             Implementation Layer                   |
| docs/implementation/ + src/                       |
+----------------------------------------------------+
```

---

# 5. SINGLE SOURCE OF TRUTH

| Topic | Authoritative Document |
|-------|----------------------|
| Architecture | `docs/architecture/architecture.md` |
| Execution Pipeline | `docs/architecture/execution-flow.md` |
| Runtime Components | `docs/architecture/runtime-engine-spec.md` |
| API Endpoints | `docs/architecture/runtime-api-spec.md` |
| Security Model | `docs/security/security-model.md` |
| Validation Pipeline | `docs/security/security-enforcement-spec.md` |
| Capability System | `docs/security/capability-system.md` |
| Plugin Lifecycle | `docs/plugin/plugin-lifecycle.md` |
| Manifest | `docs/plugin/manifest-spec.md` |
| Isolation | `docs/plugin/plugin-isolation.md` |
| Execution Model | `docs/runtime/execution-model.md` |
| Data Model | `docs/data/data-model.md` |
| Database Schema | `docs/data/database-schema.md` |
| Deployment | `docs/infrastructure/deployment-model.md` |
| Observability | `docs/infrastructure/observability.md` |
| Error Handling | `docs/implementation/error-handling.md` |
| Solution Structure | `docs/implementation/solution-structure.md` |
| Extension Standards | `docs/standards/extension-development-standard.md` |
| SDK Standards | `docs/standards/sdk-development-standard.md` |
| Inter-Extension | `docs/architecture/inter-extension-spec.md` |
| Extension Ecosystem | `docs/architecture/extension-ecosystem.md` |
| Verification Engine | `docs/architecture/verification-engine-spec.md` |
| Permission Review | `docs/architecture/permission-review-spec.md` |

---

# 6. IMPLEMENTATION STATUS

```
Architecture & Specification  ████████████████████ 100%
Implementation                □□□□□□□□□□□□□□□□□□□□   0%
Testing                       □□□□□□□□□□□□□□□□□□□□   0%
Production                    □□□□□□□□□□□□□□□□□□□□   0%
```

Next phase: Implementation.

---

# 7. GOLDEN PRINCIPLES

- Security First
- Zero Trust
- Stateless Runtime
- Capability-Based Access
- Manifest-Driven Execution
- Fail Closed
- Immutable Audit
- Explicit Versioning

---

# 8. CONFLICT RESOLUTION

If implementation conflicts with documentation:
> Documentation is the source of truth until an approved ADR updates it.

If documents conflict with each other:
> `docs/security/security-model.md` always takes precedence.

---

# 🏁 END
