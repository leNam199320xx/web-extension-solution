# 🌐 Runtime API Specification (.NET 10)

---

# 1. PURPOSE

Defines the complete HTTP API surface for the plugin runtime platform.

---

# 2. BASE URL

```
/api/v1
```

API versioning: URI-based (`/api/v1/`, `/api/v2/`).

---

# 3. PLUGIN EXECUTION

## Execute Plugin

```
POST /api/v1/execute/{pluginId}
Authorization: Bearer <token>
Content-Type: application/json
```

Request:

```json
{
  "input": { "key": "value" },
  "version": "1.0.0",
  "metadata": {
    "correlationId": "corr-456"
  }
}
```

Success response (200):

```json
{
  "success": true,
  "data": { "result": "value" },
  "executionId": "exec-789",
  "traceId": "trace-abc-123",
  "durationMs": 42
}
```

Error response (4xx/5xx):

```json
{
  "error": {
    "code": "SEC-001",
    "category": "Security",
    "message": "Signature verification failed",
    "traceId": "trace-abc-123",
    "timestamp": "2026-01-01T00:00:00Z"
  }
}
```

---

# 4. PLUGIN MANAGEMENT

## List Plugins

```
GET /api/v1/plugins
Authorization: Bearer <token>
```

Response (200):

```json
{
  "plugins": [
    {
      "pluginId": "payment-service",
      "name": "Payment Service",
      "latestVersion": "1.2.0",
      "status": "Approved"
    }
  ],
  "total": 1
}
```

## Get Plugin Details

```
GET /api/v1/plugins/{pluginId}
Authorization: Bearer <token>
```

## Upload Plugin

```
POST /api/v1/plugins/upload
Authorization: Bearer <token>
Content-Type: multipart/form-data
Body: file=<plugin.zip>
```

Response (202 Accepted):

```json
{
  "pluginVersionId": "guid",
  "status": "Scanning",
  "message": "Plugin accepted for validation"
}
```

## Reload Plugin

```
POST /api/v1/plugins/{pluginId}/reload
Authorization: Bearer <token>
```

## Revoke Plugin

```
POST /api/v1/plugins/{pluginId}/revoke
Authorization: Bearer <token>
Content-Type: application/json
```

Request:

```json
{
  "reason": "Security vulnerability detected",
  "version": "1.0.0"
}
```

---

# 5. APPROVAL WORKFLOW

## List Pending Approvals

```
GET /api/v1/approvals?status=Pending
Authorization: Bearer <token>
```

## Approve Plugin Version

```
POST /api/v1/approvals/{pluginVersionId}/approve
Authorization: Bearer <token>
```

Request:

```json
{
  "comment": "Reviewed and approved"
}
```

## Reject Plugin Version

```
POST /api/v1/approvals/{pluginVersionId}/reject
Authorization: Bearer <token>
```

## Get Permission Review Summary

```
GET /api/v1/approvals/{pluginVersionId}/permissions
Authorization: Bearer <token>
```

Response (200):

```json
{
  "pluginId": "payment-service",
  "version": "1.0.0",
  "overallRiskLevel": "High",
  "permissionSummary": {
    "totalPermissions": 5,
    "byRisk": { "low": 2, "medium": 1, "high": 2, "critical": 0 }
  },
  "permissions": [
    {
      "scope": "db:read:orders",
      "riskLevel": "Medium",
      "justification": "Read order data to calculate payment amounts",
      "flags": []
    }
  ],
  "permissionDiff": null,
  "verificationResult": { "status": "Passed" }
}
```

For full permission review model, see `docs/architecture/permission-review-spec.md`.

---

# 6. SUBSCRIPTIONS (Inter-Extension Access)

## Request Subscription

```
POST /api/v1/extensions/{targetExtensionId}/subscribe
Authorization: Bearer <token>
```

Request:

```json
{
  "requestedBy": "order-extension",
  "reason": "Need to call payment-service for order payments",
  "expectedUsage": {
    "callsPerDay": 1000,
    "peakConcurrency": 10
  }
}
```

Response (202 Accepted):

```json
{
  "subscriptionId": "guid",
  "status": "Requested"
}
```

## List Subscription Requests (for extension owner)

```
GET /api/v1/extensions/{extensionId}/subscriptions?status=Requested
Authorization: Bearer <token>
```

## Decide Subscription

```
POST /api/v1/extensions/{extensionId}/subscriptions/{subscriptionId}/decide
Authorization: Bearer <token>
```

Request:

```json
{
  "decision": "Approved",
  "conditions": "Rate limit: 100 calls/min",
  "expiresAt": "2027-01-01T00:00:00Z"
}
```

## Revoke Subscription

```
POST /api/v1/extensions/{extensionId}/subscriptions/{subscriptionId}/revoke
Authorization: Bearer <token>
```

For full inter-extension spec, see `docs/architecture/inter-extension-spec.md`.

---

# 7. AUDIT

## Query Audit Logs

```
GET /api/v1/audit?from=2026-01-01&to=2026-01-31&action=PluginRevoked
Authorization: Bearer <token>
```

Response (200):

```json
{
  "events": [
    {
      "auditId": "audit-001",
      "action": "PluginRevoked",
      "actor": "admin@company.com",
      "target": "payment-service:1.0.0",
      "timestamp": "2026-01-15T10:30:00Z"
    }
  ],
  "total": 1
}
```

---

# 8. HEALTH & MONITORING

## Health Check

```
GET /health
```

Response (200):

```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy",
    "storage": "Healthy"
  }
}
```

## Readiness Check

```
GET /ready
```

## Metrics (Prometheus format)

```
GET /metrics
```

---

# 9. ERROR FORMAT

All errors follow a consistent structure:

```json
{
  "error": {
    "code": "string",
    "category": "Validation|Security|Execution|Infrastructure",
    "message": "Human-readable description",
    "traceId": "string",
    "timestamp": "ISO 8601"
  }
}
```

For full error code taxonomy, see `docs/implementation/error-handling.md`.

---

# 10. AUTHENTICATION

All endpoints (except `/health` and `/ready`) require:
- Bearer token (JWT)
- Valid issuer and audience

For auth details, see `docs/implementation/authentication-flow.md`.

---

# 11. RATE LIMITING

- Configurable per endpoint
- Returns `429 Too Many Requests` with `Retry-After` header when exceeded

---

# 12. DESIGN PRINCIPLE

> API layer is ONLY a gateway. It validates requests, delegates to Runtime Engine, and formats responses. No business logic lives here.

---

# 🏁 END
