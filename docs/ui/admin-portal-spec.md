# 🖥️ Admin Portal Specification - Blazor Server

---

# 1. PURPOSE

Defines the UI design for the Extension Management Admin Portal — a Blazor Server application for managing the plugin lifecycle, reviewing permissions, and monitoring runtime.

---

# 2. TECHNOLOGY

- **Framework**: Blazor Server (.NET 10)
- **UI Library**: MudBlazor (Material Design components)
- **Auth**: OpenID Connect (same IdP as Runtime API)
- **Real-time**: SignalR (built-in to Blazor Server)
- **Charts**: MudBlazor charts or lightweight charting lib

No JavaScript frameworks. Pure Blazor + C#.

---

# 3. PROJECT STRUCTURE

```
src/
└── Admin/
    └── PluginRuntime.Admin/
        ├── PluginRuntime.Admin.csproj
        ├── Program.cs
        ├── Pages/
        │   ├── Dashboard.razor
        │   ├── Extensions/
        │   │   ├── ExtensionList.razor
        │   │   ├── ExtensionDetail.razor
        │   │   ├── ExtensionUpload.razor
        │   │   └── ExtensionVersions.razor
        │   ├── Approvals/
        │   │   ├── ApprovalQueue.razor
        │   │   ├── PermissionReview.razor
        │   │   └── ApprovalHistory.razor
        │   ├── Monitoring/
        │   │   ├── ExecutionMonitor.razor
        │   │   ├── RuntimeNodes.razor
        │   │   └── Metrics.razor
        │   ├── Audit/
        │   │   └── AuditLog.razor
        │   └── Settings/
        │       ├── Capabilities.razor
        │       └── Configuration.razor
        ├── Components/
        │   ├── Layout/
        │   │   ├── MainLayout.razor
        │   │   ├── NavMenu.razor
        │   │   └── TopBar.razor
        │   ├── Shared/
        │   │   ├── RiskBadge.razor
        │   │   ├── StatusChip.razor
        │   │   ├── PermissionCard.razor
        │   │   ├── DiffViewer.razor
        │   │   └── ConfirmDialog.razor
        │   └── Charts/
        │       ├── ExecutionChart.razor
        │       └── RiskDistribution.razor
        └── Services/
            ├── IExtensionService.cs
            ├── IApprovalService.cs
            ├── IMonitoringService.cs
            └── IAuditService.cs
```

---

# 4. NAVIGATION STRUCTURE

```
┌─────────────────────────────────────────────────────────┐
│  🔌 Plugin Runtime Admin                    [User ▼]    │
├──────────────┬──────────────────────────────────────────┤
│              │                                          │
│  📊 Dashboard│          [Page Content]                  │
│              │                                          │
│  📦 Extensions                                         │
│    ├ List    │                                          │
│    └ Upload  │                                          │
│              │                                          │
│  🏪 Marketplace                                        │
│              │                                          │
│  ✅ Approvals│                                          │
│    ├ Queue   │                                          │
│    └ History │                                          │
│              │                                          │
│  🔗 Subscriptions                                      │
│              │                                          │
│  📈 Monitor  │                                          │
│    ├ Executions                                        │
│    ├ Nodes   │                                          │
│    └ Metrics │                                          │
│              │                                          │
│  📋 Audit    │                                          │
│              │                                          │
│  ⚙️ Settings │                                          │
│              │                                          │
└──────────────┴──────────────────────────────────────────┘
```

---

# 5. SCREENS

---

## 5.1 Dashboard

**Route**: `/`

**Purpose**: Overview of system health and pending actions.

```
┌─────────────────────────────────────────────────────────────┐
│  Dashboard                                                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐     │
│  │ 42       │ │ 5        │ │ 3        │ │ 99.8%    │     │
│  │ Plugins  │ │ Pending  │ │ Running  │ │ Uptime   │     │
│  │ Active   │ │ Approval │ │ Now      │ │          │     │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘     │
│                                                             │
│  ┌─────────────────────────────┐ ┌───────────────────────┐ │
│  │ Executions (last 24h)       │ │ Recent Activity       │ │
│  │                             │ │                       │ │
│  │  [Line chart]               │ │ • PaymentPlugin v1.2  │ │
│  │  Success ━━━                │ │   Approved 2h ago     │ │
│  │  Failed  ┄┄┄                │ │ • OrderPlugin v2.0    │ │
│  │  Timeout ┈┈┈                │ │   Uploaded 3h ago     │ │
│  │                             │ │ • ShippingPlugin v1.0 │ │
│  │                             │ │   Revoked 5h ago      │ │
│  └─────────────────────────────┘ └───────────────────────┘ │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Security Alerts                                      │   │
│  │ ⚠ 2 signature failures in last hour                 │   │
│  │ ⚠ 1 capability violation detected                   │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

**Data cards:**
- Total active plugins
- Pending approvals (clickable → approval queue)
- Currently running executions
- System uptime percentage

**Charts:**
- Execution success/failure/timeout over 24h
- Top 5 plugins by execution count

**Activity feed:**
- Recent uploads, approvals, revocations (real-time via SignalR)

---

## 5.2 Extension List

**Route**: `/extensions`

**Purpose**: Browse and manage all registered extensions.

```
┌─────────────────────────────────────────────────────────────┐
│  Extensions                              [+ Upload] [🔍]    │
├─────────────────────────────────────────────────────────────┤
│  Filter: [All ▼] [Status ▼] [Capability ▼]   Search: [___] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 📦 payment-service              v1.2.0   🟢 Active  │   │
│  │    Process credit card payments                      │   │
│  │    Capabilities: Database, Network, Cache            │   │
│  │    Last executed: 2 min ago  |  Executions: 1,234    │   │
│  │                                         [View ▶]    │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 📦 order-processor              v2.0.0   🟡 Pending  │   │
│  │    Process and validate incoming orders              │   │
│  │    Capabilities: Database, Storage                   │   │
│  │    Awaiting approval                                 │   │
│  │                                         [View ▶]    │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 📦 legacy-importer              v1.0.0   🔴 Revoked  │   │
│  │    Import data from legacy system                    │   │
│  │    Capabilities: Database, Network                   │   │
│  │    Revoked: Security vulnerability found             │   │
│  │                                         [View ▶]    │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  [◀ 1 2 3 ... 10 ▶]                                       │
└─────────────────────────────────────────────────────────────┘
```

**Features:**
- Filter by status (Active, Pending, Revoked, Archived)
- Filter by capability type
- Search by name/author
- Sortable columns
- Pagination
- Click → Extension Detail

---

## 5.3 Extension Detail

**Route**: `/extensions/{pluginId}`

**Purpose**: Full detail view for a single extension.

```
┌─────────────────────────────────────────────────────────────┐
│  ← Back   payment-service                    [Revoke] [Reload]│
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Status: 🟢 Active    Author: dev@company.com              │
│  Current: v1.2.0      Created: 2026-01-01                  │
│                                                             │
│  ┌─── Tabs ───────────────────────────────────────────┐    │
│  │ [Overview] [Versions] [Permissions] [Executions] [Audit]│ │
│  └─────────────────────────────────────────────────────┘    │
│                                                             │
│  ── Overview Tab ──                                         │
│                                                             │
│  Description:                                               │
│  Process credit card payments via Stripe API                │
│                                                             │
│  Capabilities:                                              │
│  ┌────────────────┐ ┌─────────────────┐ ┌────────────┐    │
│  │ 🗄 Database    │ │ 🌐 Network      │ │ 💾 Cache   │    │
│  │ read: orders   │ │ api.stripe.com  │ │ payment-*  │    │
│  │ write: orders  │ │                 │ │            │    │
│  └────────────────┘ └─────────────────┘ └────────────┘    │
│                                                             │
│  Execution Policy:                                          │
│  Timeout: 5000ms | Memory: 256MB | Parallel: No            │
│                                                             │
│  Runtime Stats (last 7 days):                               │
│  Executions: 8,542 | Success: 99.2% | Avg: 120ms          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Tabs:**
- **Overview** — description, capabilities, execution policy, stats
- **Versions** — version history with status per version
- **Permissions** — full permission list with risk levels
- **Executions** — recent execution log (filterable)
- **Audit** — audit trail for this plugin only

---

## 5.4 Permission Review (Approval)

**Route**: `/approvals/{pluginVersionId}`

**Purpose**: The core review screen where admins approve/reject extensions.

```
┌─────────────────────────────────────────────────────────────┐
│  ← Queue   Review: order-processor v2.0.0                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Author: dev@company.com    Uploaded: 2026-01-15 10:00      │
│  Verification: ✅ Passed    Risk Level: 🟠 High             │
│                                                             │
│  ┌─── Permission Summary ──────────────────────────────┐   │
│  │  Total: 5 permissions                                │   │
│  │  🟢 Low: 2  │  🟡 Medium: 1  │  🟠 High: 2         │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─── Permissions Detail ──────────────────────────────┐   │
│  │                                                      │   │
│  │  🟠 db:write:orders                      [HIGH]      │   │
│  │  ┊  Justification: Update order payment_status       │   │
│  │  ┊  Frequency: per-execution                         │   │
│  │  ┊  Data sensitivity: medium                         │   │
│  │  ┊  Flags: [write-access]                           │   │
│  │                                                      │   │
│  │  🟠 network:outbound:https://api.stripe.com/*        │   │
│  │  ┊  Justification: Process payments via Stripe       │   │
│  │  ┊  Data sent: [order_id, amount, currency]          │   │
│  │  ┊  Data received: [transaction_id, status]          │   │
│  │  ┊  Flags: [external-api] [financial-data]          │   │
│  │                                                      │   │
│  │  🟡 db:read:orders                       [MEDIUM]    │   │
│  │  ┊  Justification: Read order data for calculations  │   │
│  │  ┊  Frequency: per-execution                         │   │
│  │                                                      │   │
│  │  🟢 cache:read:payment-*                 [LOW]       │   │
│  │  ┊  Justification: Cache payment tokens              │   │
│  │                                                      │   │
│  │  🟢 cache:write:payment-*                [LOW]       │   │
│  │  ┊  Justification: Store tokens (TTL 5min)           │   │
│  │                                                      │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─── Diff vs Previous Version (v1.1.0) ───────────────┐   │
│  │  + Added: network:outbound:https://api.stripe.com/*  │   │
│  │  ~ Modified: (none)                                  │   │
│  │  - Removed: (none)                                   │   │
│  │  = Unchanged: 4 permissions                          │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─── Decision ────────────────────────────────────────┐   │
│  │  Comment: [                                         ]│   │
│  │                                                      │   │
│  │  [✅ Approve]  [⚠️ Approve w/ Conditions]            │   │
│  │  [❌ Reject]   [❓ Request Info]                      │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Key features:**
- Risk level prominently displayed (color-coded)
- Each permission expandable with full justification
- Auto-generated flags highlighted
- Diff view when upgrading existing plugin
- Decision buttons with comment field
- Conditions editor (for conditional approval)

---

## 5.5 Approval Queue

**Route**: `/approvals`

**Purpose**: List of all pending extensions awaiting review.

```
┌─────────────────────────────────────────────────────────────┐
│  Approval Queue                          [5 pending]        │
├─────────────────────────────────────────────────────────────┤
│  Sort: [Newest ▼]   Filter: [All Risk ▼]                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 🟠 order-processor v2.0.0         Risk: High         │  │
│  │    Author: dev@company.com                           │  │
│  │    Uploaded: 15 min ago                              │  │
│  │    Permissions: 5 (2 high, 1 medium, 2 low)          │  │
│  │    New: +1 network permission                        │  │
│  │                                       [Review ▶]    │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 🔴 analytics-engine v1.0.0        Risk: Critical     │  │
│  │    Author: external@vendor.com                       │  │
│  │    Uploaded: 1 hour ago                              │  │
│  │    Permissions: 8 (1 critical, 3 high, 4 medium)     │  │
│  │    ⚠ First version — full review required            │  │
│  │                                       [Review ▶]    │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 🟡 notification-service v1.1.0    Risk: Medium       │  │
│  │    Author: team@company.com                          │  │
│  │    Uploaded: 2 hours ago                             │  │
│  │    Permissions: 3 (0 high, 2 medium, 1 low)          │  │
│  │    No permission changes from v1.0.0                 │  │
│  │                                       [Review ▶]    │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Sort by**: Risk level (critical first), upload time, author
**Queue indicators**: badge count in nav, real-time updates via SignalR

---

## 5.6 Extension Upload

**Route**: `/extensions/upload`

**Purpose**: Upload new extension package.

```
┌─────────────────────────────────────────────────────────────┐
│  Upload Extension                                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │                                                      │  │
│  │         📁 Drag & drop .plugin.zip here              │  │
│  │              or [Browse files]                        │  │
│  │                                                      │  │
│  │         Max size: 100 MB                             │  │
│  │                                                      │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  ── After upload: Verification Progress ──                  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Stage 1: Structure      ✅ Passed         50ms      │  │
│  │  Stage 2: Manifest       ✅ Passed         20ms      │  │
│  │  Stage 3: Static Scan    ⏳ Running...               │  │
│  │  Stage 4: Dependencies   ○ Waiting                   │  │
│  │  Stage 5: Security       ○ Waiting                   │  │
│  │  Stage 6: Standards      ○ Waiting                   │  │
│  │  Stage 7: Sandbox        ○ Waiting                   │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  ── Result (after completion) ──                            │
│                                                             │
│  ✅ Verification PASSED                                     │
│  Extension submitted to approval queue.                     │
│                                                             │
│  OR                                                         │
│                                                             │
│  ❌ Verification FAILED                                     │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  ILS-002: Direct HttpClient usage detected           │  │
│  │  File: PaymentPlugin.dll                             │  │
│  │  Fix: Use INetworkCapability.SendAsync() instead     │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Features:**
- Drag-and-drop upload
- Real-time verification progress (SignalR)
- Stage-by-stage status indicators
- Clear error reporting with suggested fixes
- Link to extension standard documentation

---

## 5.7 Execution Monitor

**Route**: `/monitoring/executions`

**Purpose**: Real-time and historical view of plugin executions.

```
┌─────────────────────────────────────────────────────────────┐
│  Execution Monitor                    🟢 Live (SignalR)     │
├─────────────────────────────────────────────────────────────┤
│  Filter: [All Plugins ▼] [All Status ▼] [Last 1h ▼]       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ [Real-time execution rate chart — last 5 minutes]    │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌────────────────────────────────────────────────────────┐│
│  │ TraceId        │ Plugin       │ Status   │ Duration    ││
│  ├────────────────┼──────────────┼──────────┼─────────────┤│
│  │ abc-123        │ payment-svc  │ ✅ OK    │ 120ms       ││
│  │ def-456        │ order-proc   │ ✅ OK    │ 85ms        ││
│  │ ghi-789        │ analytics    │ ❌ Failed│ 340ms       ││
│  │ jkl-012        │ payment-svc  │ ⏱ Timeout│ 5000ms      ││
│  │ mno-345        │ notifier     │ ✅ OK    │ 45ms        ││
│  └────────────────┴──────────────┴──────────┴─────────────┘│
│                                                             │
│  Click row → Execution detail (trace, input, output, logs) │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 5.8 Extension Marketplace

**Route**: `/marketplace`

**Purpose**: Browse public and subscription extensions available for inter-extension invocation.

```
┌─────────────────────────────────────────────────────────────┐
│  Extension Marketplace                    [🔍 Search]       │
├─────────────────────────────────────────────────────────────┤
│  Filter: [All ▼] [Category ▼] [Visibility ▼]              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 📦 payment-service              🔒 Subscription      │  │
│  │    Process credit card payments via Stripe            │  │
│  │    Author: platform-team  |  Subscribers: 12         │  │
│  │    Input: { orderId, amount, currency }               │  │
│  │    Output: { transactionId, status }                  │  │
│  │    Rate limit: 100/min per caller                     │  │
│  │                                   [Request Access]    │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 📦 notification-service          🌐 Public            │  │
│  │    Send notifications (email, push, SMS)              │  │
│  │    Author: platform-team  |  Invocations/day: 12k    │  │
│  │    Input: { type, recipient, message }                │  │
│  │                         [Copy permission string]      │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Features:**
- Browse all Public + Subscription extensions
- View input/output schemas
- Request subscription (for Subscription extensions)
- Copy permission string for manifest (`extension:invoke:{id}`)
- Show usage stats (invocations/day, subscribers count)

---

## 5.9 Subscription Management

**Route**: `/extensions/{extensionId}/subscriptions`

**Purpose**: Extension owner manages incoming subscription requests.

```
┌─────────────────────────────────────────────────────────────┐
│  ← Back   Subscriptions for: payment-service                │
├─────────────────────────────────────────────────────────────┤
│  [Pending (3)] [Active (12)] [Rejected (2)] [Revoked (1)]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ── Pending Tab ──                                          │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ order-extension wants access                         │  │
│  │ Reason: Process order payments                        │  │
│  │ Expected: 1000 calls/day, peak 10 concurrent         │  │
│  │ Requested: 2 hours ago                                │  │
│  │                                                      │  │
│  │ [✅ Approve] [❌ Reject] [❓ Ask More]                │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  ── Active Tab ──                                           │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ inventory-extension     Approved: 2026-01-10         │  │
│  │ Usage: 450 calls/day    Expires: 2027-01-10          │  │
│  │                                        [Revoke]      │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Features:**
- View pending requests with reason + expected usage
- Approve/Reject/Ask for more info
- View active subscriptions with usage metrics
- Revoke subscription at any time
- Set expiration dates
- Add conditions to approvals

---

## 5.10 Audit Log

**Route**: `/audit`

**Purpose**: Immutable audit trail viewer.

```
┌─────────────────────────────────────────────────────────────┐
│  Audit Log                                                  │
├─────────────────────────────────────────────────────────────┤
│  Filter: [All Actions ▼] [Date Range] [Actor ▼]  [Export]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 2026-01-15 10:30  │ admin@co │ PluginRevoked        │  │
│  │                   │          │ payment-svc v1.0.0    │  │
│  ├───────────────────┼──────────┼──────────────────────┤  │
│  │ 2026-01-15 10:15  │ system   │ SignatureFailure     │  │
│  │                   │          │ unknown-plugin        │  │
│  ├───────────────────┼──────────┼──────────────────────┤  │
│  │ 2026-01-15 09:45  │ reviewer │ PluginApproved       │  │
│  │                   │          │ order-proc v2.0.0     │  │
│  └───────────────────┴──────────┴──────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

# 6. ROLE-BASED ACCESS

| Page | Admin | SecurityOfficer | Developer | Auditor |
|------|-------|----------------|-----------|---------|
| Dashboard | ✅ | ✅ | ✅ (limited) | ✅ (limited) |
| Extension List | ✅ | ✅ | ✅ (own only) | ✅ (read-only) |
| Extension Detail | ✅ | ✅ | ✅ (own only) | ✅ (read-only) |
| Upload | ✅ | ❌ | ✅ | ❌ |
| Approval Queue | ✅ | ✅ | ❌ | ❌ |
| Permission Review | ✅ | ✅ | ❌ | ❌ |
| Execution Monitor | ✅ | ✅ | ✅ (own plugins) | ✅ |
| Audit Log | ✅ | ✅ | ❌ | ✅ |
| Settings | ✅ | ❌ | ❌ | ❌ |

---

# 7. REAL-TIME FEATURES (SignalR)

| Feature | Hub | Update |
|---------|-----|--------|
| Approval queue count | NotificationHub | New upload → badge update |
| Verification progress | VerificationHub | Stage completion → progress bar |
| Execution monitor | ExecutionHub | New execution → table row |
| Security alerts | AlertHub | Security event → toast notification |
| Dashboard stats | StatsHub | Periodic (5s) counter updates |

---

# 8. COMPONENT LIBRARY

Using **MudBlazor** for consistent Material Design:

| UI Element | MudBlazor Component |
|-----------|-------------------|
| Data tables | `MudTable<T>` |
| Cards | `MudCard` |
| Status badges | `MudChip` |
| Risk levels | Custom `RiskBadge.razor` |
| Charts | `MudChart` |
| Dialogs | `MudDialog` |
| File upload | `MudFileUpload` |
| Navigation | `MudNavMenu` |
| Tabs | `MudTabs` |
| Notifications | `MudSnackbar` |
| Filters | `MudSelect`, `MudDateRangePicker` |

---

# 9. API INTEGRATION

Admin Portal communicates with Runtime API via typed HttpClient:

```csharp
builder.Services.AddHttpClient<IExtensionService, ExtensionService>(client =>
{
    client.BaseAddress = new Uri(configuration["ApiBaseUrl"]!);
});
```

All services implement interfaces for testability.

---

# 10. RESPONSIVE DESIGN

- Desktop-first (admin tool)
- Minimum supported: 1280px width
- Sidebar collapsible on smaller screens
- Tables scroll horizontally on narrow viewports

---

# 11. ACCESSIBILITY

- ARIA labels on all interactive elements
- Keyboard navigation for all actions
- Color-coded risk levels also have text labels
- High contrast mode support (via MudBlazor theme)

---

# 🏁 END
