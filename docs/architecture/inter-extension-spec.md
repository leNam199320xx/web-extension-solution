# 🔗 Inter-Extension Communication Specification

---

# 1. PURPOSE

Defines how extensions can invoke other extensions through the platform, including visibility control (Public/Private/Subscription) and access governance.

For capability interfaces, see `docs/implementation/capability-interfaces.md`.
For ecosystem model, see `docs/architecture/extension-ecosystem.md`.

---

# 2. CORE PRINCIPLE

> Extensions NEVER communicate directly.
> All inter-extension calls go through the Core Runtime.
> The target extension controls who can invoke it.

```
Extension A → IExtensionCapability → Core Runtime → Validate Access → Execute Extension B → Return result to A
```

---

# 3. VISIBILITY MODEL

Each extension owner chooses a visibility level when publishing:

| Visibility | Behavior |
|-----------|----------|
| **Private** | Only the owner's other extensions can invoke it. No one else can see or call it. |
| **Public** | Any extension can invoke it (if they declare the permission in manifest). Visible to all in marketplace. |
| **Subscription** | Visible in marketplace, but requires an approved subscription request before invocation is allowed. |

### Visibility is set in the extension's manifest:

```json
{
  "extension_id": "payment-service",
  "visibility": "Subscription",
  "invocation_policy": {
    "rate_limit_per_caller": 100,
    "max_concurrent_callers": 10,
    "allowed_input_schema": "payment-input.json",
    "response_schema": "payment-output.json"
  }
}
```

---

# 4. ACCESS CONTROL FLOW

## 4.1 Public Extension

```
Extension A declares: "extension:invoke:payment-service"
    ↓
Payment-service visibility = Public
    ↓
Permission review: reviewer sees cross-extension dependency
    ↓
Approved → A can invoke payment-service at runtime
```

## 4.2 Subscription Extension

```
Extension A declares: "extension:invoke:analytics-engine"
    ↓
Analytics-engine visibility = Subscription
    ↓
System checks: does A have an approved subscription?
    ↓
If NO → A must request subscription first (separate flow)
    ↓
Analytics-engine owner reviews subscription request
    ↓
Approved → subscription recorded in database
    ↓
Now A's permission "extension:invoke:analytics-engine" is valid
    ↓
Permission review: reviewer sees subscription is active
    ↓
Approved → A can invoke analytics-engine at runtime
```

## 4.3 Private Extension

```
Extension A declares: "extension:invoke:internal-helper"
    ↓
Internal-helper visibility = Private
    ↓
System checks: is A owned by same author/team as internal-helper?
    ↓
If YES → allowed
If NO → rejected automatically (permission invalid)
```

---

# 5. SUBSCRIPTION MODEL

## 5.1 Subscription Request

```
POST /api/v1/extensions/{targetExtensionId}/subscribe
Authorization: Bearer <token>
```

```json
{
  "requestedBy": "order-extension",
  "reason": "Need to call payment-service to process order payments",
  "expectedUsage": {
    "callsPerDay": 1000,
    "peakConcurrency": 10
  }
}
```

## 5.2 Subscription States

```
Requested → Approved → Active → Revoked
                ↓
            Rejected
```

## 5.3 Subscription Decision (by target extension owner)

```
POST /api/v1/extensions/{targetExtensionId}/subscriptions/{subscriptionId}/decide
```

```json
{
  "decision": "Approved",
  "conditions": "Rate limit: 100 calls/min",
  "expiresAt": "2027-01-01T00:00:00Z"
}
```

## 5.4 Database Model

```sql
CREATE TABLE extension_subscriptions (
    subscription_id     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    source_extension_id VARCHAR(200) NOT NULL,
    target_extension_id VARCHAR(200) NOT NULL,
    status              VARCHAR(50)  NOT NULL DEFAULT 'Requested',
    reason              TEXT,
    expected_usage      JSONB,
    conditions          TEXT,
    decided_by          UUID,
    decided_at          TIMESTAMPTZ,
    expires_at          TIMESTAMPTZ,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_subscription UNIQUE (source_extension_id, target_extension_id)
);

-- status: Requested, Approved, Rejected, Revoked, Expired
```

---

# 6. IExtensionCapability INTERFACE

```csharp
public interface IExtensionCapability : ICapability
{
    /// <summary>
    /// Invoke another extension by ID.
    /// Requires "extension:invoke:{targetId}" permission in manifest.
    /// Target must be Public or have active Subscription.
    /// </summary>
    Task<ExtensionInvocationResult> InvokeAsync(
        string targetExtensionId,
        object? input = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a target extension is available and accessible.
    /// </summary>
    Task<bool> CanInvokeAsync(
        string targetExtensionId,
        CancellationToken cancellationToken = default);
}

public record ExtensionInvocationResult
{
    public bool Success { get; init; }
    public JsonElement? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string TargetExecutionId { get; init; } = "";
    public int DurationMs { get; init; }
}
```

---

# 7. RUNTIME ENFORCEMENT

When Extension A calls `IExtensionCapability.InvokeAsync("payment-service", ...)`:

```
1. Check A's manifest declares "extension:invoke:payment-service"
     → If not: throw CapabilityDeniedException

2. Check payment-service exists and is Active
     → If not: throw ExtensionNotFoundException

3. Check visibility:
     → Public: proceed
     → Private: check same owner → if not: deny
     → Subscription: check active subscription exists → if not: deny

4. Check rate limit (if target defines rate_limit_per_caller)
     → If exceeded: throw RateLimitExceededException

5. Check call depth (current depth < max_depth)
     → If exceeded: throw CircularInvocationException

6. Execute payment-service:
     → Create child execution context
     → Timeout = min(A's remaining timeout, B's timeout)
     → TraceId propagated from A
     → B runs with B's own permissions (not A's)
     → B cannot access A's data or capabilities

7. Return result to A
```

---

# 8. SAFEGUARDS

## 8.1 Circular Call Detection

```
Max call depth: 3 (configurable)

A → B → C    ✅ OK (depth 3)
A → B → C → D  ❌ Rejected (depth 4)
A → B → A    ❌ Rejected (cycle detected)
```

Implementation: maintain call stack in execution context, check before each invoke.

## 8.2 Cascading Timeout

```
A has 5000ms timeout
A calls B after 1000ms of own work
B gets timeout = min(B's manifest timeout, A's remaining 4000ms)
```

If B times out, A receives timeout error immediately.

## 8.3 Privilege Isolation

```
A has: db:read:orders, extension:invoke:payment-service
B has: db:read:payments, network:outbound:https://api.stripe.com/*

When A invokes B:
- B runs with B's permissions ONLY
- B cannot access A's "orders" table
- A cannot access B's "payments" table or Stripe
- Each extension stays in its own permission sandbox
```

## 8.4 Fault Isolation

- B crashes → A receives error result (not crash)
- B timeout → A receives timeout error
- B revoked between A's approval and runtime → A gets clear error at invocation time

---

# 9. PERMISSION DECLARATION

## Caller (Extension A) manifest:

```json
{
  "permissions": [
    "extension:invoke:payment-service",
    "extension:invoke:notification-service"
  ],
  "capabilities": [
    "ExtensionCapability"
  ]
}
```

## Target (Payment-service) manifest:

```json
{
  "visibility": "Subscription",
  "invocation_policy": {
    "rate_limit_per_caller": 100,
    "max_concurrent_callers": 10,
    "timeout_ms": 3000,
    "allowed_input_schema": {
      "type": "object",
      "properties": {
        "orderId": { "type": "string" },
        "amount": { "type": "number" }
      },
      "required": ["orderId", "amount"]
    }
  }
}
```

---

# 10. INPUT/OUTPUT CONTRACT

Target extensions can optionally define schema for what input they accept and what they return:

```json
"invocation_policy": {
  "allowed_input_schema": { ... },
  "response_schema": { ... }
}
```

- If `allowed_input_schema` is defined: runtime validates caller's input against schema before forwarding
- If input invalid: reject invocation immediately (no execution of target)
- Schema uses JSON Schema standard

---

# 11. MARKETPLACE INTEGRATION

Public and Subscription extensions appear in the Extension Marketplace:

```
┌─────────────────────────────────────────────────────────────┐
│  Extension Marketplace                                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 📦 payment-service              🔒 Subscription      │  │
│  │    Process credit card payments via Stripe            │  │
│  │    Author: platform-team                              │  │
│  │    Subscribers: 12  |  Invocations/day: 5,432         │  │
│  │    Input: { orderId, amount, currency }               │  │
│  │    Output: { transactionId, status }                  │  │
│  │                                   [Request Access]    │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 📦 notification-service          🌐 Public            │  │
│  │    Send notifications (email, push, SMS)              │  │
│  │    Author: platform-team                              │  │
│  │    Invocations/day: 12,001                            │  │
│  │    Input: { type, recipient, message }                │  │
│  │                              [Add to manifest]        │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

# 12. ADMIN PORTAL — SUBSCRIPTION MANAGEMENT

Extension owner sees incoming subscription requests:

```
┌─────────────────────────────────────────────────────────────┐
│  Subscription Requests for: payment-service                 │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ order-extension wants to invoke payment-service      │  │
│  │ Reason: Process order payments                        │  │
│  │ Expected: 1000 calls/day, peak 10 concurrent         │  │
│  │ Requested: 2 hours ago                                │  │
│  │                                                      │  │
│  │ [✅ Approve] [❌ Reject] [❓ Ask More]                │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

# 13. OBSERVABILITY

Inter-extension calls are fully traced:

```
Trace: abc-123
├── Extension A execution (span)
│   ├── A's own logic (span)
│   └── IExtensionCapability.InvokeAsync (span)
│       └── Extension B execution (child span)
│           ├── B's security validation (span)
│           └── B's plugin logic (span)
```

Metrics:
- `extension_invocation_total{source, target, status}`
- `extension_invocation_duration_ms{source, target}`
- `subscription_requests_total{target, decision}`

---

# 14. VERIFICATION ENGINE UPDATES

When verifying an extension that declares `extension:invoke:*`:

| Check | Action |
|-------|--------|
| Target exists | Verify target extension_id is registered |
| Target is active | Verify not revoked/archived |
| Visibility check | Public → OK; Subscription → check active subscription; Private → check same owner |
| Circular risk | Warn if target also invokes source (potential cycle) |
| Call depth risk | Warn if target also declares extension:invoke (nested calls) |

---

# 15. CONFIGURATION

```json
{
  "InterExtension": {
    "Enabled": true,
    "MaxCallDepth": 3,
    "DefaultRateLimitPerCaller": 100,
    "DefaultMaxConcurrentCallers": 10,
    "SubscriptionExpirationDays": 365,
    "CircularDetectionEnabled": true
  }
}
```

---

# 16. SECURITY SUMMARY

| Principle | Enforcement |
|-----------|-------------|
| No direct communication | Always via Core Runtime |
| Visibility control | Owner decides Public/Private/Subscription |
| Explicit permission | Caller must declare in manifest |
| Subscription approval | Target owner approves callers |
| Privilege isolation | Each extension uses own permissions |
| Cascading timeout | Child limited by parent remaining time |
| Circular prevention | Max depth + cycle detection |
| Rate limiting | Target defines per-caller limits |
| Full tracing | TraceId propagated, spans linked |
| Schema validation | Input validated before forwarding |

---

# 17. DESIGN PRINCIPLE

> Extensions can compose, but never couple.
> The platform mediates all communication.
> The target extension always controls access.
> Trust is never transitive: A trusts B does not mean A trusts B's callees.

---

# 🏁 END
