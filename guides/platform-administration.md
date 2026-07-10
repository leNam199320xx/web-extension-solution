# Platform Administration Guide

This guide is for platform operators who manage tenants, approve plugins, and keep the system healthy.

## Your Responsibilities

As a platform admin, you:
- Review and approve/reject uploaded plugins
- Manage tenant accounts (create, suspend, reactivate, delete)
- Configure plans and pricing
- Create and manage plugin packages
- Monitor system health and usage
- Respond to security incidents

## Accessing the Admin Portal

Log in at the Admin Portal URL with an account that has the **Platform_Admin** role. This role grants access to all admin endpoints and the admin UI.

## Plugin Approval Workflow

When a developer uploads a new extension, it enters the approval queue.

### Reviewing a Plugin

For each pending plugin, check:

1. **Permissions requested** — Are they reasonable for what the plugin claims to do?
2. **Justifications** — Does the developer explain why each permission is needed?
3. **Security scan results** — Did the automated scan find any issues?
4. **Resource limits** — Are the requested limits (memory, CPU, execution time) appropriate?

### Decision Guide

| Situation | Action |
|-----------|--------|
| Permissions match the description, clean scan | Approve |
| Requests database write but description says "read-only analytics" | Reject — permissions don't match stated purpose |
| Requests network access without justification | Reject — ask developer to explain |
| Security scan flagged a known vulnerability | Reject — notify developer |
| Everything looks fine but excessive resource limits | Approve with reduced limits |

### Approving

When you approve, the platform:
1. Signs the manifest with the platform's private key
2. Stores the signed version
3. Makes the extension available for execution
4. Notifies the developer

### Rejecting

When you reject:
1. Provide a clear reason (the developer will see it)
2. Suggest what to fix if possible
3. The developer can fix and re-upload

## Tenant Management

### Creating Internal Tenants

Internal tenants are special accounts for platform operations:
- No billing or Stripe integration
- Unlimited rate limits and quotas
- Unlimited API keys and package subscriptions
- Only Platform_Admin can create them

### Suspending a Tenant

Use suspension when:
- Payment has been overdue for 30+ days
- Terms of service violation detected
- Security concern requiring investigation

What happens when suspended:
- All API requests from the tenant are rejected
- API keys remain but stop working
- A Redis notification alerts the gateway immediately
- An audit log entry records who suspended and why

### Reactivating a Tenant

After the issue is resolved:
- Reactivate from the Admin Portal
- All access resumes immediately
- Audit log records the reactivation

### Viewing Tenant Details

The admin endpoint shows:
- Tenant name, email, company
- Current plan and subscription status
- Active/revoked API keys
- Package subscriptions
- Usage history
- Audit trail

## Plan Management

### Available Plans

| Plan | Rate Limit | Daily Quota | Max Keys | Max Packages | Price |
|------|:---:|:---:|:---:|:---:|:---:|
| Free | 100/min | 100/day | 2 | 0 | $0 |
| Pro | 10,000/min | 10,000/day | 10 | 5 | $49/mo |
| Enterprise | Unlimited | Unlimited | 50 | Unlimited | $299/mo |
| Internal | Unlimited | Unlimited | Unlimited | Unlimited | $0 (no billing) |

### Changing a Tenant's Plan

You can force a plan change for any tenant:
- Upgrades take effect immediately
- Downgrades take effect at next billing cycle
- Plan changes are recorded in the audit log

## Plugin Package Management

### Creating a Package

1. Choose a name and description
2. Set a monthly price
3. Select which extensions to include
4. All included extensions must exist and be Active

### Updating a Package

- Add or remove extensions from a package at any time
- When plugins are added/removed, all subscribers' access is automatically recalculated
- A Redis notification is sent to the gateway so access changes propagate instantly

### Deactivating a Package

When a package is no longer offered:
- Set it to Inactive
- Existing subscribers keep access until their subscription ends
- No new subscriptions are accepted
- The package disappears from public listings

## API Key Management

### Revoking a Key

If a key is compromised:
1. Revoke it immediately via Admin Portal or API
2. The revocation propagates via Redis to the gateway within seconds
3. All requests using that key are rejected immediately
4. An audit entry is created

### Key Limits

Each plan has a maximum number of active keys. When a tenant reaches their limit, they cannot create new keys until they revoke existing ones or upgrade their plan.

## Monitoring and Health

### System Health

The `/health` endpoint checks:
- PostgreSQL connectivity
- Redis connectivity
- Module status (Plugins, Tenants, Billing, Subscriptions, Gateway)

Returns `200 Healthy` or `503 Unhealthy` with details about what's failing.

### Metrics

The `/metrics` endpoint provides Prometheus-format data:
- Total requests per module
- Error rates per module
- Stripe API latency
- Active package subscription count
- Gateway notification failures

### Usage Patterns to Watch

| Pattern | Concern | Action |
|---------|---------|--------|
| Sudden spike from one tenant | Possible abuse or misconfigured client | Check if legitimate, suspend if needed |
| High error rate on one extension | Plugin may have a bug | Contact developer, consider temporary disable |
| Gateway notification failures climbing | Redis connectivity issue | Check Redis health |
| Invoice payment failures | Billing integration issue | Check Stripe dashboard, contact tenant |

## Audit Trail

Every admin action is recorded:
- Who did it (actor ID)
- What they did (action type)
- What changed (previous state → new state)
- When (timestamp)
- Why (reason, if provided)

Audit entries are immutable — they cannot be edited or deleted.

### Actions That Are Audited

- Tenant suspension / reactivation / deletion
- Plan changes (forced by admin)
- API key revocations
- Plugin approvals / rejections
- Package creation / deactivation

## Security Incident Response

If you suspect a security issue:

1. **Suspend the affected tenant** — stops all their API access immediately
2. **Revoke compromised keys** — propagates to gateway within seconds
3. **Check audit logs** — understand what happened and when
4. **Review the extension** — if a plugin is involved, disable it
5. **Document everything** — update the audit with your findings

## Quick Reference

| Action | Admin Portal Location | API Endpoint |
|--------|----------------------|-------------|
| List tenants | Admin → Tenants | `GET /api/admin/tenants` |
| Suspend tenant | Admin → Tenant Detail → Suspend | `POST /api/tenants/{id}/suspend` |
| Approve plugin | Admin → Pending Plugins → Approve | — |
| List plans | Admin → Plans | `GET /api/admin/plans` |
| List packages | Admin → Packages | `GET /api/admin/packages` |
| View health | — | `GET /health` |
| View metrics | — | `GET /metrics` |
