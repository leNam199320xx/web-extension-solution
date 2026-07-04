# 🔐 Authentication Model
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

This document defines how identities are authenticated across the system.

Authentication applies to:

- Users (Humans)
- Services (System-to-System)
- Runtime Nodes

Plugins are NOT authenticated (they are untrusted artifacts).

---

# 2. CORE PRINCIPLE

> Authentication answers: "Who are you?"

But in this system:

> Authentication NEVER grants permissions directly.

Authentication only establishes identity.

Authorization is handled separately.

---

# 3. IDENTITY TYPES

## 3.1 Human Users

- Developers
- Admins
- Security reviewers
- Auditors

Supported methods:

- OAuth2 / OpenID Connect
- SSO (Azure AD / Google Workspace / Keycloak)
- MFA required for privileged roles

---

## 3.2 Service Identity

Used for:

- Approval service
- Runtime nodes
- CI/CD pipelines

Methods:

- Client Certificates (mTLS)
- API Keys (short-lived preferred)
- JWT (service-issued)

---

## 3.3 Runtime Identity

Each Runtime Node MUST have:

- NodeId
- Certificate identity
- Signed heartbeat

Used for:

- Cluster trust
- Observability attribution

---

# 4. AUTHENTICATION FLOW

```
Client → Identity Provider → Token Issued → Runtime/API Validation
```

Steps:

1. User/service authenticates with Identity Provider
2. Token is issued (JWT or equivalent)
3. Runtime validates token signature
4. Claims are extracted

---

# 5. TOKEN MODEL

## JWT Claims (example)

```
sub: userId
role: Developer | Admin | Auditor
tenant: tenantId
scope: optional scopes
exp: expiration
iss: trusted issuer
aud: runtime-api
```

---

# 6. VALIDATION RULES

Runtime MUST validate:

- Signature validity
- Expiration time
- Issuer trust
- Audience match

If invalid:

→ Request rejected immediately (fail closed)

---

# 7. MFA REQUIREMENTS

Mandatory for:

- Plugin approval
- Plugin signing
- Capability modification
- Administrative actions

---

# 8. SESSION MODEL

The system is stateless:

- No server sessions
- No sticky identity state
- Tokens are self-contained

---

# 9. SECURITY PRINCIPLE

> Authentication is a gate, not a privilege.

---

# 10. NON-GOALS

Authentication does NOT:

- Grant plugin execution rights
- Define capabilities
- Control runtime behavior

---

# 🏁 END OF AUTHENTICATION MODEL