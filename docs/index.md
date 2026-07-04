# 📚 Documentation Index
## Metadata-Driven Secure Plugin Runtime (.NET 10)

> **Single Source of Documentation Navigation**
>
> This document provides a structured overview of all technical documentation.
>
> Read this file before exploring individual documents.

---

# 📖 Documentation Philosophy

The documentation follows an **Architecture-First** approach.

Rules:

- Every topic has exactly one authoritative document.
- Documents should not duplicate information.
- Cross-reference related documents instead of copying content.
- Architectural decisions are recorded in ADRs.
- Implementation must conform to documented specifications.

---

# 🗂 Documentation Structure

```
docs/
│
├── INDEX.md
│
├── Architecture
│   ├── architecture.md
│   ├── runtime-engine-spec.md
│   └── runtime-api-spec.md
│
├── Security
│   ├── security-model.md
│   ├── threat-model.md
│   ├── security-enforcement-spec.md
│   └── capability-system.md
│
├── Plugin
│   ├── plugin-lifecycle.md
│   ├── plugin-sdk-spec.md
│   ├── manifest-spec.md
│   └── versioning-strategy.md
│
├── Infrastructure
│   ├── deployment-model.md
│   ├── observability.md
│   └── non-functional-requirements.md
│
├── Data
│   └── data-model.md
│
└── adr/
```

---

# 📑 Reading Order

## 1. Foundation

| Document | Purpose |
|----------|---------|
| architecture.md | Overall system architecture |
| runtime-engine-spec.md | Core Runtime architecture |
| runtime-api-spec.md | Runtime API contracts |

---

## 2. Security

| Document | Purpose |
|----------|---------|
| security-model.md | Zero Trust architecture |
| threat-model.md | Threat analysis |
| security-enforcement-spec.md | Runtime enforcement |
| capability-system.md | Capability-based authorization |

---

## 3. Plugin Platform

| Document | Purpose |
|----------|---------|
| plugin-lifecycle.md | Plugin lifecycle |
| plugin-sdk-spec.md | SDK design |
| manifest-spec.md | Signed Manifest specification |
| versioning-strategy.md | Compatibility and versioning |

---

## 4. Infrastructure

| Document | Purpose |
|----------|---------|
| deployment-model.md | Deployment topology |
| observability.md | Logging, metrics, tracing |
| non-functional-requirements.md | Production quality requirements |

---

## 5. Data

| Document | Purpose |
|----------|---------|
| data-model.md | Domain entities and persistence model |

---

## 6. Architecture Decisions

Location:

```
docs/adr/
```

Purpose:

Document major architectural decisions.

Examples:

- Zero Trust Runtime
- Capability-Based Security
- Stateless Runtime
- Signed Manifest

---

# 🏗 Dependency Graph

```
README
      │
      ▼
architecture.md
      │
      ├──────── runtime-engine-spec.md
      ├──────── runtime-api-spec.md
      │
      ├──────── security-model.md
      │             │
      │             ├──────── threat-model.md
      │             └──────── security-enforcement-spec.md
      │
      ├──────── plugin-lifecycle.md
      │             │
      │             ├──────── plugin-sdk-spec.md
      │             ├──────── manifest-spec.md
      │             └──────── versioning-strategy.md
      │
      ├──────── deployment-model.md
      │             │
      │             ├──────── observability.md
      │             └──────── non-functional-requirements.md
      │
      └──────── data-model.md
```

---

# 📌 Single Source of Truth

| Topic | Document |
|--------|----------|
| Architecture | architecture.md |
| Runtime | runtime-engine-spec.md |
| Runtime API | runtime-api-spec.md |
| Security | security-model.md |
| Threats | threat-model.md |
| Capability System | capability-system.md |
| Plugin Lifecycle | plugin-lifecycle.md |
| Plugin SDK | plugin-sdk-spec.md |
| Manifest | manifest-spec.md |
| Versioning | versioning-strategy.md |
| Deployment | deployment-model.md |
| Observability | observability.md |
| Non-Functional Requirements | non-functional-requirements.md |
| Data Model | data-model.md |
| Architecture Decisions | docs/adr/ |

No document should redefine information owned by another document.

---

# 🔗 Cross-Reference Rules

Each document should:

- Reference related documents instead of duplicating content.
- Use consistent terminology.
- Follow the naming conventions defined in the project.
- Remain implementation-agnostic unless explicitly specified.

---

# 📈 Documentation Status

| Area | Status |
|------|--------|
| Architecture | ✅ Complete |
| Runtime | ✅ Complete |
| Security | ✅ Complete |
| Plugin Platform | ✅ Complete |
| Infrastructure | ✅ Complete |
| Data Model | ✅ Complete |
| ADR | ✅ Complete |

Overall documentation coverage: **Production Ready**.

---

# 📝 Maintenance Rules

When introducing a new feature:

1. Update the relevant specification document.
2. Add or update an ADR if the architecture changes.
3. Update this index if a new document is added.
4. Avoid duplicate documentation.

Documentation must evolve alongside the implementation.

---

# 🏁 End of Documentation Index