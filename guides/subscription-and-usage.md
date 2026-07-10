# Subscription Guide — Using Extensions

This guide explains how to subscribe to and use extensions on the Plugin Runtime Platform.

## How Access Works

Not all extensions are freely available. The platform uses three access levels:

| Access Level | Who can use | How to get access |
|-------------|-------------|-------------------|
| **Free** | Everyone | Automatically available — no action needed |
| **Package** | Subscribers | Subscribe to a plugin package that includes the extension |
| **Subscription** | Approved requesters | Send a subscription request to the extension owner |

## Getting Started as an API Consumer

### Step 1: Register Your Account

Sign up through the Consumer Portal. You'll choose a plan:

| Plan | Monthly Price | Daily Requests | API Keys | Package Subscriptions |
|------|:---:|:---:|:---:|:---:|
| Free | $0 | 100 | 2 | 0 |
| Pro | $49 | 10,000 | 10 | 5 |
| Enterprise | $299 | Unlimited | 50 | Unlimited |

### Step 2: Get Your API Key

After registration, the platform generates your first API key. This key is shown exactly once — copy it immediately.

```
Your API Key: acme_pro_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx7k9

⚠️ This key will not be shown again. Store it securely.
```

### Step 3: Make Your First API Call

```bash
curl https://gateway.example.com/api/plugins/execute \
  -H "X-Api-Key: your-api-key-here" \
  -H "Content-Type: application/json" \
  -d '{"extension_id": "com.example.hello-world", "input": {}}'
```

## Subscribing to Plugin Packages

Plugin packages are curated bundles of extensions offered at a monthly fee.

### What's in a Package?

Each package contains a set of related extensions. For example:

- **Analytics Suite** ($19.99/mo) — Real-time dashboards, data aggregation, reporting tools
- **Security Toolkit** ($29.99/mo) — Vulnerability scanning, threat detection, compliance reports
- **Developer Tools** ($14.99/mo) — Code generation, testing utilities, CI/CD integrations

### How to Subscribe

**Via Consumer Portal:**
1. Go to Consumer Portal → Plans
2. Browse available packages
3. Click "Subscribe" on the package you want
4. Confirm — billing starts immediately

**Via API:**
```bash
curl -X POST https://api.example.com/api/subscriptions/packages/{packageId}/subscribe \
  -H "Authorization: Bearer <token>"
```

### What Happens After Subscribing

- All extensions in the package become immediately available to you
- Your plugin access list is updated in real-time
- The monthly fee is added to your next invoice
- You can invoke any extension in the package using your API key

### Unsubscribing

- You can unsubscribe at any time
- Access continues until the end of your current billing period
- No refunds for partial months

## Extension-to-Extension Subscriptions

Some extensions have **Subscription** visibility — meaning your extension needs approval from the owner before it can call them.

### Why This Exists

Extension owners control who can invoke their extensions. This allows:
- Paid extensions to verify subscribers
- Private APIs to restrict access to trusted partners
- Rate-sensitive services to manage their caller load

### Requesting a Subscription

**Via Marketplace Portal:**
1. Find the extension in the Marketplace
2. Click "Request Subscription"
3. Fill in:
   - **Reason** — Why you need access
   - **Expected Usage** — How many calls per day, usage pattern
4. Submit the request

**Via API:**
```bash
curl -X POST https://api.example.com/api/subscriptions \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "targetExtensionId": "com.partner.enrichment-service",
    "reason": "Need data enrichment for our analytics pipeline",
    "expectedUsage": {
      "requestsPerDay": 500,
      "usagePattern": "Batch processing every hour"
    }
  }'
```

### Approval Process

After you submit a request:

1. The extension owner receives a notification
2. They review your reason and expected usage
3. They can:
   - **Approve** — Optionally with conditions or an expiration date
   - **Reject** — With a reason explaining why

You'll be notified of the decision. If approved, you can immediately start calling the extension.

### Checking Request Status

**Via Marketplace Portal:**
- Go to My Subscriptions → Outgoing Requests
- See status: Pending / Approved / Rejected

**Via API:**
```bash
curl https://api.example.com/api/subscriptions/outgoing \
  -H "Authorization: Bearer <token>"
```

## Managing Incoming Requests (for Extension Owners)

If you publish extensions with Subscription visibility, other developers will send you requests.

**Via Marketplace Portal:**
1. Go to My Subscriptions → Incoming Requests
2. Review each request: who's asking, why, expected usage
3. Click Approve or Reject

**Approving with conditions:**
- You can set an expiration date (access revoked automatically)
- You can add notes about usage limits or expectations

## Plan Limits and Quotas

### Package Subscription Limits

Your plan limits how many packages you can subscribe to:
- **Free** — Cannot subscribe to any packages
- **Pro** — Up to 5 active package subscriptions
- **Enterprise** — Unlimited

### Daily Request Quotas

Each plan has a daily limit on total API requests:
- **Free** — 100 requests/day
- **Pro** — 10,000 requests/day
- **Enterprise** — Unlimited

When you exceed your quota, requests return `429 Too Many Requests` with a `Retry-After` header indicating when your quota resets (next UTC midnight).

### Rate Limits

In addition to daily quotas, there are per-minute rate limits to prevent bursts:
- **Free** — 100 requests/minute
- **Pro** — 10,000 requests/minute
- **Enterprise** — Unlimited

### Overage Billing

On the Pro plan, if you exceed your daily quota, overage charges apply:
- $0.50 per 1,000 requests over the daily limit
- Overage appears as a line item on your monthly invoice

## Billing and Invoices

### Monthly Invoice Breakdown

Your monthly invoice includes:
1. **Base Plan Fee** — Your plan's monthly price
2. **Overage Charges** — Requests exceeding daily quota (Pro plan only)
3. **Package Subscriptions** — Sum of all active package fees

Example invoice:
```
Plan (Pro):                    $49.00
Overage (2,500 excess × $0.50/1k):  $1.25
Analytics Suite package:       $19.99
Security Toolkit package:      $29.99
─────────────────────────────────────
Total:                        $100.23
```

### Payment

- Payments are processed automatically via Stripe
- Failed payments result in account suspension after 30 days
- You can manage payment methods through the Consumer Portal → Billing → Manage Payment

## Upgrading and Downgrading Plans

### Upgrade (takes effect immediately)

- New limits apply right away
- You're charged the prorated difference for the rest of the billing period
- Example: Upgrading from Free to Pro mid-month charges half the monthly fee

### Downgrade (takes effect next billing period)

- Current limits remain active until your next billing date
- The platform verifies you don't exceed the new plan's limits:
  - Too many API keys? Revoke some first
  - Too many package subscriptions? Unsubscribe first
- New pricing starts on your next billing cycle

## Monitoring Your Usage

### Consumer Portal Dashboard

Your dashboard shows:
- Today's request count vs. quota
- Active API keys and expiring ones
- Recent activity (last 5 days)
- Current plan and subscription status

### Usage Analytics

Go to Usage Analytics for detailed charts:
- Daily requests over time (with quota line)
- Success rate per day
- Average response time
- Date range filtering (up to 90 days)

### Quota Warnings

When your daily usage exceeds 80% of your quota, you'll see a warning badge on the dashboard. This gives you time to either reduce usage or upgrade your plan.

## Quick Reference

| Action | Where |
|--------|-------|
| Subscribe to a package | Consumer Portal → Plans |
| Request extension access | Marketplace → Extension Detail → Request Subscription |
| Check subscription status | Marketplace → My Subscriptions |
| View usage | Consumer Portal → Usage Analytics |
| Manage API keys | Consumer Portal → API Keys |
| View invoices | Consumer Portal → Billing |
| Change plan | Consumer Portal → Plans |
| Manage payment | Consumer Portal → Billing → Manage Payment |
