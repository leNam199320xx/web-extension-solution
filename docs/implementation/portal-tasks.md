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
| Backend API Endpoints | 🔲 Todo | 30% |
| Plugin Runtime Engine | 🔲 Todo | 10% |
| Mock Data / Demo Mode | 🔲 Todo | 0% |
| UX Enhancements | 🔲 Todo | 0% |
| Security & Auth | 🔲 Todo | 20% |
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

## Phase 4: Mock Data & Demo Mode

> **Mục tiêu:** Cho phép demo toàn bộ UI mà không cần backend thật đang chạy.

### Task 4.1: API Mock Middleware

- **File:** `src/PluginRuntime.Api/Middleware/MockDataMiddleware.cs`
- **Mô tả:** Middleware intercept requests khi `DatabaseProvider=Json`, trả về JSON seed data
- **Scope:**
  - `GET /api/usage/dashboard` → fake DashboardUsageDto
  - `GET /api/keys` → 3 sample keys (Active, Expired, Revoked)
  - `GET /api/plans` → 3 plans (Free/Pro/Enterprise)
  - `GET /api/billing/summary` → fake billing data
  - `GET /api/billing/invoices` → 5 sample invoices
  - `GET /api/extensions` → 10 sample extensions
  - `GET /api/extensions/{id}` → full detail with permissions
  - `GET /api/subscriptions/outgoing` → 3 samples
  - `GET /api/subscriptions/incoming` → 2 samples
  - `POST /api/keys` → generate fake key
  - `POST /api/plugins/upload` → return success
  - `GET /api/usage/daily` → 30 days random data
- **Priority:** 🔴 Critical (enables full demo)

### Task 4.2: Seed Data Files

- **File:** `src/PluginRuntime.Api/data/seed/` folder
- **Mô tả:** JSON files cho mỗi entity type, load khi startup
- **Files:**
  - `tenants.json` — 3 tenants (Alice/Pro, Bob/Free, Carol/Enterprise)
  - `extensions.json` — 10 extensions with varied risk levels
  - `plans.json` — 3 plans with pricing
  - `api-keys.json` — sample keys per tenant
  - `invoices.json` — 6 months history
  - `subscriptions.json` — mixed statuses

### Task 4.3: Demo Reset Endpoint

- **File:** `src/PluginRuntime.Api/Controllers/AdminController.cs`
- **Route:** `POST /api/admin/reset-demo`
- **Mô tả:** Reset all JSON data to seed state. Useful for presentations.

---

## Phase 5: Backend API Completion

> **Mục tiêu:** Implement all real API endpoints matching portal service contracts.

### Task 5.1: Usage Controller

- **File:** `src/PluginRuntime.Api/Controllers/UsageController.cs`
- **Endpoints:**
  - `GET /api/usage/dashboard` — aggregated dashboard data per tenant
  - `GET /api/usage/daily?from=&to=` — daily breakdown
  - `GET /api/usage/summary?from=&to=` — period summary
- **Storage:** JSON file `data/usage/{tenantId}.json` (Json mode) or PostgreSQL

### Task 5.2: API Keys Controller

- **File:** `src/PluginRuntime.Api/Controllers/KeysController.cs`
- **Endpoints:**
  - `GET /api/keys` — list keys for current tenant
  - `POST /api/keys` — generate new key (SHA-256 hash stored, plaintext returned once)
  - `DELETE /api/keys/{id}` — revoke
  - `POST /api/keys/{id}/rotate` — revoke old + create new
- **Security:** Keys stored as hashes. Validate via timing-safe compare.

### Task 5.3: Billing Controller

- **File:** `src/PluginRuntime.Api/Controllers/BillingController.cs`
- **Endpoints:**
  - `GET /api/billing/summary` — current month charges, payment method
  - `GET /api/billing/invoices?page=&pageSize=` — paginated invoice list
  - `GET /api/billing/invoices/{id}` — invoice detail with daily breakdown
  - `GET /api/billing/invoices/{id}/pdf` — generate PDF (optional)
- **Integration:** Stripe (future) or mock billing engine

### Task 5.4: Plans Controller

- **File:** `src/PluginRuntime.Api/Controllers/PlansController.cs`
- **Endpoints:**
  - `GET /api/plans` — list all plans
  - `POST /api/plans/change` — change tenant plan (immediate/deferred)
- **Business rules:** Upgrade immediate, downgrade next cycle

### Task 5.5: Subscriptions Controller

- **File:** `src/PluginRuntime.Api/Controllers/SubscriptionsController.cs`
- **Endpoints:**
  - `GET /api/subscriptions/outgoing` — my requests to others
  - `GET /api/subscriptions/incoming` — requests to my extensions
  - `POST /api/subscriptions` — create subscription request
  - `POST /api/subscriptions/{id}/decide` — approve/reject
- **Notifications:** Emit event when decided (for webhook/SignalR later)

### Task 5.6: Plugin Upload Controller

- **File:** `src/PluginRuntime.Api/Controllers/PluginUploadController.cs`
- **Endpoints:**
  - `POST /api/plugins/upload` — accept multipart zip, extract manifest, validate, store
  - `GET /api/plugins/mine` — list publisher's plugins
  - `GET /api/extensions/{id}/versions` — version history
- **Validation:** Manifest schema, file size, duplicate version check

### Task 5.7: Profile & Tenant Controller

- **File:** `src/PluginRuntime.Api/Controllers/ProfileController.cs`
- **Endpoints:**
  - `GET /api/tenants/me` — current tenant info
  - `PUT /api/tenants/me/profile` — update profile
  - `PUT /api/tenants/me/notifications` — update preferences
  - `GET /api/profile` — marketplace user profile
  - `PUT /api/profile` — update marketplace profile
  - `GET /api/profile/keys` — marketplace API keys
  - `POST /api/profile/keys` — generate marketplace key
  - `DELETE /api/profile/keys/{id}` — revoke

### Task 5.8: Support Controller

- **File:** `src/PluginRuntime.Api/Controllers/SupportController.cs`
- **Endpoints:**
  - `POST /api/support/tickets` — submit ticket
  - `GET /api/support/tickets` — list user's tickets (future)
- **Storage:** JSON file or forward to external ticketing system

---

## Phase 6: Plugin Runtime Engine

> **Mục tiêu:** Core execution pipeline — load, validate, execute plugins.

### Task 6.1: Manifest Validator

- **Project:** `src/Core/PluginRuntime.Security/`
- **Mô tả:**
  - Parse manifest.json from plugin zip
  - Validate schema (required fields, valid semver, known capabilities)
  - Check signature (RSA/ECDSA digital signature over manifest hash)
  - Verify file integrity (SHA-256 of DLL matches manifest declaration)
- **Output:** `ManifestValidationResult` (Valid/Invalid + reasons)

### Task 6.2: Plugin Loader (AssemblyLoadContext)

- **Project:** `src/Core/PluginRuntime.Runtime/`
- **Mô tả:**
  - Create isolated ALC per plugin execution
  - Load plugin DLL + dependencies into ALC
  - Resolve `IPlugin` implementation via reflection
  - Support hot-unload (dispose ALC after execution)
- **Constraints:** No shared state between executions. Memory limit enforcement.

### Task 6.3: Capability Resolver

- **Project:** `src/Core/PluginRuntime.Runtime/`
- **Mô tả:**
  - Read declared capabilities from manifest
  - Create scoped capability instances (IDatabaseCapability, INetworkCapability, etc.)
  - Inject into PluginContext
  - Enforce: only declared capabilities accessible, fail-closed for undeclared
- **Pattern:** Factory per capability type, tenant-scoped

### Task 6.4: Execution Pipeline

- **Project:** `src/Core/PluginRuntime.Runtime/`
- **Mô tả:**
  - Pipeline stages: Validate → Load → Resolve Capabilities → Execute → Cleanup
  - CancellationToken propagation (timeout enforcement)
  - Resource governance: memory limit, CPU time, execution duration
  - Structured result: ExecutionResult (Success/Failed/Timeout/Cancelled)
- **Observability:** OpenTelemetry span per execution, structured log entry

### Task 6.5: Execute Endpoint

- **File:** `src/PluginRuntime.Api/Controllers/ExecuteController.cs`
- **Endpoint:** `POST /api/plugins/execute`
- **Flow:**
  1. Authenticate request (API key → tenant)
  2. Resolve extension by ID → get approved manifest
  3. Check tenant subscription/access
  4. Check quota/rate limit
  5. Execute via pipeline
  6. Record usage metric
  7. Return result
- **Fail-closed:** Any step fails → reject execution

### Task 6.6: Execution Recording

- **Mô tả:** After each execution, record:
  - Execution ID, tenant, extension, status, duration, timestamp
  - Increment daily usage counter
  - Emit event for monitoring (SignalR broadcast to Admin)
- **Storage:** JSON (dev) or PostgreSQL (prod)

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

## Thứ Tự Thực Hiện Đề Xuất

```
Sprint 1 (tuần này):
  Phase 4: Mock Data & Demo Mode (Task 4.1, 4.2, 4.3)
  Task 7.1: Dark/Light Theme

Sprint 2:
  Phase 5: Task 5.1–5.4 (Usage, Keys, Billing, Plans controllers)
  Task 7.3: Search Filters

Sprint 3:
  Phase 5: Task 5.5–5.8 (Subscriptions, Upload, Profile, Support)
  Task 7.2: Real-time Notifications

Sprint 4:
  Phase 6: Task 6.1–6.3 (Manifest Validator, Plugin Loader, Capability Resolver)

Sprint 5:
  Phase 6: Task 6.4–6.6 (Execution Pipeline, Execute Endpoint, Recording)
  Task 8.8: Rate Limiter

Sprint 6:
  Task 7.4: Reviews & Ratings
  Task 7.6: Export Data
  Task 8.4: CI/CD Pipeline

Sprint 7+:
  Task 8.1: PostgreSQL
  Task 8.2: Redis
  Task 7.7: i18n
  Task 7.8: 2FA
  Task 7.9: Webhooks
  Task 8.3: Sandbox Testing
  Task 8.5: Tenant Management
  Task 8.6: Revenue Sharing
  Task 8.7: Plugin Dependencies
  Task 7.10: API Playground
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
