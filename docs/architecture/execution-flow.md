# ⚙️ Execution Flow Architecture
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

Describes the end-to-end execution flow of a plugin request. This is the single authoritative document for the pipeline sequence.

Other documents reference this flow:
- `docs/architecture/runtime-engine-spec.md` (components)
- `docs/security/security-enforcement-spec.md` (validation stages)
- `docs/runtime/resource-governance.md` (limits enforcement)

---

# 2. HIGH-LEVEL FLOW

```
Client → API Gateway → Scheduler → Security Pipeline → Plugin Loader → Execution → Response
```

Every request MUST pass through all stages. Failure at any stage = immediate termination.

---

# 3. DETAILED PIPELINE

## Step 1 — API Request

```
POST /api/v1/execute/{pluginId}
```

API layer:
- Validates request schema (JSON format, required fields)
- Authenticates caller (JWT Bearer)
- Generates TraceId (if not provided)
- Creates ExecutionRequest DTO

---

## Step 2 — Scheduling

Scheduler:
- Checks concurrency limits
- Enqueues with priority
- Returns 429 if overloaded

See `docs/runtime/scheduler.md` for queue implementation.

---

## Step 3 — Manifest Resolution

Runtime loads:
- PluginVersion record from database
- Signed Manifest
- Plugin binary location

If plugin not found or not in `Approved` status → reject with `API-004`.

---

## Step 4 — Security Validation Pipeline

Full validation sequence (see `docs/security/security-enforcement-spec.md`):

```
Schema Validation → Expiration Check → SHA-256 → Signature → Revocation → Version Compat → Capability Mapping
```

Any failure → execution rejected. No partial validation.

---

## Step 5 — Capability Resolution

- Extract permissions from manifest
- Map permissions to capability implementations
- Create scoped capability instances for this execution
- Inject into PluginExecutionContext

See `docs/implementation/capability-interfaces.md`.

---

## Step 6 — Plugin Loading

- Load assembly via isolated AssemblyLoadContext
- Resolve entry class implementing `IPlugin`
- Validate entry point exists

See `docs/plugin/plugin-loading.md`.

---

## Step 7 — Execution

- Create `IPluginExecutionContext` with capabilities, logger, cancellation token
- Call `plugin.Execute(context)` under resource governance
- Timeout enforcement via CancellationToken

See `docs/runtime/resource-governance.md`.

---

## Step 8 — Result Capture

Collect:
- Plugin return value (PluginResult)
- Execution duration
- Memory usage (approximate)
- Any exceptions

---

## Step 9 — Observability Emit

Emit regardless of success/failure:
- TraceId, ExecutionId, PluginId
- Duration, Status
- Error code (if failed)
- Resource usage metrics

See `docs/infrastructure/observability.md`.

---

## Step 10 — Response

Map ExecutionResult to HTTP response:
- Success → 200 with data
- Plugin error → 500 with structured error
- Security error → 403
- Timeout → 504
- Validation error → 400

---

# 4. FAILURE HANDLING

Any failure triggers:
1. Execution stop (immediate)
2. Resource cleanup (CancellationToken fired, context disposed)
3. Audit event generated
4. Structured error returned

No partial trust. No partial execution results.

---

# 5. SECURITY CHECKPOINTS

| Stage | Enforcement |
|-------|-------------|
| API Entry | Authentication (JWT) |
| Manifest | Schema + Signature + Hash |
| Capability | Permission mapping |
| Execution | Resource limits |
| Response | No internal details leaked |

---

# 6. SEQUENCE DIAGRAM

```
Client          API         Scheduler      SecurityPipeline    Loader      Plugin      Observability
  │               │              │                │               │           │              │
  │──POST /execute──▶│           │                │               │           │              │
  │               │──Enqueue────▶│                │               │           │              │
  │               │              │──Validate─────▶│               │           │              │
  │               │              │                │──OK──────────▶│           │              │
  │               │              │                │               │──Load────▶│              │
  │               │              │                │               │           │──Execute───▶ │
  │               │              │                │               │           │◀──Result──── │
  │               │              │                │               │           │              │──Emit
  │◀──Response────│              │                │               │           │              │
```

---

# 7. DESIGN PRINCIPLE

> Execution is a pipeline of enforced trust boundaries.
> No step can be skipped. No shortcut exists.

---

# 🏁 END
