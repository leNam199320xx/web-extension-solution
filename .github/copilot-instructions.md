# 🧠 Copilot Brain File (AI Execution Rules)
# Project: Metadata-Driven Secure Plugin Runtime (.NET 10)

---

# 1. 🎯 SYSTEM CONTEXT (CỰC QUAN TRỌNG)

Bạn đang làm việc trong một hệ thống:

> Secure Plugin Runtime System (.NET 10)

Đặc điểm:

- Plugin execution engine (không phải CRUD app)
- Zero-trust security model
- Dynamic plugin loading (DLL runtime)
- Capability-based access control
- Signed manifest required for execution

👉 Core system KHÔNG chứa business logic.

---

# 2. 🚨 ABSOLUTE RULES (KHÔNG ĐƯỢC VI PHẠM)

## ❌ CẤM TUYỆT ĐỐI:

- Không được cho plugin truy cập DB trực tiếp
- Không bypass Manifest validation
- Không bỏ qua SHA-256 verification
- Không bỏ qua signature verification
- Không thêm architecture phức tạp không cần thiết
- Không tự thêm DDD / CQRS / Event Sourcing nếu không yêu cầu

---

## ❌ SECURITY MODEL MUST BE PRESERVED:

- Zero-trust: plugin = untrusted
- Core = ONLY trusted component
- All access must go through Capability layer
- Fail-closed on any validation error

---

# 3. 🧱 ARCHITECTURE RULES

## Core Components:

- PluginExecutor (runtime execution engine)
- ManifestValidator (security gate)
- CapabilityResolver (permission enforcement)
- AssemblyLoader (isolated runtime loader)

---

## RULE:

👉 Core system MUST remain:

- Stateless
- Minimal
- Deterministic
- Secure-first

---

# 4. 🔑 CAPABILITY MODEL (QUAN TRỌNG NHẤT)

Plugins CANNOT access:

- Database directly
- File system directly
- Network directly
- OS resources directly

## ALL ACCESS MUST GO THROUGH:

- IDatabaseCapability
- INetworkCapability
- IStorageCapability
- ICacheCapability

👉 If capability not granted in manifest → ACCESS DENIED

---

# 5. 📄 MANIFEST ENFORCEMENT RULE

Every plugin execution MUST:

1. Validate schema
2. Verify SHA-256 hash
3. Verify digital signature
4. Check version compatibility
5. Check revocation list
6. Resolve capabilities
7. Enforce resource limits

❌ ANY FAILURE = STOP EXECUTION IMMEDIATELY

---

# 6. ⚙️ CODING STYLE RULES

## Keep code:

- Simple
- Flat
- Readable
- Minimal abstraction

## Prefer:

- Direct implementation
- Explicit logic
- Small services

## Avoid:

- Over-engineering
- Deep inheritance trees
- Excessive design patterns

---

# 7. 🧠 HOW TO DECIDE ARCHITECTURE

When uncertain:

👉 Choose simplest secure solution
NOT most abstract enterprise pattern

---

# 8. 🔥 OUTPUT RULES (VERY IMPORTANT)

When generating code:

1. Show file structure first
2. Then full code
3. Keep explanation minimal
4. Focus on correctness over theory

---

# 9. ⏱ RUNTIME RULES

- All plugin execution MUST support timeout
- Must support cancellation token
- Must not block main thread
- Must log execution trace

---

# 10. 🧯 FAILURE HANDLING

Any error in plugin execution:

- MUST NOT crash core system
- MUST isolate failure
- MUST log with TraceId + PluginId
- MUST return safe error response

---

# 11. 📊 OBSERVABILITY RULES

Every execution MUST include:

- TraceId
- PluginId
- ExecutionTime
- MemoryUsage (if available)
- Status (Success/Failed/Timeout)

---

# 12. 🚀 HOT RELOAD RULES

If plugin is updated:

- Stop new requests
- Wait for active execution to complete
- Unload previous version safely
- Load new assembly
- Warm-up before serving traffic

---

# 13. 🧪 THINKING MODE (HOW YOU SHOULD REASON)

Before writing code:

- Identify security impact
- Identify capability usage
- Ensure no bypass of validation
- Ensure minimal design

---

# 14. 🎯 FINAL PRINCIPLE

> Security > Simplicity > Performance > Convenience

NEVER sacrifice security for convenience.

---

# 🏁 END OF COPILOT BRAIN FILE