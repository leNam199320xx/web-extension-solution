# 📡 Event Model
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

Defines the event-driven architecture. Events record what happened in the system and feed observability, audit, and future analytics.

For observability integration, see `docs/infrastructure/observability.md`.
For audit requirements, see `docs/data/data-model.md` (AuditLog entity).

---

# 2. CORE PRINCIPLE

> Everything important in the system is an event.
> Events are immutable, append-only, and traceable.

---

# 3. PHASE 1 DECISION

**PostgreSQL-backed event storage** for Phase 1.

Events are written to the `audit_logs` table and optionally to a dedicated `events` table.
Future: migrate to event streaming (Kafka, RabbitMQ, or Azure Service Bus) when scale demands it.

---

# 4. EVENT BASE CLASS

```csharp
public abstract record DomainEvent
{
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public string EventType { get; init; } = "";
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string TraceId { get; init; } = "";
    public string? CorrelationId { get; init; }
    public string? TenantId { get; init; }
    public string? ActorId { get; init; }
}
```

---

# 5. EVENT TYPES

## 5.1 Plugin Lifecycle Events

```csharp
public record PluginUploadedEvent : DomainEvent
{
    public string PluginId { get; init; } = "";
    public string Version { get; init; } = "";
    public string UploadedBy { get; init; } = "";
}

public record PluginApprovedEvent : DomainEvent
{
    public string PluginId { get; init; } = "";
    public string Version { get; init; } = "";
    public string ApprovedBy { get; init; } = "";
}

public record PluginRejectedEvent : DomainEvent
{
    public string PluginId { get; init; } = "";
    public string Version { get; init; } = "";
    public string Reason { get; init; } = "";
}

public record PluginRevokedEvent : DomainEvent
{
    public string PluginId { get; init; } = "";
    public string Version { get; init; } = "";
    public string Reason { get; init; } = "";
    public string RevokedBy { get; init; } = "";
}
```

## 5.2 Execution Events

```csharp
public record ExecutionStartedEvent : DomainEvent
{
    public string ExecutionId { get; init; } = "";
    public string PluginId { get; init; } = "";
    public string Version { get; init; } = "";
}

public record ExecutionCompletedEvent : DomainEvent
{
    public string ExecutionId { get; init; } = "";
    public string PluginId { get; init; } = "";
    public int DurationMs { get; init; }
}

public record ExecutionFailedEvent : DomainEvent
{
    public string ExecutionId { get; init; } = "";
    public string PluginId { get; init; } = "";
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
}

public record ExecutionTimeoutEvent : DomainEvent
{
    public string ExecutionId { get; init; } = "";
    public string PluginId { get; init; } = "";
    public int TimeoutMs { get; init; }
}
```

## 5.3 Security Events

```csharp
public record SecurityViolationEvent : DomainEvent
{
    public string PluginId { get; init; } = "";
    public string ViolationType { get; init; } = "";
    public string Detail { get; init; } = "";
}

public record SignatureValidationFailedEvent : DomainEvent
{
    public string PluginId { get; init; } = "";
    public string Version { get; init; } = "";
}

public record CapabilityDeniedEvent : DomainEvent
{
    public string PluginId { get; init; } = "";
    public string Capability { get; init; } = "";
    public string Reason { get; init; } = "";
}
```

## 5.4 System Events

```csharp
public record RuntimeNodeStartedEvent : DomainEvent
{
    public string NodeId { get; init; } = "";
    public string Version { get; init; } = "";
}

public record RuntimeNodeStoppedEvent : DomainEvent
{
    public string NodeId { get; init; } = "";
    public string Reason { get; init; } = "";
}
```

---

# 6. EVENT PUBLISHER

```csharp
public interface IEventPublisher
{
    Task PublishAsync(DomainEvent @event, CancellationToken cancellationToken = default);
    Task PublishManyAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default);
}
```

Phase 1 implementation: writes directly to database.
Phase 2: publishes to message broker.

---

# 7. EVENT CONSUMERS

| Consumer | Purpose |
|----------|---------|
| Observability pipeline | Metrics, traces, dashboards |
| Audit system | Immutable audit trail |
| Alerting engine | Security incident notifications |
| Analytics (future) | Usage patterns, trend analysis |

---

# 8. CORRELATION MODEL

All events MUST include:
- `TraceId`: request-level tracing (propagated from API)
- `CorrelationId`: business-level grouping (optional, set by caller)
- `EventId`: unique per event

---

# 9. SECURITY EVENTS PRIORITY

Security events are:
- Highest priority (never dropped)
- Always persisted to audit log
- Trigger real-time alerting
- Never subject to sampling or throttling

---

# 10. DESIGN PRINCIPLE

> Events are the truth of the system.
> They record what happened, when, and why.
> They are immutable and always available for forensics.

---

# 🏁 END
