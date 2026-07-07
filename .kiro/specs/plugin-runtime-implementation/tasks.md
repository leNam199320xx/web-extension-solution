# Implementation Plan: Plugin Runtime Implementation

## Overview

Implement the Metadata-Driven Secure Plugin Runtime on .NET 10 following a phased approach. Each phase builds on the previous, producing compilable, testable code. The implementation uses C#, ASP.NET Core, PostgreSQL, Redis, and AssemblyLoadContext for plugin isolation with a Zero-Trust security model.

## Tasks

- [ ] 1. Foundation - Solution Structure & Core Domain
  - [ ] 1.1 Create .NET solution file and all 14 source projects with correct dependency references
    - Create `PluginRuntime.sln` in `src/` directory
    - Create projects: Core, Runtime, Security, Infrastructure, Infrastructure.KeyVault, Api, Admin, Capabilities.Abstractions, Capabilities.Database, Capabilities.Network, Capabilities.Storage, Capabilities.Cache, Capabilities.Extension, Sdk
    - Configure dependency flow: SDK (zero deps) → Core (zero external NuGet) → Capabilities.Abstractions → Security/Runtime → Infrastructure → Api
    - All projects target `net10.0` with `TreatWarningsAsErrors` enabled
    - _Requirements: 1.1, 1.5_

  - [ ] 1.2 Create all 7 test projects with references to their corresponding source projects
    - Create: Core.Tests, Runtime.Tests, Security.Tests, Api.Tests, Infrastructure.Tests, IntegrationTests, Admin.Tests
    - Reference xUnit, FluentAssertions, NSubstitute, FsCheck (for PBT)
    - _Requirements: 1.7_

  - [ ] 1.3 Implement Core domain entities, value objects, and enums
    - Create `Plugin`, `PluginVersion`, `Manifest`, `Execution` entities in `PluginRuntime.Core.Entities`
    - Create `ResourceLimits`, `ValidationResult`, `ValidationError`, `VerificationResult` in `PluginRuntime.Core.ValueObjects`
    - Create all enums (`PluginStatus`, `PluginVersionStatus`, `ExecutionStatus`, `ActorType`, `AuditResult`, `Visibility`, `ApprovalDecision`, `RiskLevel`, `SignatureAlgorithm`, `SubscriptionStatus`) in `PluginRuntime.Core.Enums`
    - Implement validation logic on entity construction (reject null/empty required fields)
    - _Requirements: 1.2_

  - [ ] 1.4 Define Core interfaces
    - Create `IPluginExecutor`, `IManifestValidator`, `ISignatureVerifier`, `IHashVerifier`, `IPluginLoader`, `IExecutionPipeline`, `ICapabilityResolver`, `IExecutionGovernor`, `IRevocationChecker` in `PluginRuntime.Core.Interfaces`
    - Ensure all async methods accept `CancellationToken`
    - _Requirements: 1.2_

  - [ ] 1.5 Implement PluginRuntime.Sdk public types
    - Create `IPlugin` interface with `ExecuteAsync(PluginContext, CancellationToken)` method
    - Create `PluginContext` record with ExecutionId, PluginId, Version, Input, Capabilities, CorrelationId
    - Create `PluginResult` record with Success, Data, ErrorCode, ErrorMessage
    - Verify zero project references and zero external NuGet dependencies
    - _Requirements: 1.3_

  - [ ] 1.6 Implement Capabilities.Abstractions interfaces and record types
    - Create `ICapability` base interface with `Name` and `Version` properties
    - Create `IDatabaseCapability`, `INetworkCapability`, `IStorageCapability`, `ICacheCapability`, `IExtensionCapability` with method signatures per docs/implementation/capability-interfaces.md
    - Create associated records: `NetworkRequest`, `NetworkResponse`, `StorageMetadata`, `ExtensionInvocationResult`
    - _Requirements: 1.4_

  - [ ]* 1.7 Write unit tests for Core domain entities
    - Test each entity can be instantiated with valid data
    - Test each entity rejects invalid data (null/empty required fields)
    - Test value object equality and immutability
    - Test enum value coverage
    - _Requirements: 1.6_

  - [ ] 1.8 Verify solution builds with zero warnings
    - Run `dotnet build` on entire solution
    - Confirm all projects compile successfully targeting net10.0
    - Confirm TreatWarningsAsErrors produces zero warnings
    - _Requirements: 1.5_

- [ ] 2. Checkpoint - Foundation complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 3. Security Engine - Manifest Validation & Cryptographic Verification
  - [ ] 3.1 Implement ManifestValidator
    - Validate schema compliance: all required fields present and valid types
    - Validate required fields: plugin_id, version, permissions, capabilities, signature, public_key_id, issued_at, expires_at
    - Validate version compatibility with target_core_version
    - Validate expiration: expires_at > DateTime.UtcNow
    - Return structured `ValidationResult` with specific `ValidationError` per failure
    - _Requirements: 2.1_

  - [ ] 3.2 Implement HashVerifier
    - Compute SHA-256 of DLL bytes using `System.Security.Cryptography.SHA256`
    - Compare computed hash against manifest's sha256 field
    - Return `VerificationResult` with error code on mismatch
    - _Requirements: 2.2_

  - [ ] 3.3 Implement SignatureVerifier and IKeyProvider
    - Support RSA-SHA256 (default) and ECDSA-SHA256 algorithms
    - Load public key by `public_key_id` from `IKeyProvider`
    - Verify digital signature over canonical manifest content
    - Return `VerificationResult` with specific error code if signature invalid or key not found
    - _Requirements: 2.3_

  - [ ] 3.4 Implement RevocationChecker with Redis cache
    - Query Redis cache first for revocation status
    - Fall back to database query if cache miss
    - Expired revocations (expires_at < now) do NOT block execution
    - Cache revocation results with configurable TTL
    - _Requirements: 2.4_

  - [ ] 3.5 Implement security audit logging on validation failure
    - On any validation failure: fail closed, stop immediately
    - Log immutable audit_logs entry with TraceId, PluginId, failure reason, actor, timestamp
    - Return structured error response with category "Security"
    - _Requirements: 2.5_

  - [ ]* 3.6 Write property-based tests for ManifestValidator
    - **Property 1: Valid manifests always pass validation**
    - **Property 2: Any single tampered field causes validation failure**
    - **Property 3: Expired manifests (expires_at < now) always fail**
    - Over at least 100 randomized inputs
    - **Validates: Requirements 2.7**

  - [ ]* 3.7 Write property-based tests for SignatureVerifier and HashVerifier
    - **Property 4: Invalid signatures always fail verification**
    - **Property 5: Tampered DLL bytes always fail hash verification**
    - Over at least 100 randomized inputs
    - **Validates: Requirements 2.7**

  - [ ]* 3.8 Write property-based tests for RevocationChecker
    - **Property 6: Revoked plugin versions always fail**
    - **Property 7: Expired revocations do not block execution**
    - Over at least 100 randomized inputs
    - **Validates: Requirements 2.7**

- [ ] 4. Checkpoint - Security Engine complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Runtime Engine - Execution Pipeline & Plugin Loader
  - [ ] 5.1 Implement ExecutionPipeline with 7 sequential stages
    - Process stages in fixed order: ManifestValidator → SignatureVerifier → HashVerifier → CapabilityResolver → PluginLoader → PluginExecutor → ObservabilityCollector
    - On any stage failure: short-circuit immediately (fail closed)
    - Return structured error with failing stage name, error code, and TraceId
    - Subsequent stages SHALL NOT execute after failure
    - _Requirements: 3.1, 3.2_

  - [ ] 5.2 Implement PluginLoader with AssemblyLoadContext isolation
    - Create collectible `AssemblyLoadContext` per plugin
    - Resolve entry point class implementing `IPlugin` from SDK
    - Return plugin instance; no shared mutable state between loaded plugins
    - On resolution failure: return structured error, leave no residual ALC state
    - Track loaded ALCs for unload/hot-reload
    - _Requirements: 3.3, 3.4_

  - [ ] 5.3 Implement ExecutionGovernor for resource limit enforcement
    - Enforce execution timeout via `CancellationTokenSource` with `ResourceLimits.TimeoutMs`; CancellationToken serves as the cooperative enforcement mechanism that plugins must observe
    - Cancel the CancellationToken when timeout expires
    - Monitor memory usage against `ResourceLimits.MaxMemoryMb`; cancel CancellationToken when exceeded
    - Apply cooperative CPU cancellation against `ResourceLimits.MaxCpuMs`; cancel CancellationToken when exceeded
    - Terminate within 100ms of timeout expiry
    - Terminate within 1 second of memory limit detection
    - _Requirements: 3.5, 3.6, 3.7_

  - [ ] 5.4 Implement HotReloadManager for version transitions
    - Stop new requests to old version
    - Drain active executions (max 30 seconds drain timeout)
    - Force-cancel via CancellationToken if drain timeout exceeded
    - Unload old ALC
    - Load new version and warm-up
    - Resume traffic with zero request interruption
    - _Requirements: 3.8, 3.9_

  - [ ]* 5.5 Write unit tests for ExecutionPipeline
    - Test pipeline stages execute in correct order
    - Test failures short-circuit with structured error
    - Test all 7 stages are invoked on successful execution
    - _Requirements: 3.11_

  - [ ]* 5.6 Write unit tests for ExecutionGovernor
    - Test timeout enforcement terminates within bounds
    - Test memory limit enforcement terminates execution
    - Test CancellationToken propagation
    - _Requirements: 3.11_

  - [ ]* 5.7 Write unit tests for PluginLoader and HotReloadManager
    - Test ALC isolation prevents cross-plugin state sharing
    - Test hot-reload completes drain or force-cancels
    - Test resolution failure leaves no residual state
    - _Requirements: 3.11_

- [ ] 6. Checkpoint - Runtime Engine complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 7. Capability Layer - Infrastructure Access Control
  - [ ] 7.1 Implement CapabilityResolver
    - Return ONLY capabilities explicitly granted in manifest
    - Return dictionary keyed by capability name
    - Deny-by-default: undeclared capability → deny immediately
    - Log capability denial as security event in audit_logs
    - Return error with category "Security" on denial
    - _Requirements: 4.5, 4.6_

  - [ ] 7.2 Implement DatabaseCapability
    - Reject non-parameterized SQL (detect string interpolation markers)
    - Scope data access to plugin's isolated schema (prefix table references with plugin namespace)
    - Handle connection management via connection pooling transparently
    - Propagate CancellationToken through entire call chain
    - _Requirements: 4.1, 4.7_

  - [ ] 7.3 Implement NetworkCapability
    - Proxy HTTP calls only to domains in manifest's `allowed_domains`
    - Enforce 10 MB maximum response size per request
    - Enforce timeout from `NetworkRequest.TimeoutMs`
    - Block any URL not matching approved patterns
    - Propagate CancellationToken
    - _Requirements: 4.2, 4.7_

  - [ ] 7.4 Implement StorageCapability
    - Scope all storage keys to `{pluginId}/{key}` namespace
    - Enforce 50 MB per-object maximum size
    - Enforce configurable per-plugin total storage quota
    - Reject path traversal sequences (`../`, `..\`)
    - Propagate CancellationToken
    - _Requirements: 4.3, 4.7_

  - [ ] 7.5 Implement CacheCapability
    - Namespace all cache keys as `{pluginId}:{key}`
    - Enforce configurable maximum key count per plugin (default: 10000)
    - Enforce 1 MB maximum value size after serialization
    - Use `System.Text.Json` for serialization/deserialization
    - Propagate CancellationToken
    - _Requirements: 4.4, 4.7_

  - [ ]* 7.6 Write property-based tests for capability namespace isolation
    - **Property 8: Namespace isolation is never violated (no cross-plugin data access)**
    - **Property 9: Undeclared capabilities are always denied**
    - Over at least 100 randomized inputs
    - **Validates: Requirements 4.8**

  - [ ]* 7.7 Write property-based tests for capability size limits and path traversal
    - **Property 10: Size limits are always enforced**
    - **Property 11: Path traversal is always blocked**
    - Over at least 100 randomized inputs
    - **Validates: Requirements 4.8**

- [ ] 8. Checkpoint - Capability Layer complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. API Layer - HTTP Gateway & Controllers
  - [ ] 9.1 Configure API startup with middleware pipeline
    - Configure JWT authentication middleware (Bearer token validation)
    - Configure rate limiting middleware (per-endpoint, configurable)
    - Configure error handling middleware (standardized error format)
    - Configure request validation middleware (1 MB body size limit, JSON validation)
    - Ensure API does not accept traffic until all middleware fully initialized
    - URI-based versioning under `/api/v1` prefix
    - _Requirements: 5.1, 5.3, 5.5, 5.6_

  - [ ] 9.2 Implement ExecuteController
    - `POST /api/v1/execute/{pluginId}` — accept JSON body with `input` (required), optional `version`, optional `metadata.correlationId`
    - Delegate to `IExecutionPipeline` and return HTTP 200 with `ExecutionResult` (success, data, executionId, traceId, durationMs)
    - _Requirements: 5.2_

  - [ ] 9.3 Implement PluginsController
    - `GET /api/v1/plugins` — list plugins
    - `GET /api/v1/plugins/{pluginId}` — get plugin details
    - `POST /api/v1/plugins/upload` — multipart/form-data, max 50 MB ZIP, return 202 with pluginVersionId and status "Scanning"
    - `POST /api/v1/plugins/{pluginId}/reload` — trigger hot-reload
    - `POST /api/v1/plugins/{pluginId}/revoke` — revoke plugin
    - Reject files exceeding 50 MB or invalid ZIP with 400
    - _Requirements: 5.8_

  - [ ] 9.4 Implement ApprovalsController and ExtensionsController
    - `GET /api/v1/approvals?status=Pending` — list pending approvals
    - `POST /api/v1/approvals/{versionId}/approve` — approve plugin version
    - `POST /api/v1/approvals/{versionId}/reject` — reject plugin version
    - `GET /api/v1/approvals/{versionId}/permissions` — get permission review
    - `POST /api/v1/extensions/{targetId}/subscribe` — subscribe to extension
    - `GET /api/v1/extensions/{extensionId}/subscriptions` — list subscriptions
    - `POST /api/v1/extensions/{extensionId}/subscriptions/{id}/decide` — approve/reject subscription
    - `POST /api/v1/extensions/{extensionId}/subscriptions/{id}/revoke` — revoke subscription
    - _Requirements: 5.2, 8.4_

  - [ ] 9.5 Implement health and readiness endpoints
    - `GET /health` — return 200 with "Healthy" when DB, Redis, storage all reachable; 503 with "Unhealthy" identifying failing dependency
    - `GET /ready` — return 200 only when all checks pass and runtime initialized
    - _Requirements: 5.7_

  - [ ] 9.6 Implement standardized error response format
    - Error format: `{ error: { code, category, message, traceId, timestamp } }`
    - HTTP status mapping: Validation → 400, Security → 403, NotFound → 404, Execution → 500, Timeout → 504, ResourceLimit → 429
    - Rate limit exceeded → 429 with `Retry-After` header
    - _Requirements: 5.4, 5.6_

  - [ ]* 9.7 Write integration tests for API layer
    - Test unauthenticated requests receive 401
    - Test all error responses match standardized format schema
    - Test all documented endpoints are routable
    - Test rate-limited requests receive 429 with Retry-After header
    - Test 50 MB+ upload rejected with 400
    - _Requirements: 5.9_

- [ ] 10. Checkpoint - API Layer complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 11. Infrastructure - Persistence & External Services
  - [ ] 11.1 Implement PluginRuntimeDbContext with all 13 tables
    - Configure all 13 DbSets mapping to docs/data/database-schema.md
    - Configure `HasColumnType("jsonb")` for all JSONB columns (permissions, capabilities, metadata, risk_summary, permission_diff, conditions, invocation_policy, config, input_schema, output_schema, expected_usage)
    - Configure `ValueConverter` for all enum-to-string columns (status, decision, visibility, actor_type, result, overall_risk_level, signature_algorithm)
    - Configure `HasQueryFilter` for soft-delete on plugins table (where deleted_at IS NULL)
    - Override `SaveChanges` to prevent UPDATE/DELETE on audit_logs (throw InvalidOperationException)
    - _Requirements: 6.1, 6.3_

  - [ ] 11.2 Create database migrations matching schema specification
    - Generate EF Core migrations matching docs/data/database-schema.md exactly
    - Include all 19 indexes defined in Section 4
    - Include unique constraints: uq_plugin_version, uq_subscription, uq_declarative_version
    - Include all foreign key relationships
    - _Requirements: 6.2_

  - [ ] 11.3 Implement repository interfaces in Core and implementations in Infrastructure
    - Define one repository interface per entity in `PluginRuntime.Core.Interfaces` (13 total: IPluginRepository, IPluginVersionRepository, IManifestRepository, ICapabilityRepository, IExecutionRepository, IAuditLogRepository, IRevocationRepository, IApprovalRepository, IRuntimeNodeRepository, IExtensionRegistryRepository, IExtensionSubscriptionRepository, IPermissionReviewRepository, IDeclarativeConfigRepository)
    - Implement each repository in `PluginRuntime.Infrastructure`
    - IAuditLogRepository: insert-only (no Update/Delete methods)
    - _Requirements: 6.6_

  - [ ] 11.4 Implement RedisCacheService
    - Configurable TTL defaulting to 300 seconds (range 10-86400 seconds)
    - Cache revocation lists, plugin metadata, and capability resolution results
    - Implement `ICacheService` interface defined in Core
    - _Requirements: 6.4_

  - [ ] 11.5 Implement ObjectStorageService
    - Store plugin ZIP/DLL at `{plugin_id}/{version_id}/` path prefix
    - Enforce 50 MB maximum file size per object
    - Restrict write access to application service identity only
    - _Requirements: 6.5_

  - [ ] 11.6 Implement infrastructure failure handling
    - If PostgreSQL, Redis, or object storage is unreachable: fail closed
    - Log connectivity failure with service name and error details
    - Return structured error indicating infrastructure unavailability
    - Connection timeout: 5 seconds
    - _Requirements: 6.7_

  - [ ]* 11.7 Write tests for Infrastructure layer
    - Test EF migrations apply without errors to empty database
    - Test repository CRUD operations for all 13 entities
    - Test audit_logs rejects UPDATE and DELETE attempts
    - Test Redis cache set/get/expire with TTL
    - Test soft-delete query filter excludes deleted records
    - _Requirements: 6.8_

- [ ] 12. Checkpoint - Infrastructure complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 13. Observability - Telemetry & Monitoring
  - [ ] 13.1 Configure OpenTelemetry tracing for execution pipeline
    - Emit trace per plugin execution with spans for each pipeline stage (ManifestValidator, SignatureVerifier, HashVerifier, CapabilityResolver, PluginLoader, PluginExecutor)
    - Each span records: TraceId, SpanId, ExecutionId, PluginId, Version, StartTime, EndTime, Duration, Status, MemoryUsageMb
    - Telemetry overhead SHALL NOT exceed 5ms per execution
    - _Requirements: 7.1, 7.7_

  - [ ] 13.2 Implement structured JSON logging
    - Configure structured JSON output with fields: timestamp (ISO 8601), level, traceId, executionId, pluginId, correlationId, userId, tenantId, event name, message
    - Log on HTTP request received and plugin execution completion
    - _Requirements: 7.2_

  - [ ] 13.3 Implement Prometheus metrics endpoint
    - Expose `/metrics` endpoint with Prometheus format
    - Counters: plugin_execution_total (by status), plugin_timeout_total, security_signature_failures_total, security_capability_denied_total, security_revoked_execution_attempts
    - Histograms: plugin_execution_duration_ms, plugin_memory_usage_mb
    - Gauge: plugin_execution_active
    - _Requirements: 7.3_

  - [ ] 13.4 Implement security event audit logging and metric integration
    - On security events (invalid_signature, hash_mismatch, capability_violation, timeout_exceeded, revoked_plugin_attempt): insert immutable audit_logs record
    - Include action, actor, target, result, timestamp
    - Increment corresponding security metric counter
    - _Requirements: 7.4_

  - [ ] 13.5 Implement /health and /ready with dependency checks
    - `/health`: return JSON with overall status + individual checks (database, Redis, storage)
    - `/ready`: return 200 only when ALL dependency checks pass (database, Redis, storage) and runtime initialization complete; IF any individual check fails THEN return 503 regardless of other checks passing
    - If OpenTelemetry collector unavailable: continue processing, buffer telemetry up to configurable limit (minimum: 0, where 0 disables buffering entirely and telemetry data is dropped)
    - _Requirements: 7.5, 7.6_

  - [ ]* 13.6 Write tests for observability
    - Test trace with all required spans emitted per execution
    - Test security events produce audit_logs records
    - Test each metric counter increments by exactly 1 per event
    - Test /health returns dependency status accurately
    - _Requirements: 7.8_

- [ ] 14. Checkpoint - Observability complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 15. Inter-Extension Communication
  - [ ] 15.1 Implement ExtensionCapability invoke logic
    - Verify checks in priority order: permission → existence → visibility
    - (1) Verify caller's manifest declares `extension:invoke:{targetId}` permission → CapabilityDeniedException if denied
    - (2) Verify target extension exists and has status Active → ExtensionNotFoundException if not found
    - (3) Implement visibility check: Public → allow; Private → same owner only; Subscription → active approved subscription exists → AccessDeniedException if access denied
    - If multiple checks fail, return error for the highest-priority failed check (permission > existence > visibility)
    - _Requirements: 8.1_

  - [ ] 15.2 Implement call depth limiting and circular invocation detection
    - Maintain call stack in execution context
    - Reject invocation when call depth exceeds configurable maximum (default: 3)
    - Detect circular invocation (A → B → A) and reject with CircularInvocationException
    - Check call stack before each invoke
    - _Requirements: 8.2, 8.3_

  - [ ] 15.3 Implement subscription workflow
    - `POST /api/v1/extensions/{targetId}/subscribe` records subscription with status "Requested", reason, expected_usage
    - Target owner can Approve/Reject via decide endpoint
    - Subscription status transitions: Requested → Approved/Rejected
    - Subscription-based visibility requires status "Approved" and expires_at > now
    - _Requirements: 8.4, 8.5_

  - [ ] 15.4 Implement timeout cascading and rate limiting for inter-extension calls
    - Child timeout = min(target's manifest timeout_ms, caller's remaining time)
    - Enforce per-caller rate limit from target's `invocation_policy.rate_limit_per_caller`
    - Return RateLimitExceededException when exceeded
    - Validate caller input against `invocation_policy.allowed_input_schema` (JSON Schema)
    - Reject invalid input without executing target
    - _Requirements: 8.6, 8.7, 8.8_

  - [ ]* 15.5 Write tests for inter-extension communication
    - Test visibility enforcement (Private/Public/Subscription)
    - Test error priority order: permission denied before existence before visibility
    - Test subscription workflow state transitions
    - Test call depth limiting rejects at max depth
    - Test circular detection rejects A → B → A
    - Test timeout cascading formula correctness
    - Test rate limiting per caller
    - Test input schema validation rejects invalid input
    - Test privilege non-escalation (B runs with B's permissions, not A's)
    - _Requirements: 8.9_

- [ ] 16. Checkpoint - Inter-Extension Communication complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 17. Admin Portal - Management UI
  - [ ] 17.1 Create Blazor Server project with MudBlazor and navigation structure
    - Configure Blazor Server application with MudBlazor UI components
    - Create navigation structure: Dashboard, Extensions, Approvals, Monitoring, Audit, Marketplace pages
    - Configure typed HttpClient to PluginRuntime.Api with Bearer JWT authentication
    - _Requirements: 9.1, 9.6_

  - [ ] 17.2 Implement Dashboard page with real-time metrics
    - Display system metrics: active plugin count, total execution count, error rate %, CPU %, memory %
    - Configure SignalR for real-time updates (within 5 seconds of change)
    - _Requirements: 9.2_

  - [ ] 17.3 Implement Approvals page with permission review
    - Display: plugin name, version, author, upload timestamp, manifest permissions, risk level, per-permission risk, permission diff, capability requests, auto-generated flags
    - Admin actions: Approved, ApprovedWithConditions, Rejected, NeedsInfo with comment (max 2000 chars)
    - _Requirements: 9.3_

  - [ ] 17.4 Implement Monitoring page with live execution stream
    - Live execution stream via SignalR (within 5 seconds)
    - Historical logs paginated at 50 records/page
    - Filtering by plugin name, status (Running/Completed/Failed/Cancelled/Timeout), time range
    - Each entry: executionId, pluginId, status, durationMs, traceId
    - _Requirements: 9.4_

  - [ ] 17.5 Implement Audit page with filtering
    - Filter by actor (actor_id), action, resource type, time range, result (Success/Failure/Denied)
    - Results paginated at 50 records/page
    - _Requirements: 9.5_

  - [ ] 17.6 Implement SignalR reconnection handling
    - Display visible disconnection indicator on connection loss
    - Automatic reconnection at 5-second intervals
    - Maximum 10 reconnection attempts
    - _Requirements: 9.7_

  - [ ]* 17.7 Write Blazor component tests for Admin Portal
    - Test all six pages render without error
    - Test Dashboard binds to metric values from SignalR
    - Test approval workflow UI displays data and submits decisions
    - Test real-time updates rendered within SignalR connection
    - _Requirements: 9.8_

- [ ] 18. Checkpoint - Admin Portal complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 19. Integration Testing & Security Hardening
  - [ ] 19.1 Implement end-to-end integration test traversing full execution flow
    - Test scenario: Plugin Upload → Security Scan → Approval + Signing → Plugin Repository → Runtime Request → Manifest Validation → Signature Check → Capability Resolution → Isolated Execution → Observability
    - Each stage produces verifiable output (HTTP response, database record, or telemetry trace)
    - _Requirements: 10.1_

  - [ ] 19.2 Implement security rejection integration tests
    - Test tampered binary (SHA-256 mismatch) rejected at HashVerifier stage
    - Test forged manifest (invalid signature) rejected at SignatureVerifier stage
    - Test undeclared capability rejected at CapabilityResolver stage
    - Each rejection produces immutable audit log entry with TraceId, PluginId, reason, timestamp
    - _Requirements: 10.2_

  - [ ] 19.3 Implement concurrent isolation integration tests
    - Execute 10+ plugins concurrently
    - Verify no plugin can read/write another's namespaced data (storage, cache, DB schema)
    - Verify no plugin can invoke capabilities not in its manifest
    - Verify no plugin's ALC shares mutable state with another
    - _Requirements: 10.3_

  - [ ] 19.4 Implement security hardening verification tests
    - Verify no secrets in source code or compiled assemblies
    - Verify signing keys accessed exclusively via KMS/HSM provider interface
    - Verify all endpoints except /health and /ready return 401 without Bearer token
    - Verify rate limiting returns 429 when threshold exceeded
    - Verify payloads exceeding 50 MB are rejected
    - _Requirements: 10.5_

  - [ ]* 19.5 Write cross-module property-based tests for correctness invariants
    - **Property 12: Any validation failure results in execution rejection (fail-closed)**
    - **Property 13: Any undeclared capability request is denied (deny-by-default)**
    - **Property 14: No plugin execution can access another plugin's namespaced resources (isolation)**
    - **Property 15: No audit log entry can be modified or deleted after creation (immutability)**
    - Over at least 100 randomized inputs per property
    - **Validates: Requirements 10.6**

  - [ ]* 19.6 Write performance baseline tests
    - Verify cold plugin load under 500ms
    - Verify warm plugin load under 100ms
    - Verify manifest validation under 10ms
    - Verify signature verification under 20ms
    - Run under simulated load of 500 concurrent requests at P95
    - _Requirements: 10.4_

- [ ] 20. Final Checkpoint - All integration tests pass
  - Ensure all tests pass with zero failures and zero security validation bypasses, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation between phases
- Property tests validate universal correctness properties (fail-closed, deny-by-default, isolation, immutability)
- Unit tests validate specific examples and edge cases
- The phased approach ensures each layer compiles and passes tests before building the next
- All code uses C# with .NET 10, async/await, CancellationToken on every async path
- Security > Performance > Convenience in all implementation decisions

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["1.3", "1.4", "1.5"] },
    { "id": 2, "tasks": ["1.6", "1.7", "1.8"] },
    { "id": 3, "tasks": ["3.1", "3.2", "3.3"] },
    { "id": 4, "tasks": ["3.4", "3.5"] },
    { "id": 5, "tasks": ["3.6", "3.7", "3.8"] },
    { "id": 6, "tasks": ["5.1", "5.2", "5.3"] },
    { "id": 7, "tasks": ["5.4", "5.5", "5.6", "5.7"] },
    { "id": 8, "tasks": ["7.1", "7.2", "7.3", "7.4", "7.5"] },
    { "id": 9, "tasks": ["7.6", "7.7"] },
    { "id": 10, "tasks": ["9.1", "9.5", "9.6"] },
    { "id": 11, "tasks": ["9.2", "9.3", "9.4"] },
    { "id": 12, "tasks": ["9.7"] },
    { "id": 13, "tasks": ["11.1", "11.4", "11.5"] },
    { "id": 14, "tasks": ["11.2", "11.3", "11.6"] },
    { "id": 15, "tasks": ["11.7"] },
    { "id": 16, "tasks": ["13.1", "13.2", "13.3"] },
    { "id": 17, "tasks": ["13.4", "13.5", "13.6"] },
    { "id": 18, "tasks": ["15.1", "15.2", "15.3"] },
    { "id": 19, "tasks": ["15.4", "15.5"] },
    { "id": 20, "tasks": ["17.1"] },
    { "id": 21, "tasks": ["17.2", "17.3", "17.4", "17.5"] },
    { "id": 22, "tasks": ["17.6", "17.7"] },
    { "id": 23, "tasks": ["19.1", "19.2", "19.3", "19.4"] },
    { "id": 24, "tasks": ["19.5", "19.6"] }
  ]
}
```
