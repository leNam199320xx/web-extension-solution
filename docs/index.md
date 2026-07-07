# 📚 Documentation Index
## Metadata-Driven Secure Plugin Runtime (.NET 10)

> **Single Source of Documentation Navigation**
>
> Read this file before exploring individual documents.

---

# Documentation Philosophy

- Every topic has exactly one authoritative document (no duplication)
- Documents cross-reference related content instead of copying
- Architectural decisions are recorded in ADRs
- Implementation must conform to documented specifications
- All documents written in English for consistency

---

# Documentation Structure

```
docs/
├── index.md                          ← You are here
│
├── architecture/
│   ├── architecture.md               → System overview + design principles
│   ├── execution-flow.md             → Full execution pipeline (authoritative)
│   ├── runtime-engine-spec.md        → Core components + interfaces
│   ├── runtime-api-spec.md           → HTTP API surface (all endpoints)
│   ├── system-context.md             → External boundaries + trust zones
│   ├── verification-engine-spec.md   → Automated plugin verification pipeline
│   ├── permission-review-spec.md     → Permission display + approval flow
│   ├── extension-ecosystem.md        → Ecosystem model (repos, registry, workflow)
│   ├── inter-extension-spec.md       → Cross-extension invocation + visibility
│   └── declarative-extension-spec.md → JSON-driven extensions (no code)
│
├── security/
│   ├── security-model.md             → Zero Trust model + hard guarantees
│   ├── security-enforcement-spec.md  → Validation pipeline implementation
│   ├── capability-system.md          → Capability architecture + rules
│   └── threat-model.md               → Threats, risks, mitigations
│
├── plugin/
│   ├── plugin-lifecycle.md           → Upload → Approve → Sign → Execute → Revoke
│   ├── plugin-loading.md             → Assembly loading mechanics
│   ├── plugin-isolation.md           → Isolation levels (L1-L4)
│   ├── plugin-sdk-spec.md            → IPlugin interface + SDK
│   ├── manifest-spec.md              → Signed manifest schema
│   └── versioning-strategy.md        → SemVer, compatibility, migration
│
├── runtime/
│   ├── execution-model.md            → Execution modes, lifecycle states
│   ├── plugin-execution-context.md   → Context interface + usage
│   ├── resource-governance.md        → Timeout, memory, CPU enforcement
│   └── scheduler.md                  → Queue model, concurrency control
│
├── data/
│   ├── data-model.md                 → Logical entities + relationships
│   ├── database-schema.md            → Physical PostgreSQL schema (DDL)
│   └── event-model.md                → Domain events + C# definitions
│
├── infrastructure/
│   ├── deployment-model.md           → Topology, containers, K8s
│   ├── observability.md              → Logs, metrics, traces, alerting
│   ├── scaling-model.md              → Horizontal scaling strategy
│   ├── disaster-recovery.md          → RTO, RPO, backup, failover
│   └── non-functional-requirements.md → Performance, availability, capacity
│
├── implementation/
│   ├── index.md                      → Implementation docs reading order
│   ├── solution-structure.md         → .NET solution layout + namespaces
│   ├── dependency-manifest.md        → NuGet packages
│   ├── capability-interfaces.md      → Full C# interface definitions
│   ├── error-handling.md             → Error codes + exception hierarchy
│   ├── configuration-model.md        → appsettings + options pattern
│   ├── authentication-flow.md        → JWT setup + endpoint auth
│   ├── database-migrations.md        → Migration commands + strategy
│   ├── testing-strategy.md           → Test pyramid + frameworks
│   ├── cicd-pipeline.md              → Build/test/deploy pipeline
│   └── plugin-packaging.md           → Plugin ZIP format + upload
│
├── requirements/
│   ├── readme.md
│   ├── traceability-matrix.md        → FR → Architecture → Implementation mapping
│   ├── functional/                   → FR-100 through FR-900
│   └── business-rules/              → BR-200 through BR-400
│
├── standards/
│   ├── extension-development-standard.md → Rules for plugin developers
│   └── sdk-development-standard.md       → Rules for platform SDK team
│
├── adr/
│   ├── ADR-0001-zero-trust-runtime.md
│   ├── ADR-0002-capability-system.md
│   ├── ADR-0003-signed-manifest.md
│   └── ADR-0004-stateless-runtime.md
│
└── diagrams/                         → Visual diagrams (Mermaid/text)
```

---

# Single Source of Truth

| Topic | Authoritative Document |
|-------|----------------------|
| Architecture overview | `architecture/architecture.md` |
| Execution pipeline | `architecture/execution-flow.md` |
| Runtime components | `architecture/runtime-engine-spec.md` |
| API endpoints | `architecture/runtime-api-spec.md` |
| Security model | `security/security-model.md` |
| Validation pipeline | `security/security-enforcement-spec.md` |
| Capability system | `security/capability-system.md` |
| Threats | `security/threat-model.md` |
| Plugin lifecycle | `plugin/plugin-lifecycle.md` |
| Manifest schema | `plugin/manifest-spec.md` |
| Isolation levels | `plugin/plugin-isolation.md` |
| Execution model | `runtime/execution-model.md` |
| Resource governance | `runtime/resource-governance.md` |
| Database schema | `data/database-schema.md` |
| Events | `data/event-model.md` |
| Deployment | `infrastructure/deployment-model.md` |
| Observability | `infrastructure/observability.md` |
| Error codes | `implementation/error-handling.md` |
| Capability interfaces | `implementation/capability-interfaces.md` |
| Solution structure | `implementation/solution-structure.md` |
| Extension standards | `standards/extension-development-standard.md` |
| SDK standards | `standards/sdk-development-standard.md` |
| Verification engine | `architecture/verification-engine-spec.md` |
| Permission review | `architecture/permission-review-spec.md` |
| Extension ecosystem | `architecture/extension-ecosystem.md` |
| Inter-extension communication | `architecture/inter-extension-spec.md` |
| Declarative extensions | `architecture/declarative-extension-spec.md` |

No document should redefine information owned by another document.

---

# Reading Order

1. **Architecture** — `architecture.md` → `execution-flow.md` → `runtime-engine-spec.md`
2. **Security** — `security-model.md` → `security-enforcement-spec.md` → `capability-system.md`
3. **Plugin** — `plugin-lifecycle.md` → `manifest-spec.md` → `plugin-isolation.md`
4. **Runtime** — `execution-model.md` → `plugin-execution-context.md` → `resource-governance.md`
5. **Data** — `data-model.md` → `database-schema.md` → `event-model.md`
6. **Implementation** — `solution-structure.md` → `dependency-manifest.md` → start coding

---

# Cross-Reference Rules

- Reference related documents instead of duplicating content
- Use consistent terminology across all documents
- Remain implementation-agnostic in architecture docs
- Implementation docs provide concrete code/config details

---

# 🏁 END
