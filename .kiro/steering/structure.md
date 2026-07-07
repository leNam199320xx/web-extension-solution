# Project Structure

## Layout

```
/
├── .github/              → GitHub Copilot instructions
├── .kiro/steering/       → Kiro AI steering rules
├── ai/                   → AI behavior control layer (rules, guidelines, anti-patterns)
├── docs/                 → All system documentation (architecture-first)
│   ├── adr/             → Architecture Decision Records
│   ├── architecture/    → System architecture, runtime specs, API specs
│   ├── data/            → Data model, schema, events, migrations
│   ├── diagrams/        → Mermaid/text diagrams (flows, ERD, deployment)
│   ├── implementation/  → Solution structure, packages, config, testing, CI/CD
│   ├── infrastructure/  → Deployment, scaling, DR, observability, NFRs
│   ├── plugin/          → Plugin lifecycle, SDK, manifest, versioning
│   ├── requirements/    → Functional requirements + business rules + traceability
│   ├── runtime/         → Execution model, scheduler, resource governance
│   ├── security/        → Security model, auth, capabilities, threats
│   └── standards/       → Extension + SDK development standards
├── src/                  → .NET runtime engine (not yet implemented)
├── AI-CONTEXT.md         → AI cognitive constraints and operating model
├── project-index.md      → Primary navigation entry point
└── readme.md             → Product overview
```

## Document Hierarchy

```
README → PROJECT-INDEX → docs/ → ai/ → .github/
```

- `readme.md` explains *what* the project is
- `project-index.md` explains *how the repo is organized* (read this first)
- `docs/` explains *how the system works*
- `ai/` controls *AI behavior rules*

## Truth Priority

When conflicts arise, this hierarchy defines what is authoritative:

1. `docs/architecture/` — highest authority
2. `docs/security/`
3. `docs/plugin/`
4. `docs/runtime/`
5. `docs/data/`
6. Implementation code — lowest authority

Code must follow documentation. If code conflicts with docs, documentation wins unless overridden by an ADR.

## Key Conventions

- Each topic has exactly ONE authoritative document (no duplication)
- Cross-reference related docs instead of copying content
- ADRs record all major architectural decisions
- `security-model.md` always takes precedence if any contradiction exists
- Documentation must evolve alongside implementation
