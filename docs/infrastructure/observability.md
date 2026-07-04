# 📊 Observability Model - Plugin Runtime System (.NET 10)

---

# 1. 🎯 MỤC TIÊU

Hệ thống Observability đảm bảo:

- Theo dõi toàn bộ vòng đời plugin execution
- Truy vết lỗi, performance, security events
- Hỗ trợ debugging production issues
- Đảm bảo audit compliance cho zero-trust system

---

# 2. 🧠 OBSERVABILITY TRIAD

Hệ thống dựa trên 3 trụ cột:

```
Logs  → What happened?
Metrics → How well did it perform?
Traces → Where did it happen?
```

---

# 3. 📌 CORE OBSERVABILITY FIELDS

Mọi plugin execution MUST include:

- TraceId
- SpanId
- PluginId
- ExtensionId
- UserId (optional)
- ExecutionId

---

# 4. 🔁 DISTRIBUTED TRACING MODEL

## Flow:

```
API Request
    ↓
Middleware (TraceId 생성)
    ↓
Plugin Execution Pipeline
    ↓
Capability Calls
    ↓
Infrastructure Calls
    ↓
Response
```

---

## Trace propagation rule:

> TraceId MUST be passed through every layer

---

# 5. 🧾 STRUCTURED LOGGING

## Format:

All logs MUST be JSON structured.

---

## Example:

```json
{
  "timestamp": "2026-01-01T00:00:00Z",
  "trace_id": "abc-123",
  "plugin_id": "payment-service",
  "event": "execution_started",
  "level": "info",
  "message": "Plugin execution started"
}
```

---

## Log levels:

- INFO → normal execution flow
- WARN → abnormal behavior
- ERROR → execution failure
- CRITICAL → security violation

---

# 6. 📊 METRICS MODEL

## Core metrics:

### Execution metrics:

- plugin_execution_duration_ms
- plugin_execution_success_rate
- plugin_execution_failure_rate

---

### Resource metrics:

- plugin_memory_usage_mb
- plugin_cpu_usage_ms
- plugin_timeout_count

---

### Security metrics:

- invalid_signature_attempts
- capability_denied_requests
- revoked_plugin_execution_attempts

---

# 7. 🔍 DISTRIBUTED TRACING DETAILS

## Span hierarchy:

```
API Request Span
   ├── Manifest Validation Span
   ├── Capability Resolution Span
   ├── Plugin Execution Span
   │       ├── Database Capability Span
   │       ├── Network Capability Span
   └── Response Span
```

---

## Each span MUST include:

- Start time
- End time
- Duration
- Status
- Error (if any)

---

# 8. 🚨 SECURITY OBSERVABILITY

## Tracked security events:

### 1. Signature violations
- invalid_signature
- tampered_manifest

---

### 2. Capability violations
- unauthorized_db_access
- unauthorized_network_access

---

### 3. Runtime abuse
- timeout_exceeded
- memory_limit_exceeded
- infinite_loop_detected

---

## Rule:

👉 All security events are immutable logs

---

# 9. 🧯 AUDIT LOGGING (IMMUTABLE)

## Properties:

- Append-only
- Cannot be modified
- Stored separately from application logs

---

## Audit log example:

```json
{
  "audit_id": "audit-001",
  "plugin_id": "payment-service",
  "action": "plugin_loaded",
  "result": "success",
  "performed_by": "system",
  "timestamp": "2026-01-01T00:00:00Z"
}
```

---

# 10. ⚙️ LOG PIPELINE ARCHITECTURE

```
Application Logs
       ↓
Structured Logger
       ↓
Log Enrichment Layer
       ↓
Trace Correlation Engine
       ↓
Storage (ELK / OpenTelemetry / Loki)
```

---

# 11. 🔥 ERROR TRACKING MODEL

## Every error MUST include:

- TraceId
- PluginId
- Stack trace (sanitized)
- Execution context
- Capability involved (if any)

---

## Example:

```json
{
  "error": "CapabilityDeniedException",
  "plugin_id": "payment-service",
  "capability": "DatabaseCapability",
  "trace_id": "abc-123",
  "message": "Plugin attempted unauthorized DB access"
}
```

---

# 12. ⏱ PERFORMANCE MONITORING

## Monitored KPIs:

- P95 plugin execution time
- P99 latency per capability
- Memory usage distribution
- Timeout frequency

---

# 13. 📡 REAL-TIME MONITORING

System SHOULD support:

- Live plugin execution dashboard
- Active plugin count
- Failure rate alerts
- Security incident alerts

---

# 14. 🚨 ALERTING RULES

## Trigger alerts when:

- Signature validation failures spike
- Plugin execution failure rate > threshold
- Memory limit exceeded frequently
- Unauthorized access attempts detected

---

# 15. 🧠 DESIGN PRINCIPLES

- Observability is mandatory, not optional
- Logs must be structured
- Trace must propagate everywhere
- Security events are first-class citizens

---

# 16. 🎯 FINAL GOAL

👉 Enable full visibility into:

- What plugin ran
- What it did
- What it accessed
- How it behaved
- Whether it violated security rules

---

# 🏁 END OF OBSERVABILITY MODEL