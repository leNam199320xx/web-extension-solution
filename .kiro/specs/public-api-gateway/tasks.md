# Implementation Plan: Public API Gateway

## Overview

Implement a stateless ASP.NET Core (.NET 10) reverse-proxy gateway that authenticates API consumers via API keys, enforces per-tenant rate limiting and quotas using Redis, meters usage asynchronously to PostgreSQL, and forwards requests to the internal PluginRuntime.Api. The implementation follows a middleware pipeline architecture with fail-closed semantics.

## Tasks

- [x] 1. Set up project structure, configuration, and core models
  - [x] 1.1 Create the PublicApiGateway project with solution file, NuGet dependencies, and directory structure
    - Create `src/PublicApiGateway/PublicApiGateway.csproj` targeting .NET 10
    - Add NuGet packages: StackExchange.Redis, Npgsql, Microsoft.Extensions.Http, OpenTelemetry, System.Threading.Channels
    - Create `tests/PublicApiGateway.Tests/PublicApiGateway.Tests.csproj` with xUnit, FsCheck.Xunit, Testcontainers
    - Create directory structure: Middleware/, Services/, Models/, Configuration/, BackgroundServices/, Health/, Extensions/
    - _Requirements: all (project scaffolding)_

  - [x] 1.2 Implement domain models and enums
    - Create `Models/ApiKeyInfo.cs`, `Models/TenantContext.cs`, `Models/PlanLimits.cs`, `Models/UsageRecord.cs`
    - Create `Models/RateLimitResult.cs`, `Models/QuotaResult.cs`, `Models/GatewayError.cs`
    - Create `Models/PlanType.cs` enum (Free, Pro, Enterprise) and `Models/ApiKeyStatus.cs` enum (Active, Expired, Revoked)
    - _Requirements: 1.1, 1.6, 2.1, 3.1, 4.1, 7.1_

  - [x] 1.3 Implement configuration models and options registration
    - Create `Configuration/GatewayOptions.cs` with cache TTL, body/header size limits, buffer capacity, IP block settings, API key format pattern
    - Create `Configuration/RedisOptions.cs` and `Configuration/UpstreamOptions.cs`
    - Register options via `IOptions<T>` pattern in DI
    - _Requirements: 1.5, 2.4, 4.4, 5.7, 9.4, 9.5_

  - [x] 1.4 Implement service interfaces
    - Create `Services/IApiKeyService.cs`, `Services/IRateLimitService.cs`, `Services/IQuotaService.cs`
    - Create `Services/IUsageMeteringService.cs`, `Services/ITokenService.cs`, `Services/IIpBlockingService.cs`
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.2, 9.6_

  - [x] 1.5 Implement exception types and error code constants
    - Create base `GatewayException` class and derived types: `AuthenticationRequiredException`, `AccessDeniedException`, `RateLimitExceededException`, `QuotaExceededException`, `ServiceUnavailableException`, `UpstreamException`, `SecurityViolationException`
    - Define all GW- error codes as constants
    - _Requirements: 7.1, 7.2, 7.5, 7.6, 7.7, 7.8_

- [x] 2. Implement Security Hardening and Correlation ID middleware
  - [x] 2.1 Implement SecurityHardeningMiddleware
    - Enforce HTTPS check (reject HTTP with 421 / GW-SEC-004)
    - Enforce max request body size (reject with 413 / GW-SEC-001)
    - Enforce max request header size (reject with 431 / GW-SEC-002)
    - All checks run before authentication or routing
    - _Requirements: 9.1, 9.4, 9.5, 9.8_

  - [x]* 2.2 Write property test for request size limit enforcement
    - **Property 17: Request size limit enforcement**
    - **Validates: Requirements 9.4, 9.5**

  - [x] 2.3 Implement CorrelationIdMiddleware
    - Validate incoming `X-Correlation-Id` header (1-128 chars, printable ASCII codes 33-126)
    - Generate UUID v4 if header is missing, empty, invalid, or exceeds 128 chars
    - Store correlation ID in HttpContext.Items for downstream use
    - Return correlation ID in response header for all responses
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

  - [x]* 2.4 Write property test for Correlation ID validation and passthrough
    - **Property 11: Correlation ID validation and passthrough**
    - **Validates: Requirements 6.2, 6.3**

  - [x]* 2.5 Write property test for Correlation ID always present in response
    - **Property 12: Correlation ID always present in response**
    - **Validates: Requirements 6.6**

- [x] 3. Implement API Key Authentication
  - [x] 3.1 Implement IpBlockingService with Redis
    - Implement `IsBlockedAsync`: check `gw:ipblock:{ip}` key existence
    - Implement `RecordFailedAttemptAsync`: increment `gw:ipattempts:{ip}`, block at threshold (>10 in 60s)
    - Set block key with 5-minute TTL when threshold exceeded
    - _Requirements: 9.6_

  - [x]* 3.2 Write property test for IP brute-force blocking
    - **Property 18: IP brute-force blocking at threshold**
    - **Validates: Requirements 9.6**

  - [x] 3.3 Implement ApiKeyService with Redis cache and PostgreSQL fallback
    - Implement `ValidateAsync`: check Redis cache first (`gw:apikey:{sha256_hash}`), fallback to PostgreSQL on miss
    - Validate key status (Active/Expired/Revoked) and expiration date
    - Cache validated keys with configurable TTL (default 300s)
    - Implement `InvalidateCacheAsync` for key revocation
    - _Requirements: 1.1, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8_

  - [x]* 3.4 Write property test for API key validation resolves correct tenant
    - **Property 1: API key validation resolves correct tenant and plan**
    - **Validates: Requirements 1.1, 1.6**

  - [x]* 3.5 Write property test for invalid API keys rejection
    - **Property 2: Invalid API keys are always rejected**
    - **Validates: Requirements 1.3**

  - [x] 3.6 Implement ApiKeyAuthenticationMiddleware
    - Check IP block list first
    - Extract `X-Api-Key` header (missing → 401 / GW-AUTH-001)
    - Validate format against regex `^[a-zA-Z0-9\-_]{32,128}$` (fail → 400 / GW-SEC-005)
    - Validate against cache/DB via IApiKeyService
    - Handle expired (401 / GW-AUTH-002) and revoked (403 / GW-AUTH-003) keys
    - Set TenantContext on HttpContext.Items
    - Record failed attempts for IP blocking
    - Mask API key in logs (show only last 4 chars)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.6, 9.2, 9.3, 9.6, 9.7_

  - [x]* 3.7 Write property test for API key format validation
    - **Property 16: API key format validation**
    - **Validates: Requirements 9.2, 9.3**

  - [x]* 3.8 Write property test for API key masking in logs
    - **Property 19: API key masking in logs**
    - **Validates: Requirements 9.7**

- [x] 4. Checkpoint - Ensure authentication pipeline compiles and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement Rate Limiting
  - [x] 5.1 Implement RateLimitService with Redis sliding window
    - Implement sliding window algorithm using Redis sorted sets
    - Key pattern: `gw:ratelimit:{tenantId}:{windowId}`
    - Pipeline: ZREMRANGEBYSCORE (prune) → ZCARD (count) → ZADD (add) → EXPIRE (TTL)
    - Rollback ZADD if over limit
    - Compute remaining and reset-at values
    - Handle Enterprise plan (unlimited, always allow)
    - Return 503 / GW-RATE-002 if Redis is unreachable
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.8_

  - [x]* 5.2 Write property test for sliding window rate limiting
    - **Property 3: Sliding window rate limiting enforces plan-specific thresholds**
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.6**

  - [x]* 5.3 Write property test for rate limit header consistency
    - **Property 4: Rate limit headers are mathematically consistent**
    - **Validates: Requirements 2.7**

  - [x] 5.4 Implement RateLimitingMiddleware
    - Retrieve TenantContext from HttpContext.Items
    - Call IRateLimitService.CheckAsync
    - On rejection: return 429 with GW-RATE-001
    - Always add X-RateLimit-Limit, X-RateLimit-Remaining, X-RateLimit-Reset headers to response
    - _Requirements: 2.5, 2.6, 2.7_

- [x] 6. Implement Quota Enforcement
  - [x] 6.1 Implement QuotaService with Redis atomic counter
    - Key pattern: `gw:quota:{tenantId}:{yyyy-MM-dd}`
    - Use INCR for atomic increment, set 25h TTL on first increment
    - Reject when count exceeds daily limit
    - Calculate Retry-After as seconds until next UTC midnight
    - Return 503 / GW-QUOTA-002 if Redis is unreachable
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.6_

  - [x]* 6.2 Write property test for quota enforcement at boundary
    - **Property 5: Quota enforcement rejects at daily limit boundary**
    - **Validates: Requirements 3.1, 3.4**

  - [x]* 6.3 Write property test for Retry-After calculation
    - **Property 6: Retry-After calculation correctness**
    - **Validates: Requirements 3.2**

  - [x] 6.4 Implement QuotaEnforcementMiddleware
    - Retrieve TenantContext from HttpContext.Items
    - Call IQuotaService.IncrementAndCheckAsync
    - On rejection: return 429 with GW-QUOTA-001 and Retry-After header
    - _Requirements: 3.1, 3.2, 3.5_

- [x] 7. Implement Request Forwarding and Response Handling
  - [x] 7.1 Implement TokenService for OAuth 2.0 client credentials
    - Acquire short-lived service token from IdP using client credentials flow
    - Cache token in Redis (`gw:servicetoken`) with TTL = token expiry - 60s buffer
    - Return 503 if IdP is unavailable
    - _Requirements: 5.2_

  - [x] 7.2 Implement RequestForwardingMiddleware
    - Forward authenticated request preserving HTTP method, path, query, headers, body
    - Strip `X-Api-Key`, `Connection`, `Keep-Alive`, `Transfer-Encoding`, `Upgrade` headers
    - Add `Authorization: Bearer {service_token}` header via ITokenService
    - Add `X-Tenant-Id` header from TenantContext
    - Add `X-Correlation-Id` header
    - Enforce configurable upstream timeout (default 30s, max 300s)
    - Handle upstream unreachable/5xx → 502 / GW-UPSTREAM-001
    - Handle upstream non-conforming error responses → 502 / GW-UPSTREAM-002
    - Forward valid upstream responses preserving status, headers, body
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_

  - [x]* 7.3 Write property test for request forwarding content preservation
    - **Property 9: Request forwarding preserves content and strips sensitive headers**
    - **Validates: Requirements 5.1, 5.3, 5.4**

  - [x]* 7.4 Write property test for response forwarding
    - **Property 10: Response forwarding preserves upstream response**
    - **Validates: Requirements 5.5**

  - [x]* 7.5 Write property test for non-conforming upstream error wrapping
    - **Property 15: Non-conforming upstream errors are wrapped**
    - **Validates: Requirements 7.4**

- [x] 8. Checkpoint - Ensure request pipeline compiles and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Implement Usage Metering
  - [x] 9.1 Implement UsageMeteringService with Channel<T> and background consumer
    - Use `Channel<UsageRecord>` with bounded capacity of 10,000 (BoundedChannelFullMode.DropOldest)
    - `Enqueue` method writes to channel without blocking
    - _Requirements: 4.1, 4.2, 4.5, 4.7_

  - [x] 9.2 Implement UsageMeteringBackgroundService
    - Read from channel and batch-persist to PostgreSQL
    - Retry with exponential backoff (1s, 2s, 4s) up to 3 attempts
    - Write to dead-letter log on final failure
    - Emit alert event on dead-letter write with count and affected tenant IDs
    - _Requirements: 4.3, 4.4, 4.5, 4.6, 4.7_

  - [x]* 9.3 Write property test for usage record completeness
    - **Property 7: Usage record completeness**
    - **Validates: Requirements 4.1**

  - [x]* 9.4 Write property test for usage buffer non-blocking FIFO eviction
    - **Property 8: Usage buffer is non-blocking with FIFO eviction**
    - **Validates: Requirements 4.5, 4.7**

  - [x] 9.5 Wire usage metering into the middleware pipeline
    - Capture request start time before forwarding
    - After response completes, enqueue UsageRecord with all required fields
    - Ensure metering never blocks the response pipeline
    - _Requirements: 4.1, 4.2, 4.5_

- [x] 10. Implement Error Response Handling
  - [x] 10.1 Implement global exception handler middleware
    - Catch all GatewayException types and map to structured error JSON response
    - Catch unhandled exceptions and return sanitized 500 response
    - Set `traceId` to the request's Correlation ID
    - Set `timestamp` in ISO 8601 UTC with seconds precision
    - Truncate `message` to 500 characters
    - Set Content-Type to `application/json`
    - Ensure no internal details (stack traces, hostnames, connection strings) leak
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8_

  - [x]* 10.2 Write property test for error response schema conformance
    - **Property 13: Error response schema conformance**
    - **Validates: Requirements 7.1, 7.2, 7.6, 7.7, 7.8**

  - [x]* 10.3 Write property test for no internal details leak
    - **Property 14: No internal details leak in error responses**
    - **Validates: Requirements 7.5**

- [x] 11. Implement Health Checks and Observability
  - [x] 11.1 Implement health check endpoints and dependency checks
    - Create `Health/RedisHealthCheck.cs`, `Health/PostgresHealthCheck.cs`, `Health/UpstreamHealthCheck.cs`
    - Register `GET /health` endpoint returning individual dependency status (5s timeout)
    - Register `GET /ready` endpoint verifying startup initialization and last health check
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [x] 11.2 Configure OpenTelemetry tracing and structured logging
    - Add OpenTelemetry tracing with spans for auth, rate limit, quota, forwarding
    - Configure structured JSON logging with: timestamp, level, traceId, spanId, tenantId, method, path, statusCode, durationMs
    - _Requirements: 8.5, 8.6_

  - [x] 11.3 Configure Prometheus metrics endpoint
    - Expose `GET /metrics` with total requests, requests per tenant, error rates, p50/p95/p99 latency
    - _Requirements: 8.7_

- [x] 12. Wire up Program.cs and DI registration
  - [x] 12.1 Implement ServiceCollectionExtensions and compose the application
    - Register all services in DI container
    - Register background services (UsageMeteringBackgroundService)
    - Configure HttpClient for upstream forwarding with Polly policies
    - Register middleware pipeline in correct order: SecurityHardening → CorrelationId → ApiKeyAuth → RateLimit → Quota → RequestForwarding
    - Configure Kestrel HTTPS enforcement
    - _Requirements: all (wiring)_

  - [x]* 12.2 Write integration tests with Testcontainers
    - Test Redis-based rate limiting and quota under concurrency
    - Test PostgreSQL-based API key validation and usage record persistence
    - Test health check endpoints with real dependencies
    - Test end-to-end request flow with WireMock as upstream
    - _Requirements: 1.8, 2.4, 3.4, 4.3, 8.1, 8.2_

- [x] 13. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document using FsCheck
- Unit tests validate specific examples and edge cases using xUnit
- Integration tests use Testcontainers for real Redis/PostgreSQL instances
- The middleware pipeline order is critical — security hardening first, metering last
- All services use CancellationToken propagation per coding standards
- Fail-closed: if Redis is unavailable for rate limiting/quota, requests are rejected (503)

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3", "1.4", "1.5"] },
    { "id": 2, "tasks": ["2.1", "2.3", "3.1"] },
    { "id": 3, "tasks": ["2.2", "2.4", "2.5", "3.2", "3.3"] },
    { "id": 4, "tasks": ["3.4", "3.5", "3.6"] },
    { "id": 5, "tasks": ["3.7", "3.8", "5.1"] },
    { "id": 6, "tasks": ["5.2", "5.3", "5.4", "6.1"] },
    { "id": 7, "tasks": ["6.2", "6.3", "6.4", "7.1"] },
    { "id": 8, "tasks": ["7.2"] },
    { "id": 9, "tasks": ["7.3", "7.4", "7.5", "9.1"] },
    { "id": 10, "tasks": ["9.2", "9.3", "9.4"] },
    { "id": 11, "tasks": ["9.5", "10.1"] },
    { "id": 12, "tasks": ["10.2", "10.3", "11.1", "11.2", "11.3"] },
    { "id": 13, "tasks": ["12.1"] },
    { "id": 14, "tasks": ["12.2"] }
  ]
}
```
