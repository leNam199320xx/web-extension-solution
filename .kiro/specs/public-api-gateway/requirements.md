# Requirements Document

## Introduction

The Public API Gateway is the public-facing entry point for paying customers (small websites and systems) to access the Plugin Runtime platform. It sits in front of the internal PluginRuntime.Api and handles API key authentication, rate limiting, quota enforcement, usage metering, request routing, and correlation ID propagation. The gateway does NOT handle billing or payments — it only meters usage and enforces quotas set by the separate Tenant Management & Billing system.

Architecture:
```
API Consumer (website) → Public API Gateway → PluginRuntime.Api → Plugin Execution
```

## Glossary

- **Gateway**: The Public API Gateway ASP.NET Core application that serves as the public entry point for API consumers
- **API_Consumer**: A paying customer (small website or system) that calls the Gateway using an API key to invoke plugins
- **Platform_Admin**: An internal user who manages tenants, plans, and API keys via the Admin Portal
- **API_Key**: A cryptographic token (prefixed string) issued to an API Consumer for authentication against the Gateway
- **Tenant**: An organizational entity representing a paying customer account, associated with one or more API keys
- **Plan**: A subscription tier (Free, Pro, Enterprise) that defines rate limits and quotas for a Tenant
- **Rate_Limit**: The maximum number of requests a Tenant can make within a time window, enforced per Plan
- **Quota**: The total number of requests a Tenant is allowed within a billing period (daily), enforced per Plan
- **Usage_Record**: A structured log entry capturing details of each API call made by a Tenant for billing purposes
- **Correlation_ID**: A unique identifier (UUID) propagated across the Gateway and PluginRuntime.Api for end-to-end request tracing
- **PluginRuntime_Api**: The internal API that handles plugin execution, plugin management, and other core runtime operations
- **Redis**: In-memory data store used for rate limiting counters, quota counters, and API key caching
- **PostgreSQL**: Relational database storing Tenant, API key, Plan, and usage metering data

## Requirements

### Requirement 1: API Key Authentication

**User Story:** As an API Consumer, I want to authenticate my requests using an API key, so that the Gateway can identify my Tenant and authorize access to the platform.

#### Acceptance Criteria

1. WHEN a request is received with a valid API key in the `X-Api-Key` header, THE Gateway SHALL authenticate the request and associate it with the corresponding Tenant within 100 milliseconds under normal operating conditions
2. IF a request is received without an `X-Api-Key` header, THEN THE Gateway SHALL reject the request with HTTP 401 and an error response containing code "GW-AUTH-001"
3. IF a request is received with an invalid or expired API key, THEN THE Gateway SHALL reject the request with HTTP 401 and an error response containing code "GW-AUTH-002"
4. IF a request is received with a revoked API key, THEN THE Gateway SHALL reject the request with HTTP 403 and an error response containing code "GW-AUTH-003"
5. THE Gateway SHALL cache validated API key lookups in Redis with a configurable TTL (default: 300 seconds, minimum: 30 seconds, maximum: 3600 seconds) to reduce database load
6. WHEN an API key is validated, THE Gateway SHALL resolve the associated Tenant and Plan for use in downstream rate limiting and quota enforcement
7. WHEN an API key is revoked, THE Gateway SHALL invalidate the corresponding cache entry so that subsequent requests using that key are rejected without waiting for TTL expiry
8. IF the Redis cache is unavailable, THEN THE Gateway SHALL fall back to direct database lookup for API key validation and continue processing requests

### Requirement 2: Rate Limiting

**User Story:** As a Platform Admin, I want to enforce rate limits per Plan tier, so that no single Tenant can overwhelm the platform and degrade service for others.

#### Acceptance Criteria

1. WHILE a Tenant is on the Free plan, THE Gateway SHALL enforce a rate limit of 100 requests per rolling 24-hour sliding window
2. WHILE a Tenant is on the Pro plan, THE Gateway SHALL enforce a rate limit of 10,000 requests per rolling 24-hour sliding window
3. WHILE a Tenant is on the Enterprise plan, THE Gateway SHALL enforce no rate limit
4. THE Gateway SHALL use Redis as the backing store for rate limit counters using a sliding window algorithm
5. WHEN a request is received, THE Gateway SHALL check the Tenant rate limit counter before forwarding the request to PluginRuntime_Api
6. WHEN a Tenant exceeds their rate limit, THE Gateway SHALL reject the request with HTTP 429 and an error response containing code "GW-RATE-001"
7. THE Gateway SHALL include `X-RateLimit-Limit`, `X-RateLimit-Remaining`, and `X-RateLimit-Reset` headers in every response including HTTP 429 rejection responses, where `X-RateLimit-Reset` is a Unix timestamp in seconds indicating when the oldest request in the window expires
8. IF Redis is unreachable when checking rate limits, THEN THE Gateway SHALL reject the request with HTTP 503 and an error response containing code "GW-RATE-002"

### Requirement 3: Quota Enforcement

**User Story:** As a Platform Admin, I want to enforce daily quotas per Tenant based on their Plan, so that usage stays within contracted limits and the billing system can charge accurately.

#### Acceptance Criteria

1. WHEN a Tenant's total API request count for the current UTC day reaches or exceeds the daily request limit defined by their Plan, THE Gateway SHALL reject the request with HTTP 429 and an error response containing code "GW-QUOTA-001"
2. WHEN THE Gateway returns an HTTP 429 response due to quota exhaustion, THE Gateway SHALL include a `Retry-After` header containing the number of seconds (as an integer) remaining until the next quota reset at 00:00:00 UTC
3. THE Gateway SHALL reset quota counters for all Tenants at 00:00:00 UTC each day
4. THE Gateway SHALL persist quota counters in a shared store with atomic increment operations so that concurrent requests from the same Tenant produce a consistent count across all Gateway instances
5. WHEN a Tenant Plan is changed by the billing system, THE Gateway SHALL apply the new daily request limit within 60 seconds while preserving the Tenant's accumulated request count for the current UTC day
6. IF the quota counter store is unavailable, THEN THE Gateway SHALL reject incoming requests with HTTP 503 and an error response containing code "GW-QUOTA-002"

### Requirement 4: Usage Metering

**User Story:** As a Platform Admin, I want to track every API call per Tenant, so that the billing system can generate accurate invoices based on actual usage.

#### Acceptance Criteria

1. WHEN a request completes (success or failure), THE Gateway SHALL record a Usage_Record containing: Tenant ID, API key ID, timestamp (UTC, ISO 8601 with millisecond precision), HTTP method, request path (truncated to 2048 characters if longer), response status code, response duration as an integer in milliseconds, and Correlation_ID
2. THE Gateway SHALL write Usage_Records asynchronously such that the metering operation adds no more than 5 milliseconds to request processing time at the 99th percentile
3. THE Gateway SHALL persist Usage_Records to PostgreSQL and retain them for a minimum of 90 days
4. IF the Usage_Record persistence fails, THEN THE Gateway SHALL buffer records in memory up to a maximum of 10,000 records and retry with exponential backoff starting at 1 second and doubling per attempt up to a maximum of 3 attempts
5. THE Gateway SHALL NOT reject incoming requests due to Usage_Record persistence failures
6. IF all 3 retry attempts are exhausted for a buffered Usage_Record, THEN THE Gateway SHALL write the failed records to a local dead-letter log and emit an alert event indicating the count of lost records and the affected Tenant IDs
7. IF the in-memory buffer reaches its 10,000-record capacity, THEN THE Gateway SHALL discard the oldest buffered records first and emit an alert event indicating buffer overflow with the count of discarded records

### Requirement 5: Request Routing

**User Story:** As an API Consumer, I want my authenticated requests forwarded to the PluginRuntime_Api, so that I can invoke plugins and access platform functionality.

#### Acceptance Criteria

1. WHEN a request passes authentication, rate limiting, and quota checks, THE Gateway SHALL forward the request to the PluginRuntime_Api preserving the original HTTP method, path, query parameters, headers, and body
2. WHEN forwarding a request to PluginRuntime_Api, THE Gateway SHALL add an internal service-to-service authorization header containing a short-lived token issued via the OAuth 2.0 client credentials flow
3. WHEN forwarding a request to PluginRuntime_Api, THE Gateway SHALL extract the Tenant ID from the authenticated API key claims and include it in the `X-Tenant-Id` header
4. WHEN forwarding a request to PluginRuntime_Api, THE Gateway SHALL remove the original `X-Api-Key` header and any hop-by-hop headers (Connection, Keep-Alive, Transfer-Encoding, Upgrade) before sending the request upstream
5. WHEN PluginRuntime_Api returns a response, THE Gateway SHALL forward the response to the API Consumer preserving the status code, headers, and body
6. IF PluginRuntime_Api is unreachable or returns a 5xx error within the configured timeout, THEN THE Gateway SHALL return HTTP 502 with an error response containing code "GW-UPSTREAM-001"
7. THE Gateway SHALL enforce a configurable request timeout for upstream calls to PluginRuntime_Api with a default of 30 seconds and a maximum configurable value of 300 seconds

### Requirement 6: Correlation ID Propagation

**User Story:** As a Platform Admin, I want every request traced end-to-end with a unique Correlation ID, so that I can troubleshoot issues across the Gateway and PluginRuntime_Api.

#### Acceptance Criteria

1. WHEN a request is received without an `X-Correlation-Id` header, THE Gateway SHALL generate a new UUID v4 and use it as the Correlation_ID for the request
2. WHEN a request is received with an `X-Correlation-Id` header containing a value that is between 1 and 128 characters in length and consists only of printable ASCII characters (codes 33–126), THE Gateway SHALL use the provided value as the Correlation_ID
3. IF a request is received with an `X-Correlation-Id` header that is empty or exceeds 128 characters or contains non-printable characters, THEN THE Gateway SHALL discard the provided value, generate a new UUID v4, and use it as the Correlation_ID
4. THE Gateway SHALL propagate the Correlation_ID to PluginRuntime_Api via the `X-Correlation-Id` header on the forwarded request
5. THE Gateway SHALL include the Correlation_ID in all log entries associated with the request
6. THE Gateway SHALL return the Correlation_ID in the `X-Correlation-Id` response header to the API Consumer for both success and error responses

### Requirement 7: Error Response Format

**User Story:** As an API Consumer, I want consistent, structured error responses from the Gateway, so that I can programmatically handle errors in my integration.

#### Acceptance Criteria

1. THE Gateway SHALL return all error responses in the standard error format: `{ "error": { "code": "string", "category": "string", "message": "string", "traceId": "string", "timestamp": "ISO 8601" } }` with Content-Type `application/json`
2. THE Gateway SHALL use the category "Gateway" for all errors originating from the Gateway itself, and SHALL prefix all Gateway-originated error codes with "GW-"
3. WHEN forwarding an error response from PluginRuntime_Api that conforms to the standard error format, THE Gateway SHALL preserve the original error response body without modification
4. IF PluginRuntime_Api returns an error response that does not conform to the standard error format or is not valid JSON, THEN THE Gateway SHALL return a Gateway-originated error with code "GW-UPSTREAM-002" and category "Gateway" indicating an upstream response parsing failure
5. THE Gateway SHALL NOT expose internal implementation details, stack traces, infrastructure hostnames, file paths, or connection strings in error responses
6. THE Gateway SHALL include the Correlation_ID as the `traceId` field in all Gateway-originated error responses
7. THE Gateway SHALL return the `timestamp` field in UTC using ISO 8601 format with seconds precision (e.g., "2026-01-15T10:30:00Z")
8. THE Gateway SHALL limit the `message` field in Gateway-originated error responses to a maximum of 500 characters

### Requirement 8: Health and Observability

**User Story:** As a Platform Admin, I want health check endpoints and observability signals from the Gateway, so that I can monitor availability and diagnose issues.

#### Acceptance Criteria

1. WHEN a client sends a `GET /health` request and all dependencies (Redis, PostgreSQL, PluginRuntime_Api) respond successfully within 5 seconds, THE Gateway SHALL return HTTP 200 with a JSON body containing status "Healthy" and the individual status of each dependency
2. IF any dependency fails to respond within 5 seconds or returns an error, THEN THE Gateway SHALL return HTTP 503 with a JSON body containing status "Unhealthy" and the name and status of each failing dependency
3. WHEN a client sends a `GET /ready` request and the Gateway has completed startup initialization, verified at least one successful health check cycle, and is bound to its listening port, THE Gateway SHALL return HTTP 200 with status "Ready"
4. IF the Gateway has not completed startup initialization or the most recent health check cycle detected an unhealthy dependency, THEN THE Gateway SHALL return HTTP 503 on the `GET /ready` endpoint with status "Not Ready"
5. THE Gateway SHALL emit OpenTelemetry traces for every request including spans for authentication, rate limiting, quota check, and upstream forwarding
6. THE Gateway SHALL emit structured JSON logs for every request containing at minimum: timestamp (ISO 8601), level, traceId, spanId, tenantId, method, path, statusCode, and durationMs
7. THE Gateway SHALL expose Prometheus-compatible metrics at `GET /metrics` including: total requests, requests per Tenant, error rates, and p50/p95/p99 latency

### Requirement 9: Security Hardening

**User Story:** As a Platform Admin, I want the Gateway hardened against common attack vectors, so that the platform remains secure under the Zero-Trust model.

#### Acceptance Criteria

1. WHEN a plain HTTP request is received, THE Gateway SHALL reject the connection with HTTP 421 and error code "GW-SEC-004" without processing the request body
2. THE Gateway SHALL validate that API keys match a predefined format pattern (alphanumeric, 32 to 128 characters, no special characters beyond hyphens and underscores) before performing database lookups to prevent injection attacks
3. IF an API key fails format validation, THEN THE Gateway SHALL reject the request with HTTP 400 and error code "GW-SEC-005" without performing a database lookup
4. THE Gateway SHALL enforce a maximum request body size (configurable, default 10 MB) and reject oversized requests with HTTP 413 and error code "GW-SEC-001"
5. THE Gateway SHALL enforce a maximum total request header size (configurable, default 8 KB) and reject requests exceeding the limit with HTTP 431 and error code "GW-SEC-002"
6. WHEN more than 10 consecutive failed authentication attempts occur from the same IP within 60 seconds, THE Gateway SHALL temporarily block that IP for 5 minutes and return HTTP 429 with error code "GW-SEC-003"
7. THE Gateway SHALL NOT log API key values in plain text; keys SHALL be masked showing only the last 4 characters (e.g., "****abcd") in all log outputs
8. THE Gateway SHALL enforce HTTPS (TLS 1.2 or higher) for all inbound connections
