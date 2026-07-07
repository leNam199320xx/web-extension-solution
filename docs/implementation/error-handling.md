# 🚨 Error Handling & Exception Taxonomy

---

# 1. PURPOSE

Định nghĩa error codes, exception hierarchy, và error response format thống nhất cho toàn hệ thống.

---

# 2. DESIGN PRINCIPLES

- Fail closed: mọi error đều dẫn đến reject execution
- Structured errors: mọi response đều có format thống nhất
- No exception swallowing: log everything, hide nothing
- Security errors: không leak internal details ra client

---

# 3. ERROR RESPONSE FORMAT

## Standard error response:

```json
{
  "error": {
    "code": "MANIFEST_SIGNATURE_INVALID",
    "category": "Security",
    "message": "Manifest signature verification failed",
    "traceId": "abc-123",
    "timestamp": "2026-01-01T00:00:00Z"
  }
}
```

## HTTP status mapping:

| Category | HTTP Status |
|----------|-------------|
| Validation | 400 Bad Request |
| Security | 403 Forbidden |
| NotFound | 404 Not Found |
| Execution | 500 Internal Server Error |
| Timeout | 504 Gateway Timeout |
| ResourceLimit | 429 Too Many Requests |

---

# 4. ERROR CODE TAXONOMY

## 4.1 Manifest Errors (MFT-xxx)

| Code | Meaning |
|------|---------|
| MFT-001 | Manifest schema invalid |
| MFT-002 | Manifest not found |
| MFT-003 | Manifest expired |
| MFT-004 | Manifest version incompatible |
| MFT-005 | Manifest capability unknown |

## 4.2 Security Errors (SEC-xxx)

| Code | Meaning |
|------|---------|
| SEC-001 | Signature verification failed |
| SEC-002 | SHA-256 hash mismatch |
| SEC-003 | Plugin revoked |
| SEC-004 | Public key not found |
| SEC-005 | Signing algorithm unsupported |
| SEC-006 | Replay attack detected |

## 4.3 Capability Errors (CAP-xxx)

| Code | Meaning |
|------|---------|
| CAP-001 | Capability not granted |
| CAP-002 | Capability not registered |
| CAP-003 | Capability access denied |
| CAP-004 | Capability rate limit exceeded |

## 4.4 Execution Errors (EXE-xxx)

| Code | Meaning |
|------|---------|
| EXE-001 | Plugin execution timeout |
| EXE-002 | Plugin memory limit exceeded |
| EXE-003 | Plugin unhandled exception |
| EXE-004 | Plugin load failed |
| EXE-005 | Plugin assembly not found |
| EXE-006 | Plugin entry point missing |
| EXE-007 | Execution cancelled |

## 4.5 Infrastructure Errors (INF-xxx)

| Code | Meaning |
|------|---------|
| INF-001 | Database connection failed |
| INF-002 | Redis unavailable |
| INF-003 | Storage access failed |
| INF-004 | KMS key retrieval failed |

## 4.6 API Errors (API-xxx)

| Code | Meaning |
|------|---------|
| API-001 | Request validation failed |
| API-002 | Authentication required |
| API-003 | Authorization denied |
| API-004 | Plugin not found |
| API-005 | Rate limit exceeded |

---

# 5. EXCEPTION HIERARCHY

```csharp
// Base exception
public abstract class PluginRuntimeException : Exception
{
    public string ErrorCode { get; }
    public string Category { get; }
}

// Security
public class ManifestValidationException : PluginRuntimeException { }
public class SignatureVerificationException : PluginRuntimeException { }
public class HashMismatchException : PluginRuntimeException { }
public class PluginRevokedException : PluginRuntimeException { }

// Capability
public class CapabilityDeniedException : PluginRuntimeException { }
public class CapabilityNotFoundException : PluginRuntimeException { }

// Execution
public class PluginExecutionException : PluginRuntimeException { }
public class PluginTimeoutException : PluginRuntimeException { }
public class PluginMemoryLimitException : PluginRuntimeException { }
public class PluginLoadException : PluginRuntimeException { }

// Infrastructure
public class InfrastructureException : PluginRuntimeException { }
```

---

# 6. ERROR HANDLING RULES

## MUST:

- Catch exceptions at API boundary (middleware)
- Map to structured error response
- Include traceId in every error
- Log full exception internally
- Return sanitized message to client

## MUST NOT:

- Expose stack traces to client
- Expose internal paths or connection strings
- Swallow exceptions silently
- Use generic catch-all without logging

---

# 7. MIDDLEWARE ERROR HANDLER

```csharp
public class GlobalExceptionMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (PluginRuntimeException ex)
        {
            // Map to structured response with correct HTTP status
        }
        catch (Exception ex)
        {
            // Log unexpected error
            // Return 500 with generic message
        }
    }
}
```

---

# 🏁 END
