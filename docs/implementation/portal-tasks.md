# Implementation Tasks — Full Platform

> Cập nhật: 2026-07-13
> Mục tiêu: Hoàn thiện toàn bộ Plugin Runtime Platform

---

## Tổng Quan Tiến Độ

| Hạng mục | Trạng thái | Hoàn thành |
|----------|:----------:|:----------:|
| Portal UI (Admin) | ✅ Done | 100% |
| Portal UI (Consumer) | ✅ Done | 100% |
| Portal UI (Marketplace) | ✅ Done | 100% |
| Aspire Orchestration | ✅ Done | 100% |
| Service Layer (Consumer) | ✅ Done | 100% |
| Service Layer (Marketplace) | ✅ Done | 100% |
| Backend API Endpoints | ✅ Done | 100% |
| Plugin Runtime Engine | ✅ Done | 100% |
| Mock Data / Demo Mode | ✅ Done | 100% |
| UX Enhancements | 🔲 Todo | 0% |
| Security & Auth | 🔲 Partial | 40% |
| Production Readiness | 🔲 Todo | 0% |

---

## ✅ Phase 1-3: Portal UI (HOÀN THÀNH)

<details>
<summary>Xem chi tiết (đã implement)</summary>

### Consumer Portal — 9/9 pages
- ✅ Dashboard (quota, plan badge, activity, quick actions)
- ✅ API Keys (CRUD, copy, rotate, revoke)
- ✅ Usage Analytics (Chart.js bar chart, summary cards, date filter)
- ✅ Plans (3 pricing cards, upgrade/downgrade)
- ✅ Billing (invoice history, billing summary)
- ✅ Documentation (API reference, code examples C#/Python/JS)
- ✅ Settings (profile, notification preferences)
- ✅ Support (FAQ + ticket form)
- ✅ Login (JWT auth, quick login)

### Marketplace Portal — 8/8 pages
- ✅ Home (ecosystem stats, featured extensions)
- ✅ Browse (search, filter, ExtensionCard grid)
- ✅ Extension Detail (tabs: overview/permissions/versions, subscribe/request)
- ✅ My Plugins (DataGrid, empty state)
- ✅ Upload (drag-drop, manifest preview, submit)
- ✅ Subscriptions (3 tabs: Active/Pending/Incoming, approve/reject)
- ✅ Settings (publisher profile, API key management)
- ✅ Docs (SDK quickstart, manifest reference, capabilities)
- ✅ Login (JWT auth, quick login)

### Admin Portal — 6/6 pages
- ✅ Dashboard (real-time metrics via SignalR)
- ✅ Extensions (DataGrid, filter, sort)
- ✅ Approvals (permission review, approve/reject dialog)
- ✅ Monitoring (live stream, historical table, pagination)
- ✅ Audit (6 filters, DataGrid, pagination)
- ✅ Marketplace (extension cards, subscribe)

</details>

---

## ✅ Phase 4: Mock Data & Demo Mode (HOÀN THÀNH)

<details>
<summary>Xem chi tiết (đã implement)</summary>

- ✅ ConsumerPortalController — 12 endpoints (usage/dashboard, keys CRUD, usage/daily, usage/summary, plans, plans/change, billing/summary, billing/invoices, tenants/me, profile, notifications, support/tickets)
- ✅ MarketplacePortalController — 15 endpoints (extensions list/detail/featured/stats/mine/versions, subscriptions outgoing/incoming/request/decide, plugins/upload/mine, profile CRUD, publishers)
- ✅ Seed data: 90 UsageAggregate records (30 days × 3 tenants), 10 extensions, 5 tenants, 4 plans, 5 API keys, 4 invoices, 4 subscriptions
- ✅ Demo Reset endpoint: POST /api/admin/reset-demo

</details>

---

## ✅ Phase 5: Backend API Completion (HOÀN THÀNH)

<details>
<summary>Xem chi tiết (đã implement)</summary>

- ✅ Task 5.1: Usage Controller — GET /api/usage/dashboard, /daily, /summary
- ✅ Task 5.2: API Keys Controller — GET/POST/DELETE /api/keys, POST /keys/{id}/rotate
- ✅ Task 5.3: Billing Controller — GET /api/billing/summary, /invoices
- ✅ Task 5.4: Plans Controller — GET /api/plans, POST /api/plans/change
- ✅ Task 5.5: Subscriptions Controller — outgoing/incoming/request/decide
- ✅ Task 5.6: Plugin Upload Controller — POST /api/plugins/upload, GET /mine, /versions
- ✅ Task 5.7: Profile & Tenant Controller — GET/PUT tenants/me, GET/PUT profile, keys
- ✅ Task 5.8: Support Controller — POST /api/support/tickets

</details>

---

## ✅ Phase 6: Plugin Runtime Engine (HOÀN THÀNH)

<details>
<summary>Xem chi tiết (đã implement)</summary>

**Core Runtime (đã có từ trước):**
- ✅ ManifestValidator — schema, expiration, version compatibility, algorithm, resource limits
- ✅ SignatureVerifier — RSA-SHA256 + ECDSA-SHA256 with canonical content
- ✅ HashVerifier — SHA-256 integrity check
- ✅ PluginLoader — collectible ALC, DLL loading, IPlugin resolution, unload support
- ✅ PluginAssemblyLoadContext — isolated dependency resolution
- ✅ CapabilityResolver — deny-by-default, factory pattern, audit logging
- ✅ ExecutionGovernor — timeout + memory + CPU via linked CancellationTokenSource
- ✅ ExecutionPipeline — 7-stage (Manifest → Signature → Hash → Revocation → Capabilities → Load → Execute)
- ✅ HotReloadManager — zero-downtime version transitions, drain coordination
- ✅ PipelineTelemetry — OpenTelemetry Activity spans per stage

**Thêm mới trong phase này:**
- ✅ ExecuteController — POST /api/plugins/execute (rate limit, pipeline, HTTP status mapping)
- ✅ DemoRateLimiter — always-allow stub for demo mode
- ✅ DemoExecutionPipeline — mock success response for demo mode
- ✅ DI registration in Program.cs (IRateLimiter, IExecutionPipeline)
- ✅ Project references added (Core, Runtime, Security, Capabilities.Abstractions)

**To swap to production:**
Replace `DemoExecutionPipeline` → real `ExecutionPipeline` and `DemoRateLimiter` → Redis sliding window.

</details>

---

## Phase 7: UX & Production Enhancements

### Task 7.1: Dark/Light Theme Toggle

- **Scope:** All 3 portals
- **Mô tả:** MudThemeProvider toggle, persist in localStorage
- **Effort:** Small (1-2 hours per portal)

### Task 7.2: Real-time Notifications (Consumer/Marketplace)

- **Mô tả:**
  - Consumer: quota warning (>80%), key expiring, subscription approved
  - Marketplace: new subscription request, version approved/rejected
- **Implementation:** SignalR hub (Admin already has it) — extend to WASM via WebSocket
- **UI:** Toast notifications via MudBlazor Snackbar + notification bell icon

### Task 7.3: Extension Search Advanced Filters

- **Scope:** Marketplace Browse page
- **Mô tả:** Add dropdowns: Category, Risk Level, Visibility, Sort By
- **Implementation:** Extend SearchEngine + add filter UI components

### Task 7.4: Extension Reviews & Ratings

- **Mô tả:**
  - Consumer can rate extension (1-5 stars + optional comment)
  - Display on ExtensionDetail page (new "Reviews" tab)
  - Average rating shown on ExtensionCard
- **Backend:** New entity `Review` + endpoints

### Task 7.5: Responsive Mobile Layout

- **Scope:** All portals
- **Mô tả:** Test/fix breakpoints, auto-collapse drawer on mobile, touch-friendly buttons
- **MudBlazor:** Already responsive — mainly testing + minor CSS fixes

### Task 7.6: Export Data (CSV/PDF)

- **Scope:** Consumer (usage, invoices), Admin (audit)
- **Mô tả:**
  - Usage page: "Export CSV" button → download daily breakdown
  - Billing: "Download PDF" per invoice
  - Admin Audit: "Export" button → CSV of filtered results
- **Implementation:** JS interop for file download, server-side CSV generation

### Task 7.7: Multi-language (i18n)

- **Scope:** All portals
- **Mô tả:** Vietnamese + English. Resource files per language.
- **Implementation:** `IStringLocalizer<T>` pattern, language toggle in layout
- **Effort:** Medium-Large (all text strings need extraction)

### Task 7.8: 2FA / TOTP Authentication

- **Scope:** All portals (especially Admin)
- **Mô tả:**
  - Enable TOTP (Google Authenticator / Authy)
  - QR code setup flow
  - Verify on each login
  - Recovery codes
- **Implementation:** Backend TOTP validation, frontend setup wizard

### Task 7.9: Webhook System

- **Scope:** Marketplace publishers, Consumer
- **Mô tả:**
  - Publisher: webhook on subscription request/decision, version approved/rejected
  - Consumer: webhook on quota warning, key expiring
  - Settings page: configure URL + events
- **Implementation:** Background job queue, retry logic, delivery log

### Task 7.10: API Playground

- **Scope:** Consumer Portal → Docs page
- **Mô tả:** Interactive API tester (like Swagger "Try it out") using user's own API key
- **UI:** Request builder → execute → show response
- **Implementation:** JS fetch from browser → Gateway → API

---

## Phase 8: Infrastructure & Ecosystem

### Task 8.1: PostgreSQL Integration

- **Mô tả:** Switch from JSON storage to PostgreSQL for production
- **Implementation:** EF Core migrations, repository pattern (already designed in docs)
- **Toggle:** `DatabaseProvider` setting (Json/PostgreSQL)

### Task 8.2: Redis Caching

- **Mô tả:** Cache frequently accessed data: plans, extension metadata, quota counters
- **Implementation:** IDistributedCache, Redis connection via Aspire

### Task 8.3: Plugin Sandbox Testing

- **Mô tả:** Publisher tests plugin in isolated sandbox before submission
- **Implementation:** Ephemeral ALC execution with mock capabilities, timeout, result display
- **UI:** Marketplace → My Plugins → "Test" button → sandbox execution result

### Task 8.4: CI/CD Pipeline

- **File:** `.github/workflows/ci.yml`
- **Mô tả:**
  - Build all projects
  - Run unit tests
  - Run integration tests
  - Publish Docker images (future)
- **Gate:** All tests pass before merge

### Task 8.5: Admin Tenant Management

- **Scope:** Admin Portal (new page)
- **Mô tả:**
  - List all tenants: name, plan, status, usage
  - Suspend/activate tenant
  - Override plan limits
  - View tenant's API keys and usage
- **Route:** `/tenants`

### Task 8.6: Marketplace Revenue Sharing

- **Scope:** Marketplace → new "Earnings" page
- **Mô tả:**
  - Publisher dashboard: total earnings, monthly breakdown
  - Payout history
  - Platform fee configuration (admin)
- **Dependencies:** Billing system, Stripe Connect (future)

### Task 8.7: Plugin Dependencies

- **Mô tả:** Extension A declares dependency on Extension B in manifest
- **Implementation:**
  - Manifest field: `"dependencies": ["com.other.plugin@^1.0"]`
  - Runtime resolver: validate all deps available before execution
  - UI: show dependency tree on ExtensionDetail page

### Task 8.8: Rate Limiter (Per-Tenant)

- **Mô tả:** Enforce rate limits per tenant per minute
- **Implementation:**
  - Sliding window counter (Redis)
  - Return `429` with `Retry-After` header
  - Dashboard shows current rate usage
- **Configuration:** Per-plan limits defined in plans table

---

## Thứ Tự Thực Hiện Tiếp Theo

```
Sprint tiếp theo:
  Task 7.1: Dark/Light Theme (quick win, 1-2h per portal)
  Task 7.3: Extension Search Advanced Filters
  Task 7.5: Responsive Mobile Layout

Sprint sau:
  Task 7.2: Real-time Notifications (Consumer/Marketplace via SignalR)
  Task 7.4: Extension Reviews & Ratings
  Task 7.6: Export Data (CSV/PDF)

Sprint 3:
  Task 8.4: CI/CD Pipeline
  Task 8.8: Rate Limiter (Redis sliding window — replace DemoRateLimiter)
  Task 8.1: PostgreSQL Integration

Sprint 4+:
  Task 8.2: Redis Caching
  Task 7.7: Multi-language (i18n)
  Task 7.8: 2FA / TOTP
  Task 7.9: Webhook System
  Task 8.3: Plugin Sandbox Testing
  Task 8.5: Admin Tenant Management page
  Task 8.6: Marketplace Revenue Sharing
  Task 8.7: Plugin Dependencies
  Task 7.10: API Playground

  → Swap DemoExecutionPipeline → real ExecutionPipeline (requires: IKeyProvider, IPluginBinaryStore, IObjectStorageService, IRevocationChecker, IObservabilityCollector, IAuditLogger, IPluginEventBus implementations)
```

---

## Port Map (Reference)

| Service | HTTP | HTTPS |
|---------|------|-------|
| Aspire Dashboard | 6000 | 6001 |
| API Backend | 6100 | 6101 |
| API Gateway | 6200 | 6201 |
| Marketplace | 6300 | 6301 |
| Consumer Portal | 6400 | 6401 |
| Admin Portal | 6500 | 6501 |
