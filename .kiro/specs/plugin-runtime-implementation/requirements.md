# Requirements Document

## Introduction

Phát triển implementation cho Metadata-Driven Secure Plugin Runtime trên .NET 10. Dự án có documentation kiến trúc hoàn chỉnh (100%) nhưng chưa có code (0%). Plan chia thành 10 phases, mỗi phase tạo ra code hoàn chỉnh, chạy được, test được trước khi chuyển sang phase tiếp theo.

Nguồn tham chiếu:
- #[[file:docs/architecture/architecture.md]]
- #[[file:docs/implementation/solution-structure.md]]
- #[[file:docs/architecture/runtime-engine-spec.md]]
- #[[file:docs/architecture/runtime-api-spec.md]]
- #[[file:docs/security/security-model.md]]
- #[[file:docs/implementation/capability-interfaces.md]]
- #[[file:docs/data/database-schema.md]]

## Glossary

- **ALC**: AssemblyLoadContext - .NET mechanism for plugin isolation
- **Capability**: Interface-based access control layer between plugins and infrastructure
- **Manifest**: Security contract defining plugin identity, permissions, and resource limits
- **Zero Trust**: Security model where no plugin is trusted, even after approval
- **Fail Closed**: Any error stops execution immediately, no fallback to unsafe mode
- **Hot Reload**: Loading new plugin version without restarting the Core runtime
- **PBT**: Property-Based Testing - testing with formal correctness properties over randomized inputs

## Requirements

### Requirement 1: Foundation - Solution Structure & Core Domain

**User Story:** As a developer, I want the .NET solution skeleton with Core domain entities and SDK interfaces, so that all subsequent phases have a stable foundation to build upon.

#### Acceptance Criteria

1. WHEN the solution is created, THEN it SHALL contain all projects defined in docs/implementation/solution-structure.md (PluginRuntime.Core, PluginRuntime.Runtime, PluginRuntime.Security, PluginRuntime.Infrastructure, PluginRuntime.Infrastructure.KeyVault, PluginRuntime.Api, PluginRuntime.Admin, PluginRuntime.Capabilities.Abstractions, PluginRuntime.Capabilities.Database, PluginRuntime.Capabilities.Network, PluginRuntime.Capabilities.Storage, PluginRuntime.Capabilities.Cache, PluginRuntime.Capabilities.Extension, PluginRuntime.Sdk) with dependency flow matching docs/implementation/solution-structure.md: SDK has zero dependencies, Core has zero external NuGet dependencies, Capabilities.Abstractions depends on Core, Security depends on Core, Runtime depends on Core and Capabilities.Abstractions, Infrastructure depends on Core, and Api depends on all projects
2. WHEN PluginRuntime.Core is built, THEN it SHALL compile with zero external NuGet dependencies and contain domain entities (Plugin, Manifest, Execution, PluginVersion), interfaces (IPluginExecutor, IManifestValidator, IPluginLoader, IExecutionPipeline, ICapabilityResolver, IExecutionGovernor), value objects, and enums as referenced in the Core namespace conventions (PluginRuntime.Core.Entities, PluginRuntime.Core.Interfaces, PluginRuntime.Core.ValueObjects, PluginRuntime.Core.Enums)
3. WHEN PluginRuntime.Sdk is built, THEN it SHALL compile with zero project references and zero external NuGet dependencies, and expose exactly IPlugin interface, PluginContext, and PluginResult as public types suitable for packaging as an independent NuGet package for plugin developers
4. WHEN PluginRuntime.Capabilities.Abstractions is built, THEN it SHALL contain ICapability base interface (with Name and Version properties) plus IDatabaseCapability, INetworkCapability, IStorageCapability, ICacheCapability, and IExtensionCapability with method signatures matching docs/implementation/capability-interfaces.md, and their associated record types (NetworkRequest, NetworkResponse, StorageMetadata, ExtensionInvocationResult)
5. WHEN `dotnet build` is run on the solution, THEN all projects SHALL target net10.0 and compile successfully with TreatWarningsAsErrors enabled and producing zero warnings
6. WHEN `dotnet test` is run, THEN unit tests for Core domain entity creation and validation SHALL pass, covering at minimum: each domain entity can be instantiated with valid data, and each entity rejects invalid data (null/empty required fields)
7. WHEN the solution is created, THEN it SHALL contain corresponding test projects (PluginRuntime.Core.Tests, PluginRuntime.Runtime.Tests, PluginRuntime.Security.Tests, PluginRuntime.Api.Tests, PluginRuntime.Infrastructure.Tests, PluginRuntime.IntegrationTests) each referencing their respective source project

---

### Requirement 2: Security Engine - Manifest Validation & Cryptographic Verification

**User Story:** As a runtime operator, I want the security engine to validate plugin manifests and verify cryptographic signatures, so that only authorized, untampered plugins can execute.

#### Acceptance Criteria

1. WHEN a manifest is submitted for validation, THEN ManifestValidator SHALL verify: schema compliance (all required fields present and valid types), required fields (plugin_id, version, permissions, capabilities, signature, public_key_id, issued_at, expires_at), version compatibility with target_core_version, and expiration date; IF any check fails THEN validation SHALL return a structured error identifying the specific failure
2. WHEN a plugin binary is loaded, THEN HashVerifier SHALL compute SHA-256 of the DLL bytes and compare against the sha256 field in the manifest; IF mismatch THEN execution SHALL be rejected immediately with error code indicating integrity failure
3. WHEN signature verification runs, THEN SignatureVerifier SHALL validate RSA-SHA256 (default) or ECDSA-SHA256 digital signature on the manifest content using the public key referenced by public_key_id; IF the signature is invalid or the key is not found THEN execution SHALL be rejected with a specific error code
4. WHEN RevocationChecker runs, THEN it SHALL query the revocations table (with Redis cache) and reject any plugin version_id that has an active revocation record; expired revocations (expires_at < now) SHALL NOT block execution
5. WHEN any security validation fails, THEN the system SHALL fail closed (stop immediately), log the failure as an immutable audit_logs entry with TraceId, PluginId, failure reason, actor, and timestamp, and return a structured error response with category "Security"
6. WHEN all validations pass on a valid manifest, THEN manifest validation SHALL complete in under 10ms and signature verification in under 20ms at P95
7. WHEN `dotnet test` is run on PluginRuntime.Security.Tests, THEN property-based tests SHALL verify over at least 100 randomized inputs: valid manifests always pass validation, any single tampered field causes validation failure, expired manifests (expires_at < now) always fail, revoked plugin versions always fail, and invalid signatures always fail

---

### Requirement 3: Runtime Engine - Execution Pipeline & Plugin Loader

**User Story:** As a runtime operator, I want the execution engine to orchestrate plugin loading and execution through a staged pipeline with resource governance, so that plugins run in isolation with enforced limits.

#### Acceptance Criteria

1. WHEN an execution request arrives, THEN IExecutionPipeline SHALL process it through stages in fixed order: ManifestValidator → SignatureVerifier → HashVerifier → CapabilityResolver → PluginLoader → PluginExecutor → ObservabilityCollector
2. WHEN any pipeline stage fails, THEN execution SHALL short-circuit immediately (fail closed), subsequent stages SHALL NOT execute, and the pipeline SHALL return a structured error result containing the failing stage name, error code, and TraceId
3. WHEN IPluginLoader loads a plugin, THEN it SHALL create an isolated AssemblyLoadContext (collectible), resolve the entry point class implementing IPlugin, and return the plugin instance; no shared mutable state between loaded plugins
4. IF IPluginLoader cannot resolve the entry point class or the class does not implement IPlugin, THEN it SHALL reject the load with a structured error indicating the resolution failure and leave no residual state in the AssemblyLoadContext registry
5. WHEN IExecutionGovernor enforces limits, THEN it SHALL enforce execution timeout (as defined in ResourceLimits.TimeoutMs) by cancelling the CancellationToken when the timeout expires, monitor memory usage against ResourceLimits.MaxMemoryMb, and apply cooperative CPU cancellation against ResourceLimits.MaxCpuMs; the CancellationToken serves as the enforcement mechanism that plugins must observe cooperatively
6. IF a plugin exceeds its ResourceLimits.MaxMemoryMb during execution, THEN IExecutionGovernor SHALL cancel the CancellationToken and terminate execution within 1 second of detection
7. WHEN a plugin exceeds its timeout (ResourceLimits.TimeoutMs), THEN execution SHALL be terminated via CancellationToken cancellation within 100ms of the timeout expiring
8. WHEN hot-reload is triggered, THEN the system SHALL stop new requests to old version, wait for active executions to complete up to a maximum drain timeout of 30 seconds, unload old ALC, load new version, warm-up, then resume traffic with zero request interruption
9. IF active executions do not complete within the 30-second drain timeout during hot-reload, THEN the system SHALL cancel remaining executions via CancellationToken and proceed with unloading the old ALC
10. WHEN cold plugin load occurs, THEN it SHALL complete in under 500ms; warm load SHALL complete in under 100ms
11. WHEN `dotnet test` is run on PluginRuntime.Runtime.Tests, THEN tests SHALL verify: pipeline stages execute in correct order, failures short-circuit with structured error, timeout enforcement terminates within bounds, memory limit enforcement terminates execution, ALC isolation prevents cross-plugin state sharing, hot-reload completes drain or force-cancels

---

### Requirement 4: Capability Layer - Infrastructure Access Control

**User Story:** As a plugin developer, I want capability interfaces that provide controlled access to database, network, storage, and cache, so that my plugin can interact with infrastructure without direct access.

#### Acceptance Criteria

1. WHEN IDatabaseCapability is used, THEN it SHALL execute only parameterized queries (reject any SQL containing string interpolation markers), scope data access to the plugin's own isolated schema (prefix all table references with plugin namespace), and handle connection management transparently via connection pooling
2. WHEN INetworkCapability is used, THEN it SHALL proxy HTTP calls only to domains explicitly listed in the plugin manifest's allowed_domains field, enforce a maximum response size of 10 MB per request, enforce the request timeout from NetworkRequest.TimeoutMs, and block any URL not matching approved patterns
3. WHEN IStorageCapability is used, THEN it SHALL scope all storage keys to `{pluginId}/{key}` namespace, enforce a maximum per-object size of 50 MB and configurable per-plugin total storage quota, and reject any key containing path traversal sequences (../, ..\)
4. WHEN ICacheCapability is used, THEN it SHALL namespace all cache keys as `{pluginId}:{key}`, enforce a configurable maximum key count per plugin (default: 10000), enforce maximum value size of 1 MB after serialization, and use System.Text.Json for serialization/deserialization
5. WHEN a plugin attempts to use a capability not declared in its manifest capabilities array, THEN the system SHALL deny access immediately (deny-by-default), log the violation as a security event in audit_logs, and return an error with category "Security" and code indicating capability denial
6. WHEN ICapabilityResolver resolves capabilities for a plugin, THEN it SHALL return ONLY capabilities explicitly granted in the manifest; no implicit permissions; the returned dictionary SHALL use capability name as key
7. WHEN any capability method is invoked, THEN CancellationToken SHALL be propagated through the entire call chain and the capability SHALL respect cancellation within 100ms
8. WHEN `dotnet test` is run on capability tests, THEN property-based tests SHALL verify over at least 100 randomized inputs: namespace isolation is never violated (no cross-plugin data access), undeclared capabilities are always denied, size limits are enforced, path traversal is always blocked

---

### Requirement 5: API Layer - HTTP Gateway & Controllers

**User Story:** As an API consumer, I want a well-structured HTTP API that handles authentication, request validation, and error formatting, so that I can interact with the plugin runtime programmatically.

#### Acceptance Criteria

1. WHEN the API starts, THEN it SHALL expose all endpoints defined in docs/architecture/runtime-api-spec.md under `/api/v1` prefix with URI-based versioning and SHALL NOT accept traffic until all middleware (authentication, rate limiting, error handling) is fully initialized
2. WHEN POST /api/v1/execute/{pluginId} is called with a valid JWT token and a JSON request body containing `input` (required object), optional `version` (string), and optional `metadata.correlationId` (string), THEN it SHALL delegate to the Runtime Engine and return HTTP 200 with ExecutionResult containing success (boolean), data (object), executionId (string), traceId (string), and durationMs (number)
3. IF any request (except GET /health and GET /ready) lacks a Bearer token, has an expired token, or has a token with invalid signature, wrong issuer, or wrong audience, THEN the API SHALL return HTTP 401 Unauthorized with the standardized error format
4. WHEN any error occurs, THEN the API SHALL return the standardized error format `{ error: { code, category, message, traceId, timestamp } }` with HTTP status mapped as: Validation → 400, Security → 403, NotFound → 404, Execution → 500, Timeout → 504, ResourceLimit → 429
5. IF a request body fails validation (malformed JSON, missing required fields, or body exceeding 1 MB), THEN the API SHALL return HTTP 400 with error category "Validation" and an error message indicating the validation failure reason
6. WHEN rate limiting is exceeded for any endpoint, THEN the API SHALL return HTTP 429 Too Many Requests with a Retry-After header specifying the number of seconds until the client may retry, where rate limits are configurable per endpoint
7. WHEN GET /health is called, THEN it SHALL return HTTP 200 with status "Healthy" and individual check results for database, redis, and storage when all dependencies are reachable; IF any dependency is unreachable, THEN it SHALL return HTTP 503 with status "Unhealthy" and identify the failing dependency
8. WHEN POST /api/v1/plugins/upload is called with multipart/form-data containing a plugin ZIP file not exceeding 50 MB, THEN it SHALL return HTTP 202 Accepted with pluginVersionId, status "Scanning", and initiate async validation; IF the file exceeds 50 MB or is not a valid ZIP archive, THEN it SHALL return HTTP 400 with error category "Validation"
9. WHEN `dotnet test` is run on PluginRuntime.Api.Tests, THEN integration tests SHALL verify: unauthenticated requests receive 401, all error responses match the standardized error format schema, all documented endpoints are routable, and rate-limited requests receive 429 with Retry-After header

---

### Requirement 6: Infrastructure - Persistence & External Services

**User Story:** As a runtime operator, I want PostgreSQL persistence, Redis caching, and object storage integration, so that the system has reliable data storage and performance optimization.

#### Acceptance Criteria

1. WHEN the application starts, THEN EF Core DbContext SHALL be configured with all 13 tables from docs/data/database-schema.md with HasColumnType("jsonb") for all JSONB columns (permissions, capabilities, metadata, risk_summary, permission_diff, conditions, invocation_policy, config, input_schema, output_schema, expected_usage), ValueConverter for all enum-to-string columns (status, decision, visibility, actor_type, result, overall_risk_level, signature_algorithm), and HasQueryFilter for soft-delete on the plugins table (where deleted_at IS NULL)
2. WHEN database migrations are applied, THEN the schema SHALL match docs/data/database-schema.md exactly, including all 19 indexes defined in Section 4, unique constraints (uq_plugin_version, uq_subscription, uq_declarative_version), and foreign key relationships
3. WHEN audit_logs table is accessed, THEN the application SHALL enforce insert-only behavior by preventing UPDATE and DELETE operations at the DbContext level; IF an update or delete is attempted on audit_logs, THEN the system SHALL throw an InvalidOperationException and the operation SHALL NOT be persisted
4. WHEN Redis is configured, THEN it SHALL serve as cache for revocation lists, plugin metadata, and capability resolution results with a configurable TTL defaulting to 300 seconds (5 minutes) and supporting a configurable range of 10 to 86400 seconds
5. WHEN object storage is configured, THEN it SHALL store plugin ZIP binaries and DLLs scoped by plugin_id and version_id path prefix, enforce a maximum file size of 50 MB per object, and restrict write access to the application service identity only
6. THE PluginRuntime.Infrastructure project SHALL expose a repository interface in PluginRuntime.Core for each of the 13 database entities (plugins, plugin_versions, manifests, capabilities, executions, audit_logs, revocations, approvals, runtime_nodes, extension_registry, extension_subscriptions, permission_reviews, declarative_configs) with corresponding implementations in PluginRuntime.Infrastructure
7. IF PostgreSQL, Redis, or object storage is unreachable during an operation, THEN the system SHALL fail closed, log the connectivity failure with the service name and error details, and return a structured error indicating infrastructure unavailability within 5 seconds (connection timeout)
8. WHEN `dotnet test` is run on PluginRuntime.Infrastructure.Tests, THEN tests SHALL verify: EF migrations apply without errors to an empty database, repository CRUD operations succeed for all 13 entities, audit_logs rejects UPDATE and DELETE attempts, Redis cache set/get/expire operations work with TTL, and soft-delete query filter excludes records where deleted_at is not null

---

### Requirement 7: Observability - Telemetry & Monitoring

**User Story:** As a runtime operator, I want comprehensive observability with traces, metrics, and structured logging, so that I can monitor system health and debug issues.

#### Acceptance Criteria

1. WHEN any plugin executes, THEN OpenTelemetry SHALL emit a trace containing spans for each pipeline stage (ManifestValidator, SignatureVerifier, HashVerifier, CapabilityResolver, PluginLoader, PluginExecutor) with each span recording: TraceId, SpanId, ExecutionId, PluginId, Version, StartTime, EndTime, Duration, Status, and MemoryUsageMb
2. WHEN any HTTP request is received or any plugin execution completes, THEN structured JSON logging SHALL record the event with fields: timestamp (ISO 8601), level (Information/Warning/Error/Critical), traceId, executionId, pluginId, correlationId, userId, tenantId, event name, and message
3. WHEN /metrics endpoint is called, THEN it SHALL expose Prometheus-format metrics including: plugin_execution_total (Counter, labeled by status), plugin_execution_duration_ms (Histogram), plugin_execution_active (Gauge), plugin_memory_usage_mb (Histogram), plugin_timeout_total (Counter), security_signature_failures_total (Counter), security_capability_denied_total (Counter), and security_revoked_execution_attempts (Counter)
4. WHEN a security event occurs (invalid_signature, hash_mismatch, capability_violation, timeout_exceeded, or revoked_plugin_attempt), THEN the system SHALL insert an immutable record into the audit_logs table with action, actor, target, result, and timestamp, and SHALL increment the corresponding security metric counter
5. WHEN /health is called, THEN it SHALL return a JSON response with overall status (Healthy/Degraded/Unhealthy) and individual check results for database, Redis, and storage connectivity; WHEN /ready is called, THEN it SHALL return 200 only when all dependency checks pass (database, Redis, and storage) and the runtime has completed initialization; IF any individual dependency check fails THEN /ready SHALL return 503 regardless of other checks passing
6. IF the OpenTelemetry collector or logging sink is unavailable, THEN the system SHALL continue processing requests without blocking execution and SHALL buffer telemetry data up to a configurable limit (minimum: 0, where 0 disables buffering entirely and telemetry data is dropped when collectors are unavailable)
7. WHEN telemetry is emitted for a plugin execution, THEN the telemetry overhead SHALL NOT add more than 5ms to the total execution duration
8. WHEN `dotnet test` is run, THEN tests SHALL verify: a trace with all required spans is emitted for each plugin execution, security events produce audit_logs records, each metric counter increments by exactly 1 per corresponding event, and /health returns dependency status accurately

---

### Requirement 8: Inter-Extension Communication

**User Story:** As a plugin developer, I want my extension to invoke other extensions through a controlled interface with visibility and subscription management, so that extensions can compose functionality safely.

#### Acceptance Criteria

1. WHEN IExtensionCapability.InvokeAsync is called, THEN it SHALL verify in priority order: (a) caller's manifest declares `extension:invoke:{targetId}` permission, (b) target extension exists and has status Active, (c) visibility check passes (Public → allow; Private → same owner only; Subscription → active approved subscription exists); IF multiple checks fail THEN the system SHALL return the error for the highest-priority failed check: permission errors (CapabilityDeniedException) before existence errors (ExtensionNotFoundException) before visibility errors (AccessDeniedException)
2. WHEN call depth exceeds the configured maximum (default: 3, configurable via InterExtension.MaxCallDepth), THEN invocation SHALL be rejected with CircularInvocationException
3. WHEN circular invocation is detected (extension A calls B which calls A), THEN the system SHALL reject the call immediately by maintaining a call stack in the execution context and checking before each invoke
4. WHEN a subscription request is submitted via POST /api/v1/extensions/{targetExtensionId}/subscribe, THEN it SHALL be recorded in extension_subscriptions with status "Requested", reason, and expected_usage; the target extension owner SHALL be able to decide (Approve/Reject) via the decide endpoint
5. WHEN extension visibility is set to Private, THEN only extensions owned by the same author_id SHALL be able to invoke it; Public extensions allow any caller with declared permission; Subscription-based requires a subscription record with status "Approved" and expires_at > now
6. WHEN timeout cascades across extension invocations, THEN child execution timeout SHALL be: min(target's manifest timeout_ms, caller's remaining time); IF parent has 4000ms remaining and target defines 3000ms timeout, child gets 3000ms
7. WHEN target extension defines invocation_policy.rate_limit_per_caller, THEN the system SHALL enforce the rate limit per source extension and return RateLimitExceededException when exceeded
8. WHEN target extension defines invocation_policy.allowed_input_schema, THEN the system SHALL validate caller's input against the JSON Schema before forwarding; IF invalid THEN reject without executing target
9. WHEN `dotnet test` is run, THEN tests SHALL verify: visibility enforcement (Private/Public/Subscription), subscription workflow state transitions, call depth limiting, circular detection, timeout cascading formula, rate limiting per caller, input schema validation, and privilege non-escalation (B runs with B's permissions, not A's)

---

### Requirement 9: Admin Portal - Management UI

**User Story:** As a system administrator, I want a web-based admin portal to manage plugins, review approvals, and monitor system health, so that I can operate the runtime without direct API calls.

#### Acceptance Criteria

1. WHEN admin navigates to the portal URL, THEN a Blazor Server application SHALL render with MudBlazor UI components and display a navigation structure containing Dashboard, Extensions, Approvals, Monitoring, Audit, and Marketplace pages, each accessible within 2 seconds of navigation
2. WHEN viewing the Dashboard, THEN it SHALL display system metrics updated via SignalR within 5 seconds of change: active plugin count, total execution count, error rate percentage, CPU utilization percentage, and memory utilization percentage
3. WHEN reviewing a pending approval, THEN the admin SHALL see plugin name, version, author, upload timestamp, manifest permissions list, overall risk level (Low/Medium/High/Critical), per-permission risk classification, permission diff from previous version (if applicable), capability requests, and auto-generated flags; the admin SHALL be able to select a decision (Approved, ApprovedWithConditions, Rejected, NeedsInfo) and provide a comment of up to 2000 characters
4. WHEN monitoring executions, THEN the portal SHALL show a live execution stream updated via SignalR within 5 seconds, and historical execution logs paginated at 50 records per page with filtering by plugin name, status (Running/Completed/Failed/Cancelled/Timeout), and time range; each execution entry SHALL display executionId, pluginId, status, durationMs, and traceId
5. WHEN viewing audit logs, THEN the admin SHALL be able to filter by actor (actor_id), action, resource type, time range, and result (Success/Failure/Denied), with results paginated at 50 records per page
6. WHEN the admin portal communicates with the backend, THEN it SHALL use typed HttpClient to PluginRuntime.Api with Bearer JWT token authentication on all requests
7. IF the SignalR connection to the backend is lost, THEN the portal SHALL display a visible disconnection indicator and attempt automatic reconnection at 5-second intervals up to a maximum of 10 attempts
8. WHEN `dotnet test` is run, THEN Blazor component tests SHALL verify: all six pages render without error, Dashboard binds to metric values from SignalR, approval workflow UI displays permission review data and submits decisions, and real-time updates are received and rendered within the SignalR connection

---

### Requirement 10: Integration Testing & Security Hardening

**User Story:** As a quality engineer, I want end-to-end integration tests and security hardening verification, so that the complete system works correctly and securely before production deployment.

#### Acceptance Criteria

1. WHEN the full integration test suite runs, THEN it SHALL execute at least one test scenario that traverses every stage in order (Plugin Upload → Security Scan → Approval + Signing → Plugin Repository → Runtime Request → Manifest Validation → Signature Check → Capability Resolution → Isolated Execution → Observability), and each stage SHALL produce a verifiable output (HTTP response, database record, or telemetry trace) confirming it executed successfully
2. WHEN a plugin with a tampered binary (SHA-256 mismatch) is submitted, THEN the system SHALL reject it at the HashVerifier stage; WHEN a plugin with a forged manifest (invalid signature) is submitted, THEN the system SHALL reject it at the SignatureVerifier stage; WHEN a plugin requests capabilities not approved in its manifest, THEN the system SHALL reject it at the CapabilityResolver stage; each rejection SHALL produce an immutable audit log entry containing TraceId, PluginId, rejection reason, and timestamp
3. WHEN 10 or more plugins execute concurrently, THEN isolation SHALL be maintained: no plugin can read or write another plugin's namespaced data (storage keys, cache keys, database schema), no plugin can invoke capabilities not declared in its own manifest, and no plugin's ALC shares mutable state with another plugin's ALC
4. WHEN the system is under load of 500 concurrent requests, THEN performance targets SHALL be met at P95: cold plugin load completes in under 500ms, warm plugin load completes in under 100ms, manifest validation completes in under 10ms, and signature verification completes in under 20ms
5. WHEN security hardening verification runs, THEN the test suite SHALL confirm: no secrets (API keys, signing keys, connection strings) exist in application source code or compiled assemblies, cryptographic signing keys are accessed exclusively via KMS/HSM provider interface, all API endpoints except GET /health and GET /ready return 401 when called without a valid Bearer JWT token, rate limiting returns 429 when request count exceeds the configured threshold, and all API request payloads exceeding 50 MB are rejected
6. WHEN property-based tests run on Security, Runtime, and Capability modules, THEN they SHALL verify correctness invariants over at least 100 randomized inputs per property: any validation failure results in execution rejection (fail-closed), any undeclared capability request is denied (deny-by-default), no plugin execution can access another plugin's namespaced resources (isolation), and no audit log entry can be modified or deleted after creation (immutability)
7. WHEN `dotnet test` is run on PluginRuntime.IntegrationTests, THEN all end-to-end scenarios SHALL pass with zero test failures and zero security validation bypasses
