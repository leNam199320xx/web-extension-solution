# Requirements Document

## Introduction

The Unified API Architecture restructures the Plugin Runtime ecosystem from separate deployable services into a Modular Monolith pattern. The previously separate Tenant Management & Billing system merges INTO PluginRuntime.Api as internal modules, creating a single deployable application that handles plugin execution, tenant management, billing, and subscriptions. The Public API Gateway remains a separate deployable (reverse proxy) but reads from the same PostgreSQL database that PluginRuntime.Api writes to.

This restructuring also introduces a Unified Subscription System where ONE set of plans (Free, Pro, Enterprise) applies to ALL users — both plugin developers (Marketplace users) and API consumers share the same plan tiers. Additionally, Tenants can subscribe to Plugin Packages (curated groups of plugins) for additional monthly fees, and internal platform services can be registered as Tenants with an "Internal" plan for unlimited access without billing.

Architecture after restructuring:
```
PluginRuntime.Api (Modular Monolith)
├── Modules/
│   ├── Plugins/        → Plugin management, execution, manifests, capabilities
│   ├── Tenants/        → Tenant registration, lifecycle, API keys
│   ├── Billing/        → Plans, invoices, Stripe, usage aggregation
│   ├── Subscriptions/  → Plan subscriptions + plugin package subscriptions
│   └── Gateway/        → Internal gateway support (tenant resolution, cache invalidation)
├── Shared/             → Shared kernel (entities, interfaces, domain events)
└── Program.cs

Public API Gateway (Separate Deployable — unchanged)
├── Reads tenant/key/plan data from same PostgreSQL
├── Redis pub/sub for cache invalidation
└── Reverse proxy to PluginRuntime.Api
```

## Glossary

- **Monolith**: PluginRuntime.Api — the single modular monolith ASP.NET Core application hosting all business modules
- **Module**: A self-contained vertical slice within the Monolith with its own Domain, Services, Controllers, and Data configuration
- **Shared_Kernel**: Cross-cutting entities, interfaces, and domain events shared between modules
- **Tenant**: An organizational entity representing any platform user account (plugin developer, API consumer, or internal service)
- **Plan**: A subscription tier (Free, Pro, Enterprise, Internal) defining rate limits, quotas, pricing, and feature access for ALL user types
- **Plugin_Package**: A curated group of plugins that Tenants can subscribe to for an additional monthly fee, granting API key access to invoke those plugins via the Gateway
- **Package_Subscription**: A Tenant-level subscription to a Plugin_Package, distinct from extension-to-extension subscriptions (which are developer-level, free, permission-based)
- **Internal_Tenant**: A Tenant representing an internal platform service, assigned the Internal plan with no billing and unlimited access
- **Domain_Event**: An in-process event used for inter-module communication within the Monolith (e.g., TenantCreated, PlanChanged, PackageSubscribed)
- **Gateway_Module**: The module within the Monolith that supports the external Public API Gateway by publishing cache invalidation events and managing tenant resolution data
- **EF_DbContext**: The shared Entity Framework Core DbContext; each module registers its own entity configurations but all share a single database connection
- **Redis_PubSub**: Redis publish/subscribe mechanism used to notify the external Public API Gateway of changes (plan updates, key revocations, package access changes)
- **Plugin_Developer**: A Tenant who uploads and manages plugins via the Marketplace Portal
- **API_Consumer**: A Tenant who invokes plugins via the Public API Gateway using API keys
- **Platform_Admin**: An internal user who manages tenants, plans, packages, and billing via the Admin Portal

## Requirements

### Requirement 1: Modular Monolith Structure

**User Story:** As a developer, I want PluginRuntime.Api organized as a modular monolith with clear module boundaries, so that the system is maintainable while running as a single deployable.

#### Acceptance Criteria

1. WHEN the solution is built, THE Monolith SHALL contain exactly five modules (Plugins, Tenants, Billing, Subscriptions, Gateway) each with its own Domain, Services, Controllers, and Data subdirectories
2. THE Monolith SHALL contain a Shared_Kernel directory exposing cross-cutting entities (Tenant, Plan, ApiKey), shared interfaces (ICurrentTenantContext, IDomainEventDispatcher), and domain event definitions accessible by all modules
3. WHEN modules communicate, THE Monolith SHALL use Domain_Events dispatched in-process via IDomainEventDispatcher for cross-module notifications (e.g., Tenants module publishes TenantCreated, Billing module subscribes to initialize Stripe customer)
4. THE Monolith SHALL NOT allow direct service-to-service calls between modules; inter-module communication SHALL occur exclusively through Domain_Events or Shared_Kernel interfaces
5. WHEN Entity Framework migrations are applied, THE EF_DbContext SHALL load entity configurations from all modules via IEntityTypeConfiguration implementations registered per module, producing a single unified database schema
6. WHEN Program.cs configures services, THE Monolith SHALL register each module via a dedicated extension method (e.g., services.AddPluginsModule(), services.AddTenantsModule()) that encapsulates the module's internal DI registrations
7. THE Monolith SHALL expose a single OpenAPI/Swagger document aggregating all module endpoints under their respective route prefixes

### Requirement 2: API Route Organization

**User Story:** As a developer, I want clear, consistent API route prefixes per module, so that endpoints are discoverable and the system is easy to navigate.

#### Acceptance Criteria

1. THE Monolith SHALL route requests to the Plugins module under the `/api/plugins` prefix for all plugin management and execution endpoints
2. THE Monolith SHALL route requests to the Tenants module under the `/api/tenants` prefix for tenant registration, lifecycle, and API key management endpoints
3. THE Monolith SHALL route requests to the Billing module under the `/api/billing` prefix for plan management, invoice retrieval, Stripe webhooks, and usage query endpoints
4. THE Monolith SHALL route requests to the Subscriptions module under the `/api/subscriptions` prefix for plan change, plugin package subscription, and subscription management endpoints
5. THE Monolith SHALL route requests to cross-module admin endpoints under the `/api/admin` prefix requiring Platform_Admin authentication
6. THE Monolith SHALL expose `/health`, `/ready`, and `/metrics` endpoints at the root level as shared infrastructure endpoints accessible without authentication

### Requirement 3: Unified Plan System

**User Story:** As a Platform Admin, I want a single set of subscription plans that apply to all Tenant types, so that billing and access control are consistent across plugin developers and API consumers.

#### Acceptance Criteria

1. THE Monolith SHALL support exactly four plan types: Free (rate_limit: 100/day, daily_quota: 100, max_api_keys: 2, max_plugins_upload: 3, max_package_subscriptions: 0, price: $0/month), Pro (rate_limit: 10000/day, daily_quota: 10000, max_api_keys: 10, max_plugins_upload: 20, max_package_subscriptions: 5, price: $49/month), Enterprise (rate_limit: unlimited, daily_quota: unlimited, max_api_keys: 50, max_plugins_upload: unlimited, max_package_subscriptions: unlimited, price: $299/month), Internal (rate_limit: unlimited, daily_quota: unlimited, max_api_keys: unlimited, max_plugins_upload: unlimited, max_package_subscriptions: unlimited, price: $0/month, no billing)
2. WHEN a Tenant registers, THE Monolith SHALL assign the Free plan regardless of whether the Tenant is a Plugin_Developer or API_Consumer
3. THE Monolith SHALL create exactly one Stripe customer per Tenant regardless of whether the Tenant uses the Marketplace Portal or API Consumer Portal
4. WHEN a Tenant changes plans, THE Monolith SHALL apply the new plan's limits to ALL activities (plugin uploads, API key creation, rate limits, quotas, and package subscriptions) within a single transaction
5. THE Monolith SHALL enforce max_plugins_upload limit in the Plugins module by querying the Tenant's current Plan from the Shared_Kernel
6. THE Monolith SHALL enforce max_package_subscriptions limit in the Subscriptions module by querying the Tenant's current Plan from the Shared_Kernel

### Requirement 4: Plugin Package Definition and Management

**User Story:** As a Platform Admin, I want to create and manage Plugin Packages (curated groups of plugins), so that Tenants can subscribe to premium plugin collections.

#### Acceptance Criteria

1. WHEN a Platform_Admin creates a Plugin_Package, THE Monolith SHALL persist the package with: name (1–200 characters), description (up to 2000 characters), monthly price (minimum $0.00), list of included plugin IDs, and active/inactive status
2. WHEN a Platform_Admin adds or removes plugins from a Plugin_Package, THE Monolith SHALL update the package composition and publish a Domain_Event (PackageCompositionChanged) so the Gateway_Module can update access permissions
3. IF a Plugin_Package creation or update contains a plugin ID that does not exist or is not in Active status, THEN THE Monolith SHALL reject the request with HTTP 400 and error code "UA-PKG-001"
4. IF a Platform_Admin deactivates a Plugin_Package that has active Package_Subscriptions, THEN THE Monolith SHALL set the package status to "inactive", preserve existing subscriptions until their next billing cycle, and prevent new subscriptions
5. WHEN a client requests the list of available Plugin_Packages, THE Monolith SHALL return only packages with status "active" with pagination (default page size: 20, maximum: 100)
6. THE Monolith SHALL allow a plugin to belong to multiple Plugin_Packages simultaneously

### Requirement 5: Plugin Package Subscription (Tenant-Level)

**User Story:** As a Tenant, I want to subscribe to Plugin Packages, so that my API keys gain access to invoke the plugins contained in those packages via the Gateway.

#### Acceptance Criteria

1. WHEN a Tenant subscribes to a Plugin_Package, THE Monolith SHALL create a Package_Subscription record with tenant_id, package_id, status "active", start date (current UTC), and Stripe subscription item for the package's monthly price
2. WHEN a Package_Subscription is activated, THE Monolith SHALL publish a Domain_Event (PackageSubscribed) and the Gateway_Module SHALL publish a Redis_PubSub notification so the Public API Gateway grants the Tenant's API keys access to invoke plugins in that package
3. IF a Tenant attempts to subscribe to a Plugin_Package and has reached their Plan's max_package_subscriptions limit, THEN THE Monolith SHALL reject the request with HTTP 403 and error code "UA-SUB-001"
4. IF a Tenant attempts to subscribe to a Plugin_Package they are already subscribed to, THEN THE Monolith SHALL reject the request with HTTP 409 and error code "UA-SUB-002"
5. WHEN a Tenant cancels a Package_Subscription, THE Monolith SHALL set the subscription status to "cancelled", cancel the corresponding Stripe subscription item, and publish a Domain_Event (PackageUnsubscribed) so the Gateway_Module revokes access at the end of the current billing cycle
6. WHEN the Public API Gateway receives a plugin invocation request, THE Gateway SHALL verify that the Tenant has an active Package_Subscription granting access to the target plugin OR that the plugin is marked as publicly accessible (free-tier plugins)
7. THE Monolith SHALL bill Plugin_Package subscriptions as separate line items on the Tenant's monthly Stripe invoice in addition to the base plan price

### Requirement 6: Internal Tenant Registration

**User Story:** As a Platform Admin, I want to register internal platform services as Tenants with unlimited access and no billing, so that internal services can consume plugins via the same Gateway.

#### Acceptance Criteria

1. WHEN a Platform_Admin registers an Internal_Tenant, THE Monolith SHALL create a Tenant record with the Internal plan assigned, status "active", and a metadata flag `is_internal: true`
2. THE Monolith SHALL NOT create a Stripe customer for Internal_Tenants; the stripe_customer_id field SHALL remain null
3. WHILE a Tenant is assigned the Internal plan, THE Monolith SHALL exempt that Tenant from all billing operations (no invoices generated, no usage-based charges, no overage calculations)
4. WHILE a Tenant is assigned the Internal plan, THE Public API Gateway SHALL enforce no rate limits and no daily quotas for that Tenant's API keys
5. WHEN a Platform_Admin requests a list of Tenants, THE Monolith SHALL support filtering by `is_internal` flag to distinguish internal services from external customers
6. THE Monolith SHALL record all Internal_Tenant API usage in usage_records for auditing purposes despite no billing being applied
7. IF a non-Platform_Admin attempts to register or modify an Internal_Tenant, THEN THE Monolith SHALL reject the request with HTTP 403 and error code "UA-INT-001"

### Requirement 7: Tenant Module (Merged from Tenant Management)

**User Story:** As a Tenant, I want to register, manage my account lifecycle, and generate API keys within the unified platform, so that I have a single account regardless of how I use the platform.

#### Acceptance Criteria

1. WHEN a valid registration request is received containing tenant name (1–200 characters), contact email (valid RFC 5322 format), and optional company name, THE Monolith SHALL create a new Tenant record with status "active", assign the Free plan, and publish a TenantCreated Domain_Event
2. WHEN a TenantCreated Domain_Event is received by the Billing module, THE Billing module SHALL create a Stripe customer and persist the stripe_customer_id association (unless is_internal is true)
3. WHEN a Platform_Admin suspends a Tenant, THE Monolith SHALL set the Tenant status to "suspended", publish a TenantStatusChanged Domain_Event, and the Gateway_Module SHALL publish a Redis_PubSub notification so the Public API Gateway rejects subsequent requests
4. WHEN a Tenant requests a new API key, THE Monolith SHALL generate a cryptographically random key of 64 characters, compute its SHA-256 hash, store the hash with prefix (first 8 characters) and suffix (last 4 characters), enforce the Plan's max_api_keys limit, and return the full plaintext key exactly once
5. WHEN an API key is revoked, THE Monolith SHALL set the key status to "revoked", record the revoked_at timestamp, and the Gateway_Module SHALL publish a Redis_PubSub cache-invalidation event
6. IF a Tenant has reached the maximum number of active API keys for their current Plan, THEN THE Monolith SHALL reject the request with HTTP 403 and error code "UA-KEY-001"
7. THE Monolith SHALL record all Tenant lifecycle state transitions in an immutable audit log containing tenant_id, previous status, new status, actor ID, timestamp (UTC), and reason

### Requirement 8: Billing Module (Merged from Billing System)

**User Story:** As a Platform Admin, I want unified billing that handles plan subscriptions, plugin package charges, and usage-based overage within a single Stripe integration, so that invoicing is consolidated per Tenant.

#### Acceptance Criteria

1. THE Billing module SHALL generate consolidated monthly invoices on the 1st of each month containing: base plan amount, overage charges (requests exceeding daily quota summed across the period), and all active Plugin_Package subscription fees as separate line items
2. WHEN a Tenant upgrades to a higher-tier plan, THE Billing module SHALL apply the new plan limits immediately and prorate the billing difference for the remainder of the current billing period via Stripe
3. WHEN a Tenant downgrades to a lower-tier plan, THE Billing module SHALL schedule the new plan to take effect at the start of the next billing period while continuing the current plan until then
4. WHEN a plan change takes effect, THE Billing module SHALL publish a PlanChanged Domain_Event and the Gateway_Module SHALL publish a Redis_PubSub notification containing tenant_id, new plan_id, new rate_limit, new daily_quota, and a monotonically increasing version number
5. THE Billing module SHALL aggregate Usage_Records from the usage_records table into daily Usage_Aggregates at 01:00 UTC each day for the previous UTC day's data
6. IF the Stripe API returns an error during any billing operation, THEN THE Billing module SHALL return HTTP 502 with error code "UA-BILL-001" and log the failure details without exposing Stripe internals to the client
7. WHEN a Stripe webhook is received, THE Billing module SHALL verify the webhook signature, process the event idempotently (deduplicate by stripe_event_id), and update invoice/subscription status accordingly

### Requirement 9: Gateway Module (Internal Gateway Support)

**User Story:** As a developer, I want an internal Gateway module that manages cache invalidation and access control data, so that the external Public API Gateway stays synchronized with the Monolith's state.

#### Acceptance Criteria

1. WHEN the Gateway_Module receives a PlanChanged, TenantStatusChanged, KeyRevoked, PackageSubscribed, or PackageUnsubscribed Domain_Event, THE Gateway_Module SHALL publish a corresponding Redis_PubSub notification to the appropriate channel within 1 second of event receipt
2. THE Gateway_Module SHALL maintain a `plugin_access` materialized view (or equivalent cached structure) mapping each Tenant to the set of plugin IDs they can access (free plugins + plugins from active Package_Subscriptions)
3. WHEN a PackageCompositionChanged Domain_Event is received, THE Gateway_Module SHALL recalculate the affected Tenants' plugin_access sets and publish a Redis_PubSub notification so the Gateway updates its access control cache
4. IF Redis_PubSub is unavailable when publishing a notification, THEN THE Gateway_Module SHALL retry publication 3 times with 5-second intervals and persist the failed event for later reconciliation if all retries fail
5. THE Gateway_Module SHALL include a monotonically increasing version number in all pub/sub notifications so the Public API Gateway can discard stale updates received out of order
6. WHEN the Public API Gateway performs API key validation, THE Gateway SHALL resolve the Tenant, Plan, and plugin_access set from the shared PostgreSQL database (with Redis caching) written by the Monolith

### Requirement 10: Subscription Module (Plan Changes + Package Subscriptions)

**User Story:** As a Tenant, I want a unified subscription experience where I manage my plan tier and plugin package subscriptions in one place, so that I can control my access and costs from a single module.

#### Acceptance Criteria

1. WHEN a Tenant requests a plan change via `/api/subscriptions/plan`, THE Subscriptions module SHALL validate the change, apply upgrade/downgrade rules, update Stripe, and publish a PlanChanged Domain_Event
2. WHEN a Tenant requests a plugin package subscription via `/api/subscriptions/packages/{packageId}/subscribe`, THE Subscriptions module SHALL validate the Package_Subscription limit, create the subscription, create a Stripe subscription item, and publish a PackageSubscribed Domain_Event
3. WHEN a Tenant requests package unsubscription via `/api/subscriptions/packages/{packageId}/unsubscribe`, THE Subscriptions module SHALL cancel at end of billing cycle, update Stripe, and publish a PackageUnsubscribed Domain_Event
4. WHEN a Tenant requests a list of their subscriptions via `/api/subscriptions`, THE Subscriptions module SHALL return the current plan details and all active/pending Package_Subscriptions with package names, prices, and start dates
5. IF a Tenant on the Free plan attempts to subscribe to a Plugin_Package, THEN THE Subscriptions module SHALL reject the request with HTTP 403 and error code "UA-SUB-003" because Free plan has max_package_subscriptions of 0
6. THE Subscriptions module SHALL validate that the target Plugin_Package exists and has status "active" before creating a Package_Subscription

### Requirement 11: Migration Compatibility

**User Story:** As a developer, I want the new modular monolith to maintain data and integration compatibility with the existing Public API Gateway, so that the migration is seamless.

#### Acceptance Criteria

1. THE Monolith SHALL write to the same PostgreSQL tables (tenants, plans, api_keys, usage_records, usage_aggregates, invoices, webhook_events, audit_log) that the Public API Gateway reads from, maintaining backward-compatible schema
2. THE Monolith SHALL publish Redis_PubSub messages on the same channels (`tenant:plan-changed`, `tenant:key-revoked`) with the same payload format that the Public API Gateway currently subscribes to
3. WHEN the Monolith adds new tables for Plugin_Packages and Package_Subscriptions, THE Monolith SHALL add a new Redis_PubSub channel `tenant:access-changed` for package access notifications without modifying existing channels
4. THE Monolith SHALL serve all endpoints that both the Marketplace Portal and API Consumer Portal currently call, maintaining backward-compatible request/response contracts
5. THE Monolith SHALL support the same JWT authentication mechanism (issuer, audience, signing keys) used by the existing Tenant Management & Billing API so that existing portal tokens remain valid
6. WHEN the Public API Gateway resolves tenant access for a plugin invocation, THE Gateway SHALL check both the plugin's public accessibility flag AND the Tenant's package_subscriptions to determine access

### Requirement 12: Security and Access Control

**User Story:** As a Platform Admin, I want the modular monolith secured under the Zero-Trust model with role-based access per module, so that tenant data and billing information are protected.

#### Acceptance Criteria

1. THE Monolith SHALL authenticate all API requests (except /health, /ready, /metrics, and Stripe webhook endpoint) using JWT bearer tokens validated against the platform identity provider with signature verification and expiration checks
2. THE Monolith SHALL enforce role-based access control: Platform_Admin role for /api/admin/* endpoints, Tenant-owner role for self-service endpoints (own tenant data, own subscriptions, own API keys)
3. IF a Tenant attempts to access another Tenant's data via any module endpoint, THEN THE Monolith SHALL reject the request with HTTP 403 and error code "UA-AUTH-001"
4. THE Monolith SHALL validate and sanitize all input parameters across all modules to prevent SQL injection, XSS, and other injection attacks
5. THE Monolith SHALL NOT log sensitive data (full API keys, Stripe secrets, payment card details) in any log output; sensitive fields SHALL be masked or omitted
6. THE Monolith SHALL enforce HTTPS (TLS 1.2 or higher) for all inbound connections
7. WHEN the Stripe webhook endpoint receives a request, THE Monolith SHALL verify the Stripe webhook signature before processing, rejecting requests with invalid signatures with HTTP 401

### Requirement 13: Health, Observability, and Metrics

**User Story:** As a Platform Admin, I want unified health checks, traces, and metrics from the modular monolith, so that I can monitor all modules from a single observability surface.

#### Acceptance Criteria

1. WHEN a client sends `GET /health` and all dependencies (PostgreSQL, Redis, Stripe API) respond within 5 seconds, THE Monolith SHALL return HTTP 200 with status "Healthy" and individual dependency statuses plus per-module health indicators
2. IF any dependency fails to respond within 5 seconds, THEN THE Monolith SHALL return HTTP 503 with status "Unhealthy" identifying the failing dependency and affected modules
3. THE Monolith SHALL emit OpenTelemetry traces for all API requests with spans identifying the target module, business operation, database calls, Redis operations, and Stripe API calls
4. THE Monolith SHALL emit structured JSON logs for every request containing: timestamp (ISO 8601), level, traceId, spanId, tenantId (when applicable), module name, method, path, statusCode, and durationMs
5. THE Monolith SHALL expose Prometheus-compatible metrics at `GET /metrics` including: total requests per module, error rates per module, Stripe API latency, plugin package subscription count, and background job execution status
6. THE Monolith SHALL include a `X-Module` response header indicating which module handled the request for debugging purposes

