# 🗄 Database Schema
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

This document defines the logical database schema used by the platform.

It focuses on:

- Plugin metadata storage
- Execution tracking
- Security auditing
- Approval workflow

---

# 2. CORE PRINCIPLE

> Database is a system of record, not a system of logic.

No business logic is executed inside the database.

---

# 3. CORE TABLES

## 3.1 Plugin

Stores plugin identity.

```
Plugin
- PluginId (PK)
- Name
- Description
- OwnerId
- CreatedAt
```

---

## 3.2 PluginVersion

Stores versioned artifacts.

```
PluginVersion
- VersionId (PK)
- PluginId (FK)
- Version
- Sha256
- Status (Pending, Approved, Revoked)
- CreatedAt
```

---

## 3.3 Manifest

Signed metadata for execution.

```
Manifest
- ManifestId (PK)
- VersionId (FK)
- Permissions (JSON)
- Capabilities (JSON)
- Signature
- KeyId
```

---

## 3.4 Execution

Tracks plugin execution lifecycle.

```
Execution
- ExecutionId (PK)
- PluginId
- VersionId
- Status
- StartTime
- EndTime
- CorrelationId
```

---

## 3.5 AuditLog

Immutable audit trail.

```
AuditLog
- LogId (PK)
- ActorId
- Action
- Target
- Timestamp
- Metadata (JSON)
```

---

## 3.6 Approval

Approval workflow tracking.

```
Approval
- ApprovalId (PK)
- VersionId
- ReviewerId
- Status
- Comments
- Timestamp
```

---

# 4. RELATIONSHIPS

- Plugin → PluginVersion (1:N)
- PluginVersion → Manifest (1:1)
- PluginVersion → Execution (1:N)
- PluginVersion → Approval (1:N)

---

# 5. DESIGN PRINCIPLE

- Immutable execution history
- Append-only audit logs
- Versioned plugin artifacts
- No in-place mutation of critical records

---

# 🏁 END OF DATABASE SCHEMA