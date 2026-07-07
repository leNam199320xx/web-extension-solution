# 📊 Observability Model - Plugin Runtime System (.NET 10)

---

# 1. PURPOSE

The observability system ensures:
- Full visibility into plugin execution lifecycle
- Error tracing, performance tracking, security event monitoring
- Production debugging support
- Audit compliance for zero-trust system

---

# 2. THREE PILLARS

| Pillar | Question it answers |
|--------|-------------------|
| Logs | What happened? |
| Metrics | How well did it perform? |
| Traces | Where did it flow? |

---

# 3. CORE FIELDS (Required on Every Execution)

- TraceId (distributed trace)
- SpanId (current span)
- ExecutionId (unique per execution)
- PluginId
- Version
- TenantId (if multi-tenant)
- UserId (if authenticated)

---

# 4. DISTRIBUTED TRACING

## Span hierarchy:

```
API Request Span
  ├── Authentication Span
  ├── Security Validation Span
  │     ├── Signature Verification Span
  │     └── Hash Verification Span
  ├── Capability Resolution Span
  ├── Plugin Execution Span
  │     ├── Database Capability Span
  │     └── Network Capability Span
  └── Response Span
```

Each span includes: start time, end time, duration, status, error (if any).

Rule: TraceId MUST propagate through every layer.

---

# 5. STRUCTURED LOGGING

All logs MUST be JSON structured:

```json
{
  "timestamp": "2026-01-01T00:00:00Z",
  "level": "Information",
  "traceId": "abc-123",
  "executionId": "exec-456",
  "pluginId": "payment-service",
  "event": "execution_started",
  "message": "Plugin execution started",
  "properties": {}
}
```

Log levels:
- **Information** — normal execution flow
- **Warning** — abnormal but recoverable behavior
- **Error** — execution failure
- **Critical** — security violation, system failure

---

# 6. METRICS

## Execution metrics:

| Metric | Type | Description |
|--------|------|-------------|
| `plugin_execution_duration_ms` | Histogram | Execution time distribution |
| `plugin_execution_total` | Counter | Total executions (labeled by status) |
| `plugin_execution_active` | Gauge | Currently running executions |

## Resource metrics:

| Metric | Type | Description |
|--------|------|-------------|
| `plugin_memory_usage_mb` | Histogram | Memory per execution |
| `plugin_timeout_total` | Counter | Timeout count |

## Security metrics:

| Metric | Type | Description |
|--------|------|-------------|
| `security_signature_failures_total` | Counter | Invalid signature attempts |
| `security_capability_denied_total` | Counter | Capability access denials |
| `security_revoked_execution_attempts` | Counter | Attempts to run revoked plugins |

---

# 7. SECURITY EVENTS

Tracked and alerted:
- `invalid_signature` — signature verification failed
- `hash_mismatch` — SHA-256 doesn't match
- `capability_violation` — unauthorized access attempt
- `timeout_exceeded` — plugin ran past limit
- `revoked_plugin_attempt` — execution of revoked plugin

Rule: Security events are immutable logs, never dropped, always alerted.

---

# 8. AUDIT LOGGING

Properties:
- Append-only (immutable)
- Cannot be modified or deleted
- Stored separately from application logs (dedicated table)
- Retained per compliance requirements

Example:

```json
{
  "auditId": "audit-001",
  "action": "PluginRevoked",
  "actor": "admin@company.com",
  "target": "payment-service:1.0.0",
  "result": "Success",
  "timestamp": "2026-01-01T00:00:00Z"
}
```

For audit table schema, see `docs/data/database-schema.md`.

---

# 9. LOG PIPELINE

```
Application → Serilog (structured) → OpenTelemetry Exporter → Collector → Storage
```

Storage options:
- Elasticsearch (ELK stack)
- Loki (Grafana stack)
- Azure Monitor / CloudWatch

---

# 10. ERROR TRACKING

Every error MUST include:
- TraceId + ExecutionId
- PluginId + Version
- Error code (see `docs/implementation/error-handling.md`)
- Sanitized message (no internals leaked)
- Capability involved (if applicable)

---

# 11. PERFORMANCE MONITORING (KPIs)

- P50, P95, P99 plugin execution time
- P99 latency per capability type
- Timeout frequency per plugin
- Memory usage distribution

---

# 12. ALERTING RULES

Trigger alerts when:
- Signature validation failures > 5/minute
- Plugin execution failure rate > 10%
- Timeout rate > 5%
- Unauthorized access attempts detected
- System memory pressure high

---

# 13. IMPLEMENTATION (ASP.NET Core)

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("PluginRuntime")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter("PluginRuntime")
        .AddOtlpExporter());

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.OpenTelemetry());
```

---

# 14. DESIGN PRINCIPLE

> Observability is mandatory, not optional.
> Every execution is fully traceable.
> Security events are first-class citizens.

---

# 🏁 END
