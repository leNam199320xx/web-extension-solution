# 🧠 PROJECT INDEX
## Metadata-Driven Secure Plugin Runtime (.NET 10)

> **Single Source of Navigation**
>
> This document is the primary entry point for understanding the repository.
>
> Read this document first before exploring other documentation.

---

# 1. PROJECT VISION

The Metadata-Driven Secure Plugin Runtime is an enterprise-grade platform that enables
dynamic deployment and execution of plugins without restarting the Core Runtime.

The platform is designed around:

- Zero Trust
- Signed Manifest
- Capability-Based Security
- Stateless Runtime
- AI-Assisted Development
- Enterprise Observability

---

# 2. DOCUMENT HIERARCHY

```
README
    │
    ▼
PROJECT-INDEX
    │
    ├──────── docs
    ├──────── ai
    └──────── .github
```

README explains **what** the project is.

PROJECT-INDEX explains **how the repository is organized**.

The `/docs` folder explains **how the system works**.

---

# 3. RECOMMENDED READING ORDER

## Step 1 — Product Overview

```
README.md
```

---

## Step 2 — Repository Navigation

```
PROJECT-INDEX.md
```

---

## Step 3 — Architecture

```
docs/architecture.md
docs/runtime-engine-spec.md
docs/runtime-api-spec.md
```

---

## Step 4 — Security

```
docs/security-model.md
docs/threat-model.md
docs/security-enforcement-spec.md
```

---

## Step 5 — Plugin System

```
docs/plugin-lifecycle.md
docs/plugin-sdk-spec.md
docs/manifest-spec.md
docs/capability-system.md
docs/versioning-strategy.md
```

---

## Step 6 — Infrastructure

```
docs/deployment-model.md
docs/non-functional-requirements.md
docs/observability.md
```

---

## Step 7 — Domain

```
docs/data-model.md
```

---

## Step 8 — Architecture Decisions

```
docs/adr/
```

---

## Step 9 — AI Layer

```
ai/
.github/
```

---

# 4. DOCUMENT CATEGORIES

## 📘 Product

Purpose:

Explain the project.

Files:

- README.md
- roadmap.md

---

## 🏗 Architecture

Purpose:

Describe runtime architecture.

Files:

- architecture.md
- runtime-engine-spec.md
- runtime-api-spec.md

---

## 🔐 Security

Purpose:

Describe Zero Trust implementation.

Files:

- security-model.md
- threat-model.md
- security-enforcement-spec.md

---

## 🔌 Plugin

Purpose:

Describe plugin ecosystem.

Files:

- plugin-sdk-spec.md
- plugin-lifecycle.md
- manifest-spec.md
- capability-system.md
- versioning-strategy.md

---

## 🚀 Infrastructure

Purpose:

Describe deployment and operations.

Files:

- deployment-model.md
- observability.md
- non-functional-requirements.md

---

## 🗄 Data

Purpose:

Describe persistence model.

Files:

- data-model.md

---

## 📝 ADR

Purpose:

Explain why architectural decisions were made.

Files:

- docs/adr/

---

## 🤖 AI

Purpose:

Guide AI-assisted development.

Files:

```
ai/
```

Contains:

- rules.md
- coding-guidelines.md
- output-format.md
- anti-patterns.md

---

## ⚙ GitHub Copilot

Purpose:

Repository-level AI instructions.

Files:

```
.github/copilot-instructions.md
```

---

# 5. ARCHITECTURE LAYERS

```
+----------------------------------------------------+
|                 Product Layer                      |
| README / Roadmap                                  |
+----------------------------------------------------+

+----------------------------------------------------+
|             Architecture Layer                     |
| docs/*.md                                          |
+----------------------------------------------------+

+----------------------------------------------------+
|              Security Layer                        |
| Zero Trust / Manifest / Capability                 |
+----------------------------------------------------+

+----------------------------------------------------+
|             Runtime Layer                          |
| Core Runtime (.NET 10)                             |
+----------------------------------------------------+

+----------------------------------------------------+
|             Infrastructure Layer                   |
| PostgreSQL / Redis / KMS / Storage                 |
+----------------------------------------------------+
```

---

# 6. SINGLE SOURCE OF TRUTH

Each topic has exactly one authoritative document.

| Topic | Source |
|---------|--------|
| Architecture | architecture.md |
| Runtime | runtime-engine-spec.md |
| Runtime API | runtime-api-spec.md |
| Plugin Lifecycle | plugin-lifecycle.md |
| Manifest | manifest-spec.md |
| Capability | capability-system.md |
| Security | security-model.md |
| Threats | threat-model.md |
| Security Enforcement | security-enforcement-spec.md |
| Versioning | versioning-strategy.md |
| Deployment | deployment-model.md |
| Data Model | data-model.md |
| Observability | observability.md |
| NFR | non-functional-requirements.md |
| ADR | docs/adr/ |

Duplicate documentation should be avoided.

---

# 7. IMPLEMENTATION ROADMAP

Current project maturity:

```
Architecture
████████████████████ 100%

Specification
████████████████████ 100%

Implementation
□□□□□□□□□□□□□□ 0%

Testing
□□□□□□□□□□□□□□ 0%

Production
□□□□□□□□□□□□□□ 0%
```

The next phase is implementation.

---

# 8. AI ENTRY POINT

AI assistants should read documents in this order:

1. PROJECT-INDEX.md
2. README.md
3. docs/INDEX.md
4. ai/INDEX.md
5. .github/copilot-instructions.md

Do not generate implementation that contradicts the documented architecture.

---

# 9. GOLDEN PRINCIPLES

The project follows these principles:

- Security First
- Zero Trust
- Stateless Runtime
- Capability-Based Access
- Manifest-Driven Execution
- Explicit Versioning
- Fail Closed
- Immutable Audit
- AI-Friendly Repository

---

# 10. FINAL NOTE

This repository is an **architecture-first project**.

Documentation defines the system.

Implementation must follow the documentation.

If implementation conflicts with documentation:

> Documentation is considered the source of truth until an approved Architecture Decision Record (ADR) updates it.

---

# 🏁 END OF PROJECT INDEX