# 🛡 Authorization Model
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

This document defines how access control decisions are made.

It covers:

- Who can do what
- Capability enforcement
- RBAC + ABAC hybrid model
- Runtime authorization checks

---

# 2. CORE PRINCIPLE

> Authorization answers: "What are you allowed to do?"

In this system:

> Authorization is **strictly capability-based**, not identity-based alone.

---

# 3. AUTHORIZATION MODEL TYPE

The system uses:

```
RBAC + ABAC + Capability-Based Access Control (CBAC)
```

But the dominant model is:

> ✔ Capability-Based Access Control (CBAC)

---

# 4. AUTHORIZATION LAYERS

## Layer 1 — Identity Check (RBAC)

User roles:

- Admin
- Developer
- SecurityReviewer
- Auditor

Used for:

- API access
- management operations

---

## Layer 2 — Policy Check (ABAC)

Attributes:

- TenantId
- Environment
- Plugin ownership
- Execution context

Used for:

- Approval rules
- deployment rules

---

## Layer 3 — Capability Enforcement (CORE)

This is the most important layer.

Plugins NEVER access resources directly.

They MUST use capabilities:

- DatabaseCapability
- StorageCapability
- NetworkCapability
- QueueCapability

---

# 5. CAPABILITY AUTHORIZATION FLOW

```
Plugin → Runtime → Capability Engine → Decision → Resource Access
```

Steps:

1. Plugin requests capability usage
2. Runtime checks manifest permissions
3. Capability Engine validates policy
4. Access is granted or denied

---

# 6. DEFAULT DENY POLICY

All access is denied by default.

Only explicitly granted capabilities are allowed.

---

# 7. CAPABILITY RULES

Each capability defines:

- Allowed operations
- Resource scope
- Limits
- Version constraints

Example:

```
DatabaseCapability:
  - Read: allowed
  - Write: optional
  - Transaction: controlled
```

---

# 8. RUNTIME ENFORCEMENT

Authorization is enforced at:

- API Gateway
- Runtime Engine
- Capability Layer

NOT inside plugin code.

---

# 9. PLUGIN AUTHORIZATION MODEL

Plugins:

- Have no identity
- Have no direct permissions
- Inherit permissions ONLY from manifest

Execution is constrained by:

```
Signed Manifest → Capability Engine → Execution Context
```

---

# 10. TENANT ISOLATION

If multi-tenant enabled:

- Each tenant has isolated capabilities
- Cross-tenant access is forbidden
- Enforcement is runtime-level

---

# 11. POLICY EXAMPLES

## Allow DB Read

```
role = Developer
capability = DatabaseCapability:Read
tenant = match
```

---

## Deny Network Access

```
capability = NetworkCapability
action = outbound
→ DENY
```

---

# 12. FAILURE MODE

Any authorization failure:

- MUST be denied
- MUST be logged
- MUST trigger audit event

No fallback allowed.

---

# 13. SECURITY PRINCIPLE

> Authentication identifies.
> Authorization constrains.
> Capability enforces.

---

# 14. NON-GOALS

This model does NOT:

- Trust plugin identity
- Allow dynamic privilege escalation
- Allow implicit permissions
- Allow runtime self-authorization

---

# 🏁 END OF AUTHORIZATION MODEL