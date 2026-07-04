# 📡 Event Model
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

Defines the event-driven architecture used in the system.

It covers:

- System events
- Plugin events
- Security events
- Audit events

---

# 2. CORE PRINCIPLE

> Everything important in the system is an event.

Events are:

- Immutable
- Append-only
- Traceable
- Replayable (optional)

---

# 3. EVENT TYPES

## 3.1 System Events

- RuntimeStarted
- RuntimeStopped
- NodeScaled
- NodeFailed

---

## 3.2 Plugin Events

- PluginUploaded
- PluginApproved
- PluginRejected
- PluginExecuted
- PluginFailed

---

## 3.3 Security Events

- ManifestValidated
- SignatureVerified
- CapabilityGranted
- CapabilityDenied
- SecurityViolationDetected

---

## 3.4 Execution Events

- ExecutionStarted
- ExecutionCompleted
- ExecutionTimeout
- ExecutionCancelled

---

# 4. EVENT FLOW

```
Runtime → Event Generator → Event Store → Observability Pipeline
```

---

# 5. EVENT STORE MODEL

Events are:

- Append-only
- Immutable
- Time-ordered

Stored in:

- PostgreSQL (primary)
- Event streaming system (future: Kafka / RabbitMQ)

---

# 6. EVENT STRUCTURE

Each event contains:

```
EventId
EventType
Timestamp
CorrelationId
TenantId
Payload
Metadata
```

---

# 7. CORRELATION MODEL

All events MUST share:

- CorrelationId (request-level tracing)
- ExecutionId (runtime-level tracing)

---

# 8. SECURITY EVENTS RULE

Security events are:

- Highest priority
- Never discarded
- Always audited

---

# 9. EVENT CONSUMERS

Consumers include:

- Observability system
- Audit system
- Monitoring dashboards
- Future AI analytics engine

---

# 10. DESIGN PRINCIPLE

> Events are the truth of the system.

Not state, not cache, not logs.

---

# 🏁 END OF EVENT MODEL