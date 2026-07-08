# Implementation Plan: Unified API Architecture (Modular Monolith)

## Overview

This plan implements the restructuring of PluginRuntime.Api into a Modular Monolith with five modules (Plugins, Tenants, Billing, Subscriptions, Gateway), a Shared Kernel with domain events, unified plan system, plugin package subscriptions, and Redis pub/sub for Gateway sync. Implementation uses .NET 10, C#, ASP.NET Core, PostgreSQL, Redis, Stripe.net SDK, OpenTelemetry, and FsCheck + xUnit + Testcontainers.

## Tasks

- [x] 1. Set up project structure, Shared Kernel, and core infrastructure
  - [x] 1.1 Create solution structure with module directories and shared kernel
    - Create `src/PluginRuntime.Api/` directory with `Shared/`, `Modules/`, and `Middleware/` subdirectories
    - Create module subdirectories: `Modules/Plugins/`, `Modules/Tenants/`, `Modules/Billing/`, `Modules/Subscriptions/`, `Modules/Gateway/`
    - Each module gets `Domain/`, `Services/`, `Controllers/`, `Data/`, `EventHandlers/` subdirectories
    - Create `tests/PluginRuntime.Api.Tests/` with `Unit/`, `Integration/`, `Properties/`, and `Properties/Generators/`
    - Set up `.csproj` with package references: Npgsql.EntityFrameworkCore.PostgreSQL, StackExchange.Redis, Stripe.net, OpenTelemetry, FsCheck.Xunit, Testcontainers
    - _Requirements: 1.1, 1.2_

  - [x] 1.2 Implement Shared Kernel interfaces and domain event definitions
    - Create `IDomainEvent` marker interface with `EventId` and `OccurredAt`
    - Create `IDomainEventDispatcher` with `DispatchAsync<TEvent>` method
    - Create `IDomainEventHandler<TEvent>` with `HandleAsync` method
    - Create `ICurrentTenantContext` interface with TenantId, PlanId, IsInternal, IsAdmin
    - Define all 7 domain event records: TenantCreated, TenantStatusChanged, KeyRevoked, PlanChanged, PackageSubscribed, PackageUnsubscribed, PackageCompositionChanged
    - _Requirements: 1.2, 1.3_

  - [x] 1.3 Implement Shared Kernel entities and value objects
    - Create `Tenant` entity with domain methods (AssignPlan, Suspend, Reactivate), status enum, version counter
    - Create `Plan` entity with PlanType enum (Free, Pro, Enterprise, Internal) and all limit fields
    - Create `ApiKey` entity with KeyHash, KeyPrefix, KeySuffix, status, expiration
    - Create value objects: `Email` (RFC 5322 validation), `Money`, `KeyHash`
    - _Requirements: 1.2, 3.1, 7.1_

  - [x] 1.4 Implement DomainEventDispatcher and AppDbContext
    - Implement `DomainEventDispatcher` using IServiceProvider to resolve handlers per event type
    - Implement `AppDbContext` with DbSets for all shared entities
    - Override `OnModelCreating` to apply configurations from all module assemblies
    - Implement `CurrentTenantContext` resolving tenant from HttpContext
    - _Requirements: 1.3, 1.5_

  - [x] 1.5 Create Program.cs with module registration and middleware pipeline
    - Configure host builder with PostgreSQL, Redis, and OpenTelemetry
    - Register IDomainEventDispatcher as singleton
    - Add module registration extension method calls (AddPluginsModule, AddTenantsModule, etc.)
    - Configure middleware pipeline: GlobalExceptionMiddleware → JWT Auth → TenantContext
    - Map module endpoints and health/metrics endpoints
    - _Requirements: 1.6, 1.7, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [x] 1.6 Implement error handling infrastructure
    - Create `UnifiedApiException` base class with ErrorCode and HttpStatusCode
    - Create all typed exceptions: PackageValidationException, SubscriptionLimitException, DuplicateSubscriptionException, ApiKeyLimitException, TenantIsolationException, InternalTenantAuthException, BillingProviderException
    - Implement `GlobalExceptionMiddleware` mapping exceptions to standardized JSON error responses
    - _Requirements: 12.4_

  - [ ]* 1.7 Write property test for domain event dispatch completeness
    - **Property 1: Domain event dispatch completeness**
    - Test that all registered handlers for an event type receive the exact payload and no unrelated handlers are invoked
    - **Validates: Requirements 1.3**

- [x] 2. Implement Tenants Module
  - [x] 2.1 Implement Tenant registration service and controller
    - Create `ITenantService` and `TenantService` with RegisterAsync, RegisterInternalAsync, SuspendAsync, ReactivateAsync, GetByIdAsync, ListAsync
    - Validate name (1–200 chars), email (RFC 5322), assign Free plan on registration
    - Dispatch TenantCreated domain event on successful registration
    - Create `TenantsController` at `/api/tenants` with registration, lifecycle, and listing endpoints
    - _Requirements: 7.1, 7.3, 3.2_

  - [x] 2.2 Implement API Key service
    - Create `IApiKeyService` and `ApiKeyService` with GenerateAsync, RevokeAsync, ListAsync
    - Generate 64-character cryptographically random key, compute SHA-256 hash, store hash/prefix/suffix
    - Enforce plan's max_api_keys limit, reject with UA-KEY-001 when exceeded
    - Return plaintext key exactly once; dispatch KeyRevoked event on revocation
    - Create `ApiKeysController` at `/api/tenants/{id}/keys`
    - _Requirements: 7.4, 7.5, 7.6, 3.5_

  - [x] 2.3 Implement Internal Tenant registration
    - Add RegisterInternalAsync to TenantService: assign Internal plan, set is_internal=true, skip Stripe
    - Enforce Platform_Admin authorization, reject with UA-INT-001 for non-admins
    - Support filtering by is_internal in ListAsync
    - _Requirements: 6.1, 6.2, 6.5, 6.7_

  - [x] 2.4 Implement audit logging for tenant lifecycle
    - Create immutable audit_log table/entity with tenant_id, previous_status, new_status, actor_id, timestamp, reason
    - Record all status transitions (active→suspended, suspended→active) and plan changes
    - Dispatch TenantStatusChanged domain event on status changes
    - _Requirements: 7.7_

  - [x] 2.5 Implement TenantEntityConfiguration for EF Core
    - Configure Tenant, ApiKey entities with proper column mappings, indexes, constraints
    - Configure audit_log table mapping
    - Register configurations via TenantsModuleExtensions.AddTenantsModule()
    - _Requirements: 1.5_

  - [ ]* 2.6 Write property tests for Tenant registration and API key limits
    - **Property 2: Tenant registration invariants** — valid registration produces Active tenant with Free plan and TenantCreated event
    - **Property 5: API key limit enforcement** — key generation succeeds iff N < M, key is 64 chars, hash matches SHA-256
    - **Validates: Requirements 3.2, 7.1, 3.5, 7.4, 7.6**

  - [ ]* 2.7 Write property tests for Internal Tenant and tenant isolation
    - **Property 16: Internal tenant authorization** — non-admin requests rejected with UA-INT-001
    - **Property 22: Tenant data isolation** — cross-tenant access rejected with UA-AUTH-001
    - **Validates: Requirements 6.7, 12.3**

- [~] 3. Checkpoint - Ensure Shared Kernel and Tenants Module tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 4. Implement Billing Module
  - [x] 4.1 Implement Stripe service integration
    - Create `IStripeService` and `StripeService` wrapping Stripe.net SDK
    - Implement CreateCustomerAsync, CreateSubscriptionAsync, UpdateSubscriptionAsync, CancelSubscriptionItemAsync, AddSubscriptionItemAsync
    - Implement VerifyWebhookSignature for webhook validation
    - Wrap all Stripe calls with error handling: catch StripeException → throw BillingProviderException (UA-BILL-001)
    - _Requirements: 8.6, 12.7_

  - [x] 4.2 Implement Billing service and TenantCreated event handler
    - Create `IBillingService` and `BillingService` with CreateStripeCustomerAsync, GetCurrentInvoiceAsync, ListInvoicesAsync
    - Create `TenantCreatedHandler` implementing IDomainEventHandler<TenantCreated>: create Stripe customer if not internal
    - Persist stripe_customer_id association on tenant record
    - _Requirements: 7.2, 3.3, 6.2_

  - [x] 4.3 Implement Invoice service and monthly invoice generation
    - Create `IInvoiceService` and `InvoiceService`
    - Implement consolidated monthly invoice: base plan amount + overage charges + package subscription fees as separate line items
    - Create `InvoiceGenerationService` (BackgroundService) running on 1st of each month
    - Calculate overage from usage exceeding daily_quota across billing period
    - _Requirements: 8.1, 5.7_

  - [~] 4.4 Implement Usage Aggregation background service
    - Create `UsageAggregationService` (BackgroundService) running daily at 01:00 UTC
    - Aggregate usage_records into daily UsageAggregate: total_requests, successful_requests (2xx), failed_requests (4xx+)
    - Calculate avg_duration_ms per tenant per day
    - _Requirements: 8.5_

  - [~] 4.5 Implement Stripe webhook endpoint and idempotent processing
    - Create `WebhooksController` at `/api/billing/webhooks/stripe`
    - Verify webhook signature before processing
    - Deduplicate by stripe_event_id using webhook_events table
    - Process invoice.paid, invoice.payment_failed, subscription.updated events
    - Return HTTP 200 for duplicate events (idempotent)
    - _Requirements: 8.7, 12.7_

  - [~] 4.6 Implement BillingController and BillingEntityConfiguration
    - Create `BillingController` at `/api/billing` with invoice listing, current invoice, usage query endpoints
    - Configure Invoice, UsageAggregate, WebhookEvent entity mappings
    - Register via BillingModuleExtensions.AddBillingModule()
    - _Requirements: 2.3, 1.5_

  - [ ]* 4.7 Write property tests for Billing module
    - **Property 3: Stripe customer creation rule** — TenantCreated with is_internal=false creates Stripe customer; is_internal=true does not
    - **Property 14: Invoice consolidation correctness** — total = base + overage + Σ(package prices)
    - **Property 15: Internal tenant billing exemption** — Internal plan generates no invoices, no overage, no rate limits
    - **Property 18: Usage aggregation correctness** — aggregate counts match record counts, successful + failed <= total
    - **Property 19: Webhook idempotent processing** — duplicate webhook produces no state change, returns 200
    - **Validates: Requirements 3.3, 6.2, 7.2, 5.7, 8.1, 6.3, 6.4, 6.6, 8.5, 8.7**

- [ ] 5. Implement Subscriptions Module
  - [x] 5.1 Implement Plugin Package service (admin CRUD)
    - Create `IPluginPackageService` and `PluginPackageService` with CreateAsync, UpdateAsync, DeactivateAsync, ListActiveAsync
    - Validate all plugin IDs exist and are Active; reject with UA-PKG-001 if any invalid
    - On composition change, dispatch PackageCompositionChanged domain event
    - On deactivation: set status to inactive, preserve existing subscriptions, prevent new ones
    - Enforce pagination (default 20, max 100) on list; return only active packages
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [x] 5.2 Implement Plan Subscription service (upgrade/downgrade)
    - Create `IPlanSubscriptionService` and `PlanSubscriptionService` with ChangePlanAsync, GetCurrentAsync
    - Implement upgrade logic: apply immediately with Stripe proration, update tenant plan, dispatch PlanChanged event
    - Implement downgrade logic: schedule for next billing period, set pending_plan_id, notify Stripe atPeriodEnd
    - Enforce plan limit validation (cannot downgrade if active resources exceed new limits → UA-PLAN-002)
    - _Requirements: 8.2, 8.3, 8.4, 10.1, 3.4_

  - [x] 5.3 Implement Package Subscription service
    - Create `IPackageSubscriptionService` and `PackageSubscriptionService` with SubscribeAsync, UnsubscribeAsync, ListActiveAsync
    - Enforce max_package_subscriptions limit from tenant's plan; reject with UA-SUB-001 when exceeded
    - Reject Free plan subscriptions with UA-SUB-003 (max_package_subscriptions = 0)
    - Reject duplicate subscriptions with UA-SUB-002 (HTTP 409)
    - On subscribe: create PackageSubscription, create Stripe subscription item, dispatch PackageSubscribed event
    - On unsubscribe: cancel at period end, dispatch PackageUnsubscribed event
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 10.2, 10.3, 10.5, 10.6_

  - [~] 5.4 Implement Subscription controllers and entity configuration
    - Create `PlanSubscriptionController` at `/api/subscriptions/plan`
    - Create `PackageSubscriptionController` at `/api/subscriptions/packages/{packageId}/subscribe`, `/unsubscribe`
    - Add `GET /api/subscriptions` returning current plan + all active package subscriptions
    - Configure PluginPackage, PackagePlugin, PackageSubscription, PluginAccess entity mappings with indexes and constraints
    - Register via SubscriptionsModuleExtensions.AddSubscriptionsModule()
    - _Requirements: 2.4, 10.4, 1.5_

  - [ ]* 5.5 Write property tests for Subscriptions module
    - **Property 4: Plan change atomicity and direction** — upgrade applies immediately, downgrade schedules for next period
    - **Property 6: Package subscription limit enforcement** — subscribe succeeds iff N < M; Free plan always rejected
    - **Property 7: Plugin package validation** — invalid/inactive plugin IDs reject with UA-PKG-001
    - **Property 8: Package deactivation preserves subscriptions** — existing subscriptions unchanged, new ones blocked
    - **Property 9: Active package listing filter** — only active packages returned
    - **Property 10: Package subscription creates correct state** — correct fields, Stripe item created
    - **Property 11: Duplicate subscription prevention** — duplicate rejected with UA-SUB-002
    - **Property 13: Package subscription cancellation** — sets cancelled, cancels Stripe, dispatches event
    - **Validates: Requirements 3.4, 8.2, 8.3, 3.6, 5.3, 10.5, 4.3, 4.4, 4.5, 5.1, 5.4, 5.5, 10.2, 10.3**

- [~] 6. Checkpoint - Ensure Billing and Subscriptions Module tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 7. Implement Gateway Module
  - [~] 7.1 Implement Plugin Access Resolver
    - Create `IPluginAccessResolver` and `PluginAccessResolver` with GetAccessiblePluginsAsync, RecalculateAccessAsync, RecalculateForPackageAsync
    - Implement access resolution algorithm: union of free plugins (is_publicly_accessible=true) and package subscription plugins
    - UPSERT into plugin_access table with source (Free/Package) and package_id
    - _Requirements: 5.6, 9.2, 11.6_

  - [~] 7.2 Implement Gateway Notification service (Redis pub/sub)
    - Create `IGatewayNotificationService` and `GatewayNotificationService`
    - Implement PublishPlanChangedAsync → channel `tenant:plan-changed`
    - Implement PublishTenantStatusChangedAsync → channel `tenant:status-changed`
    - Implement PublishKeyRevokedAsync → channel `tenant:key-revoked`
    - Implement PublishAccessChangedAsync → channel `tenant:access-changed`
    - Include monotonically increasing version in all payloads
    - Implement retry logic: 3 retries with 5-second intervals; persist failed events on exhaustion
    - _Requirements: 9.1, 9.4, 9.5, 11.2, 11.3_

  - [~] 7.3 Implement Gateway event handlers
    - Create `PlanChangedHandler`: publish Redis notification with tenantId, planId, rateLimit, dailyQuota, version
    - Create `TenantStatusChangedHandler`: publish Redis notification with tenantId, status, version
    - Create `KeyRevokedHandler`: publish Redis notification with tenantId, keyId, keyHash, version
    - Create `PackageSubscribedHandler`: recalculate plugin_access, publish access-changed notification
    - Create `PackageUnsubscribedHandler`: recalculate plugin_access, publish access-changed notification
    - Create `PackageCompositionChangedHandler`: recalculate access for all affected tenants, publish notifications
    - _Requirements: 9.1, 9.3, 8.4_

  - [~] 7.4 Implement GatewayEntityConfiguration and module registration
    - Configure plugin_access table mapping with composite primary key (tenant_id, plugin_id)
    - Configure failed_notifications table for retry persistence
    - Register via GatewayModuleExtensions.AddGatewayModule()
    - _Requirements: 1.5, 9.2_

  - [ ]* 7.5 Write property tests for Gateway module
    - **Property 12: Plugin access resolution correctness** — access granted iff public OR subscribed package contains plugin
    - **Property 17: Tenant status change propagation** — status change produces event, Redis notification, and audit log
    - **Property 20: Package composition change propagation** — affected tenants get recalculated access and Redis notification
    - **Property 21: Monotonic version ordering** — version strictly increases per tenant across notifications
    - **Property 23: Redis notification payload compatibility** — payload matches expected JSON schema
    - **Validates: Requirements 5.6, 9.2, 11.6, 7.3, 7.5, 4.2, 9.3, 9.5, 11.2**

- [ ] 8. Implement Plugins Module integration and Security Middleware
  - [~] 8.1 Implement Plugins Module extension and route registration
    - Create `PluginsModuleExtensions` with AddPluginsModule() and MapPluginsEndpoints()
    - Create `PluginsController` at `/api/plugins` for plugin CRUD, execution, manifests, capabilities
    - Enforce max_plugins_upload limit from tenant's plan via Shared Kernel
    - _Requirements: 2.1, 3.5, 1.6_

  - [~] 8.2 Implement JWT Authentication and TenantContext middleware
    - Create `JwtAuthenticationMiddleware`: validate JWT bearer tokens (signature, expiration, issuer, audience)
    - Skip auth for /health, /ready, /metrics, and Stripe webhook endpoint
    - Create `TenantContextMiddleware`: resolve tenant from JWT claims, populate ICurrentTenantContext
    - Enforce role-based access: Platform_Admin for /api/admin/*, tenant-owner for self-service endpoints
    - Reject cross-tenant access with UA-AUTH-001
    - _Requirements: 12.1, 12.2, 12.3, 11.5_

  - [~] 8.3 Implement Admin endpoints
    - Create admin controller at `/api/admin` for cross-module platform admin operations
    - Include tenant listing with is_internal filter, package management, plan management
    - Require Platform_Admin role authentication
    - _Requirements: 2.5, 6.5, 6.7_

  - [~] 8.4 Implement input validation and security hardening
    - Add input validation/sanitization on all module endpoints (prevent SQL injection, XSS)
    - Ensure sensitive data masking in logs (API keys, Stripe secrets, payment details)
    - Enforce HTTPS (TLS 1.2+) configuration
    - Add X-Module response header indicating which module handled the request
    - _Requirements: 12.4, 12.5, 12.6, 13.6_

- [~] 9. Checkpoint - Ensure Gateway, Plugins, and Security tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 10. Implement Health, Observability, and Database Migrations
  - [~] 10.1 Implement health checks and readiness endpoints
    - Create `/health` endpoint checking PostgreSQL, Redis, and Stripe API (5-second timeout)
    - Return HTTP 200 with "Healthy" status and per-dependency + per-module health indicators
    - Return HTTP 503 with "Unhealthy" identifying failing dependency on timeout
    - Create `/ready` endpoint for Kubernetes readiness probe
    - _Requirements: 13.1, 13.2, 2.6_

  - [~] 10.2 Implement OpenTelemetry tracing and structured logging
    - Configure OpenTelemetry with spans for API requests identifying module, operation, DB calls, Redis ops, Stripe calls
    - Configure structured JSON logging: timestamp (ISO 8601), level, traceId, spanId, tenantId, module, method, path, statusCode, durationMs
    - Ensure no sensitive data in traces/logs
    - _Requirements: 13.3, 13.4_

  - [~] 10.3 Implement Prometheus metrics endpoint
    - Create `/metrics` endpoint with Prometheus-compatible format
    - Track: total requests per module, error rates per module, Stripe API latency, package subscription count, background job status
    - _Requirements: 13.5, 2.6_

  - [~] 10.4 Create EF Core database migrations
    - Create initial migration with all tables: tenants (extended), plans, api_keys, plugin_packages, package_plugins, package_subscriptions, plugin_access, invoices, usage_aggregates, webhook_events, audit_log
    - Include plan seed data (Free, Pro, Enterprise, Internal)
    - Add is_publicly_accessible column to existing plugins table
    - Ensure backward compatibility with existing Public API Gateway read patterns
    - _Requirements: 11.1, 11.4, 1.5_

  - [ ]* 10.5 Write integration tests with Testcontainers
    - Test full module registration via WebApplicationFactory<Program>
    - Test database migrations apply cleanly with Testcontainers PostgreSQL
    - Test Redis pub/sub notification flow with Testcontainers Redis
    - Test cross-module event flow end-to-end (registration → Stripe customer → plan change → access update)
    - Mock Stripe API via WireMock.Net
    - _Requirements: 1.1, 1.3, 9.1, 11.1_

- [ ] 11. Wire modules together and implement cross-cutting flows
  - [~] 11.1 Wire tenant registration end-to-end flow
    - Connect TenantService → TenantCreated event → BillingService.CreateStripeCustomerAsync
    - Verify Free plan assignment → plan limits active immediately
    - Test internal tenant path: no Stripe customer, Internal plan, unlimited access
    - _Requirements: 7.1, 7.2, 3.2, 3.3, 6.1, 6.2_

  - [~] 11.2 Wire plan change end-to-end flow
    - Connect PlanSubscriptionService → PlanChanged event → GatewayModule → Redis pub/sub
    - Verify upgrade immediate application with proration
    - Verify downgrade scheduling with pending_plan_id
    - Ensure version increment and monotonic ordering
    - _Requirements: 8.2, 8.3, 8.4, 9.1, 9.5_

  - [~] 11.3 Wire package subscription end-to-end flow
    - Connect PackageSubscriptionService → PackageSubscribed event → GatewayModule → AccessResolver → Redis pub/sub
    - Verify plugin_access table updated with correct plugin set
    - Verify unsubscription → PackageUnsubscribed → access revocation at period end
    - Verify package composition change → recalculation for all affected tenants
    - _Requirements: 5.1, 5.2, 5.5, 5.6, 4.2, 9.3_

  - [~] 11.4 Wire API key revocation end-to-end flow
    - Connect ApiKeyService.RevokeAsync → KeyRevoked event → GatewayModule → Redis pub/sub
    - Verify Redis message on `tenant:key-revoked` channel with keyId, keyHash, version
    - _Requirements: 7.5, 9.1_

  - [~] 11.5 Implement OpenAPI/Swagger document aggregation
    - Configure Swashbuckle/NSwag to aggregate all module endpoints into single OpenAPI document
    - Ensure route prefixes are correctly grouped per module
    - _Requirements: 1.7_

- [~] 12. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation at module boundaries
- Property tests validate universal correctness properties from the design document (23 properties across 5 test task groups)
- Unit tests validate specific examples and edge cases within each module
- Integration tests use Testcontainers (PostgreSQL, Redis) and WireMock.Net (Stripe)
- All code uses C# / .NET 10 with async/await, CancellationToken propagation, and built-in DI
- FsCheck with xUnit integration is used for property-based testing
- The Plugins module (task 8.1) integrates with the existing plugin management code; focus is on wiring the module boundary and enforcing plan limits

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3"] },
    { "id": 2, "tasks": ["1.4", "1.6"] },
    { "id": 3, "tasks": ["1.5", "1.7"] },
    { "id": 4, "tasks": ["2.1", "4.1"] },
    { "id": 5, "tasks": ["2.2", "2.3", "4.2", "5.1"] },
    { "id": 6, "tasks": ["2.4", "2.5", "4.3", "4.4", "5.2", "5.3"] },
    { "id": 7, "tasks": ["2.6", "2.7", "4.5", "4.6", "5.4"] },
    { "id": 8, "tasks": ["4.7", "5.5"] },
    { "id": 9, "tasks": ["7.1", "7.2"] },
    { "id": 10, "tasks": ["7.3", "7.4"] },
    { "id": 11, "tasks": ["7.5", "8.1", "8.2"] },
    { "id": 12, "tasks": ["8.3", "8.4"] },
    { "id": 13, "tasks": ["10.1", "10.2", "10.3"] },
    { "id": 14, "tasks": ["10.4", "10.5"] },
    { "id": 15, "tasks": ["11.1", "11.2", "11.3", "11.4"] },
    { "id": 16, "tasks": ["11.5"] }
  ]
}
```
