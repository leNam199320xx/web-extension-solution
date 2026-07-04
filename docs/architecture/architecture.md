# 🧠 System Architecture - Metadata-Driven Secure Plugin Runtime (.NET 10)

---

# 1. 🎯 OVERVIEW

Hệ thống này là một **Plugin Runtime Engine an toàn**, cho phép:

- Load plugin động (DLL)
- Không restart Core API
- Kiểm soát bằng Signed Manifest
- Áp dụng Zero-Trust Security Model
- Capability-based access control

👉 Core system = Execution Engine + Security Gateway

---

# 2. 🧱 HIGH-LEVEL ARCHITECTURE

```
                ┌──────────────────────┐
                │   API Gateway (.NET) │
                └──────────┬───────────┘
                           │
                           ▼
                ┌──────────────────────┐
                │ Plugin Controller API│
                └──────────┬───────────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
┌──────────────┐  ┌────────────────┐  ┌────────────────┐
│ Manifest     │  │ Security Engine│  │ Plugin Loader  │
│ Validator    │  │ (Zero Trust)   │  │ (ALC Runtime)  │
└──────┬───────┘  └────────┬───────┘  └────────┬───────┘
       │                   │                    │
       ▼                   ▼                    ▼
┌──────────────────────────────────────────────────────┐
│            Capability Enforcement Layer              │
└──────────────────────────────────────────────────────┘
                           │
                           ▼
                ┌──────────────────────┐
                │ Plugin Execution     │
                │ Sandbox (Isolated)   │
                └──────────────────────┘
                           │
                           ▼
                ┌──────────────────────┐
                │ Observability Layer  │
                │ Logs + Metrics + Traces │
                └──────────────────────┘
```

---

# 3. 🧠 CORE DESIGN PRINCIPLES

## 3.1 Zero Trust

- Plugin = NOT trusted
- Core = ONLY trusted component
- Every request must be validated

---

## 3.2 Fail Closed

- Any validation failure → STOP execution immediately
- No fallback unsafe mode

---

## 3.3 Capability-Based Security

Plugin KHÔNG truy cập trực tiếp:

- Database
- Network
- File system
- OS resources

👉 Mọi access phải qua Capability Layer

---

## 3.4 Stateless Core

- Core API không giữ state plugin execution
- State nếu cần → external store (DB/Cache)

---

# 4. 🔄 PLUGIN EXECUTION FLOW

```
1. API Request Received
        ↓
2. Manifest Validation
        ↓
3. Signature Verification (RSA/ECDSA)
        ↓
4. SHA-256 Hash Check
        ↓
5. Capability Resolution
        ↓
6. Load Plugin (AssemblyLoadContext)
        ↓
7. Execute Plugin
        ↓
8. Enforce Timeout / Resource Limits
        ↓
9. Collect Observability Data
        ↓
10. Return Response
```

---

# 5. 📄 MANIFEST ROLE

Manifest là “hợp đồng an toàn” của plugin.

Nó định nghĩa:

- Plugin ID
- Version compatibility
- Permissions
- Resource limits
- Signature

👉 Không có manifest hợp lệ → plugin KHÔNG được chạy

---

# 6. 🔐 SECURITY ARCHITECTURE

## 6.1 Validation Pipeline

```
Manifest Schema Check
        ↓
SHA-256 Verification
        ↓
Digital Signature Verification
        ↓
Revocation Check
        ↓
Version Compatibility Check
        ↓
Capability Mapping
        ↓
Execution Approval
```

---

## 6.2 Threat Model

Hệ thống chống:

- Tampering plugin DLL
- Inject code trái phép
- Privilege escalation
- Network exfiltration
- Memory dump attacks (giảm thiểu)

---

## 6.3 Fail Strategy

- Fail = reject execution
- Không fallback unsafe mode
- Log đầy đủ trace

---

# 7. 🔑 CAPABILITY SYSTEM

## Nguyên tắc:

Plugin KHÔNG gọi trực tiếp infrastructure.

## Thay vào đó:

```
Plugin → Capability Interface → Core Proxy → Infra
```

---

## Example Capabilities:

- IDatabaseCapability
- INetworkCapability
- IStorageCapability
- ICacheCapability

---

## Rule:

👉 Capability phải được cấp trong Manifest

---

# 8. 🧩 PLUGIN LOADER DESIGN

## Technology:

- AssemblyLoadContext (.NET 10)

## Isolation rules:

- Plugin load vào memory riêng
- Không share static state
- Unload được khi cần

---

## Important Note:

AssemblyLoadContext ≠ security boundary

👉 chỉ là isolation, không phải sandbox security

---

# 9. ⏱ RUNTIME CONTROL

## Enforced limits:

- Execution timeout
- Memory usage limit
- CPU usage limit (soft control)
- CancellationToken enforcement

---

## Behavior:

- Timeout → terminate execution
- Overflow → log + reject
- Crash → isolate plugin only

---

# 10. 🔁 HOT RELOAD STRATEGY

```
Stop new requests
↓
Wait active executions
↓
Unload old plugin ALC
↓
Load new version
↓
Warm-up
↓
Resume traffic
```

---

# 11. 📊 OBSERVABILITY DESIGN

Mỗi execution phải emit:

- TraceId
- PluginId
- ExecutionTime
- Status (Success / Failed / Timeout)
- Resource usage (if available)

---

## Logging:

- Structured JSON
- Audit-safe
- Queryable (ELK / OpenTelemetry)

---

# 12. ⚙️ SCALABILITY MODEL

## Core is:

- Stateless
- Horizontally scalable
- Stateless execution engine

## Scaling strategy:

- Multiple Core instances
- Shared plugin repository
- Central revocation service

---

# 13. 🚨 CRITICAL DESIGN RULES

## MUST:

- Validate before execution
- Enforce capability rules
- Fail closed
- Log everything

---

## MUST NOT:

- Trust plugin input
- Allow direct DB access
- Skip signature validation
- Bypass manifest rules

---

# 14. 🧠 AI READABILITY OPTIMIZATION

File này được thiết kế để:

- Copilot hiểu architecture scope
- Kiro hiểu execution flow
- Agent AI hiểu security constraints
- Reduce hallucination in code generation

---

# 15. 🎯 FINAL SYSTEM GOAL

👉 Build a runtime system that:

- Executes untrusted plugins safely
- Enforces strict metadata governance
- Prevents privilege escalation
- Supports hot deployment
- Scales horizontally

---

# 🏁 END OF ARCHITECTURE