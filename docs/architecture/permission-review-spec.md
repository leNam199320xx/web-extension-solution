# 🔐 Permission Review & Approval Specification

---

# 1. PURPOSE

Defines how extension permission requests are declared, displayed to reviewers, and approved/rejected. When an extension is uploaded, the system extracts all permission declarations and presents them in a structured review interface.

For extension permission format, see `docs/standards/extension-development-standard.md` (§7 Resource Scope).
For verification engine, see `docs/architecture/verification-engine-spec.md`.
For plugin lifecycle, see `docs/plugin/plugin-lifecycle.md`.

---

# 2. PERMISSION DECLARATION MODEL

## 2.1 Where Permissions Are Declared

Extensions declare permissions in **two places**:

### manifest.json (required)

```json
{
  "permissions": [
    "db:read:orders",
    "db:write:orders",
    "network:outbound:https://api.stripe.com/*",
    "cache:read:payment-*",
    "cache:write:payment-*"
  ],
  "capabilities": [
    "DatabaseCapability",
    "NetworkCapability",
    "CacheCapability"
  ]
}
```

### permissions.json (optional — detailed justification)

```json
{
  "permissions": [
    {
      "scope": "db:read:orders",
      "justification": "Read order data to calculate payment amounts",
      "frequency": "per-execution",
      "data_sensitivity": "medium"
    },
    {
      "scope": "db:write:orders",
      "justification": "Update order payment_status after processing",
      "frequency": "per-execution",
      "data_sensitivity": "medium"
    },
    {
      "scope": "network:outbound:https://api.stripe.com/*",
      "justification": "Process credit card payments via Stripe API",
      "frequency": "per-execution",
      "data_sensitivity": "high",
      "data_sent": ["order_id", "amount", "currency"],
      "data_received": ["transaction_id", "status"]
    },
    {
      "scope": "cache:read:payment-*",
      "justification": "Cache payment method tokens to reduce API calls",
      "frequency": "high",
      "data_sensitivity": "low"
    },
    {
      "scope": "cache:write:payment-*",
      "justification": "Store payment method tokens temporarily (TTL 5min)",
      "frequency": "low",
      "data_sensitivity": "low"
    }
  ]
}
```

## 2.2 Permission Fields

| Field | Required | Description |
|-------|----------|-------------|
| `scope` | Yes | Permission string matching manifest |
| `justification` | Yes | Human-readable explanation of WHY |
| `frequency` | No | How often: `once`, `per-execution`, `high`, `background` |
| `data_sensitivity` | No | Classification: `low`, `medium`, `high`, `critical` |
| `data_sent` | No | What data leaves the system (for network) |
| `data_received` | No | What data enters from external (for network) |
| `ttl` | No | For cache: how long data is stored |
| `max_records` | No | For db: estimated max records accessed per execution |

---

# 3. PERMISSION EXTRACTION PIPELINE

```
Upload Package
    ↓
Extract manifest.json → parse permissions[]
    ↓
Extract permissions.json (if exists) → enrich with justifications
    ↓
Cross-validate: permissions.json scopes match manifest permissions
    ↓
Classify risk level per permission
    ↓
Generate Permission Review Summary
    ↓
Store in database (linked to PluginVersion)
    ↓
Present to reviewer
```

---

# 4. RISK CLASSIFICATION (Automated)

Each permission is auto-classified:

| Risk Level | Criteria | Review Required |
|-----------|----------|-----------------|
| 🟢 Low | Cache read/write in own namespace | Auto-approvable |
| 🟡 Medium | DB read specific tables, storage in own namespace | Standard review |
| 🟠 High | DB write, network outbound to known APIs | Security review |
| 🔴 Critical | Wildcard permissions (`*`), network to unknown domains | Senior security review |

Classification rules:

```
db:read:{specific_table}     → Medium
db:write:{specific_table}    → High
db:read:*                    → Critical
db:write:*                   → Critical
network:outbound:{known_api} → High
network:outbound:{unknown}   → Critical
network:outbound:*           → Critical
storage:read:/plugins/self/* → Low
storage:write:/plugins/self/* → Medium
storage:read:/shared/*       → High
cache:*                      → Low
```

---

# 5. REVIEWER INTERFACE (API Response)

When a reviewer queries pending approvals, they see:

```
GET /api/v1/approvals/{pluginVersionId}/permissions
```

Response:

```json
{
  "pluginId": "payment-service",
  "version": "1.0.0",
  "author": "developer@company.com",
  "uploadedAt": "2026-01-15T10:00:00Z",
  "overallRiskLevel": "High",
  "permissionSummary": {
    "totalPermissions": 5,
    "byRisk": {
      "low": 2,
      "medium": 1,
      "high": 2,
      "critical": 0
    },
    "capabilities": ["DatabaseCapability", "NetworkCapability", "CacheCapability"]
  },
  "permissions": [
    {
      "scope": "db:read:orders",
      "capability": "DatabaseCapability",
      "action": "read",
      "resource": "orders",
      "riskLevel": "Medium",
      "justification": "Read order data to calculate payment amounts",
      "frequency": "per-execution",
      "dataSensitivity": "medium",
      "flags": []
    },
    {
      "scope": "db:write:orders",
      "capability": "DatabaseCapability",
      "action": "write",
      "resource": "orders",
      "riskLevel": "High",
      "justification": "Update order payment_status after processing",
      "frequency": "per-execution",
      "dataSensitivity": "medium",
      "flags": ["write-access"]
    },
    {
      "scope": "network:outbound:https://api.stripe.com/*",
      "capability": "NetworkCapability",
      "action": "outbound",
      "resource": "https://api.stripe.com/*",
      "riskLevel": "High",
      "justification": "Process credit card payments via Stripe API",
      "frequency": "per-execution",
      "dataSensitivity": "high",
      "dataSent": ["order_id", "amount", "currency"],
      "dataReceived": ["transaction_id", "status"],
      "flags": ["external-api", "financial-data"]
    },
    {
      "scope": "cache:read:payment-*",
      "capability": "CacheCapability",
      "action": "read",
      "resource": "payment-*",
      "riskLevel": "Low",
      "justification": "Cache payment method tokens to reduce API calls",
      "flags": []
    },
    {
      "scope": "cache:write:payment-*",
      "capability": "CacheCapability",
      "action": "write",
      "resource": "payment-*",
      "riskLevel": "Low",
      "justification": "Store payment method tokens temporarily (TTL 5min)",
      "flags": []
    }
  ],
  "verificationResult": {
    "status": "Passed",
    "completedAt": "2026-01-15T10:01:30Z"
  },
  "previousVersionPermissions": null,
  "permissionDiff": null
}
```

---

# 6. PERMISSION DIFF (Version Upgrades)

When a new version is uploaded for an existing plugin, the system shows what changed:

```json
{
  "permissionDiff": {
    "added": [
      {
        "scope": "network:outbound:https://api.shipping.com/*",
        "riskLevel": "High",
        "justification": "New shipping integration"
      }
    ],
    "removed": [],
    "unchanged": [
      "db:read:orders",
      "db:write:orders",
      "cache:read:payment-*",
      "cache:write:payment-*"
    ],
    "modified": [
      {
        "scope": "network:outbound:https://api.stripe.com/*",
        "change": "URL pattern expanded from /v1/charges to /*"
      }
    ]
  }
}
```

This helps reviewers focus on **what's new** instead of re-reviewing everything.

---

# 7. APPROVAL ACTIONS

## 7.1 Approve All

```
POST /api/v1/approvals/{pluginVersionId}/approve
```

```json
{
  "decision": "Approved",
  "comment": "Permissions are justified and appropriate",
  "conditions": null
}
```

## 7.2 Approve With Conditions

```json
{
  "decision": "ApprovedWithConditions",
  "comment": "Approved but network scope must be narrowed in next version",
  "conditions": [
    "Narrow network:outbound scope to specific Stripe endpoints in v1.1.0"
  ],
  "expiresAt": "2026-06-01T00:00:00Z"
}
```

## 7.3 Reject

```json
{
  "decision": "Rejected",
  "comment": "db:write:* is too broad. Specify exact tables needed.",
  "rejectedPermissions": ["db:write:*"],
  "suggestedFix": "Replace db:write:* with db:write:orders and db:write:payments"
}
```

## 7.4 Request More Information

```json
{
  "decision": "NeedsInfo",
  "comment": "Why does a payment plugin need to read the users table?",
  "questionsFor": ["db:read:users"]
}
```

---

# 8. PERMISSION FLAGS (Auto-Generated)

The system auto-flags permissions for reviewer attention:

| Flag | Trigger | Description |
|------|---------|-------------|
| `write-access` | Any `write` action | Data modification risk |
| `external-api` | Network outbound | Data leaves system |
| `financial-data` | Keywords in justification | Payment/money related |
| `pii-access` | Tables: users, profiles, etc. | Personal data access |
| `wildcard` | `*` in resource scope | Overly broad access |
| `new-permission` | Not in previous version | First time requesting this |
| `high-frequency` | frequency = `high` | Performance impact |
| `cross-namespace` | Storage outside own namespace | Cross-boundary access |

---

# 9. AUTO-APPROVAL RULES

Some permissions can be auto-approved without human review:

| Condition | Auto-Approve |
|-----------|-------------|
| All permissions are Low risk | ✅ Yes |
| Plugin is from trusted publisher (verified) | ✅ If all ≤ Medium |
| Same permissions as previous approved version | ✅ Yes |
| Any Critical permission | ❌ Never |
| First version of new plugin | ❌ Never |

Auto-approval still generates audit log.

---

# 10. DATABASE MODEL

```sql
CREATE TABLE permission_reviews (
    review_id       UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    version_id      UUID NOT NULL REFERENCES plugin_versions(version_id),
    permissions     JSONB NOT NULL,           -- full permission list with metadata
    risk_summary    JSONB NOT NULL,           -- risk classification summary
    permission_diff JSONB,                    -- diff from previous version
    reviewer_id     UUID,
    decision        VARCHAR(50),             -- Approved, Rejected, NeedsInfo, etc.
    comment         TEXT,
    conditions      JSONB,
    decided_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

# 11. INTEGRATION WITH VERIFICATION ENGINE

```
Upload
  ↓
Verification Engine (stages 1-7)
  ↓
Permission Extraction & Classification
  ↓
Generate Review Summary
  ↓
Store in permission_reviews table
  ↓
┌──────────────────────┐
│ Auto-approve check   │
│ (all Low risk?)      │
└──────┬───────────────┘
       │
  ┌────┴────┐
  ▼         ▼
Auto     Human Review
Approved   Queue
  │         │
  ▼         ▼
Signing    Reviewer sees
(KMS)      permission summary
            │
            ▼
         Decision
            │
      ┌─────┴─────┐
      ▼            ▼
  Approved     Rejected
      │            │
      ▼            ▼
  Signing      Report sent
  (KMS)        to developer
```

---

# 12. OBSERVABILITY

Every permission review generates:
- Audit log entry (who reviewed, what decision, when)
- Metrics: avg review time, approval rate, common rejection reasons
- Alerting: if Critical permissions queue grows

---

# 13. DESIGN PRINCIPLE

> Permissions must be **visible**, **justified**, and **auditable**.
> A reviewer should understand in 30 seconds what an extension wants and why.
> The system should highlight risk, not hide it.

---

# 🏁 END
