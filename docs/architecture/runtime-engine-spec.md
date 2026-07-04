# ⚙️ Runtime Engine Specification (.NET 10)

---

# 1. 🎯 PURPOSE

Runtime Engine là lõi thực thi plugin, chịu trách nhiệm:

- Nhận request từ API Gateway
- Load plugin từ repository
- Validate manifest + signature
- Inject capabilities
- Execute plugin trong isolated context
- Collect observability data

---

# 2. 🧠 CORE COMPONENTS

## 2.1 PluginExecutor (Core Orchestrator)

Responsible for:

- Execution lifecycle orchestration
- Pipeline control
- Error handling (fail-closed)

---

## 2.2 PluginLoader

- Load assembly (.dll)
- Create isolated context (AssemblyLoadContext)
- Prevent assembly leakage

---

## 2.3 ExecutionPipeline

Stages:

```
Validate Manifest
→ Verify Signature
→ Verify SHA256
→ Resolve Capabilities
→ Load Plugin
→ Execute
→ Collect Metrics
```

---

## 3. 🔁 EXECUTION FLOW

```
HTTP Request
   ↓
Runtime API Layer
   ↓
PluginExecutor
   ↓
ManifestValidator
   ↓
CapabilityResolver
   ↓
PluginLoader
   ↓
Execute()
   ↓
Response + Observability
```

---

# 4. 🧱 ISOLATION MODEL

## Required:

- AssemblyLoadContext per plugin
- No shared mutable state
- No static plugin caching

---

# 5. ⏱ EXECUTION RULES

- Must enforce timeout (CancellationToken)
- Must enforce memory cap (soft limit monitoring)
- Must enforce CPU guard (cooperative cancellation)

---

# 6. 🚨 FAILURE MODEL

Any failure:

- Stop execution immediately
- Do NOT fallback silently
- Log with TraceId

---

# 7. 🧠 DESIGN RULE

> Core never trusts plugin code under any condition

---

# 🏁 END SPEC