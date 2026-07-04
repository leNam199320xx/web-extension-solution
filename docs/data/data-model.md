# 🗄 Data Model Specification
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

This document defines the logical data model for the Metadata-Driven Plugin Runtime.

It serves as the Single Source of Truth for:

- Domain Entities
- Aggregate Roots
- Relationships
- Persistence
- Runtime Metadata

The data model is independent from any specific database engine.

---

# 2. DESIGN PRINCIPLES

The model follows:

- Explicit Ownership
- Immutable Versioning
- Soft Delete
- Auditability
- Event Friendly
- DDD-inspired Aggregates

---

# 3. DOMAIN OVERVIEW

The platform consists of the following domains.

```

Approval
│
├── Plugin
├── PluginVersion
├── Manifest
└── Approval

Runtime
│
├── Execution
├── Capability
├── RuntimeNode
└── Revocation

Administration
│
├── User
├── Role
├── AuditLog
└── Configuration

```

---

# 4. AGGREGATE ROOTS

Primary aggregates:

Plugin

PluginVersion

Manifest

Capability

Execution

AuditLog

---

# 5. ENTITY: Plugin

Represents one logical plugin.

Properties

```
PluginId (GUID)

Name

DisplayName

Description

Owner

Status

CreatedAt

UpdatedAt
```

Relationships

```
Plugin

↓

PluginVersion (1:N)
```

---

# 6. ENTITY: PluginVersion

Represents one immutable release.

Properties

```
PluginVersionId

PluginId

Version

StorageUri

SHA256

ManifestId

Status

ApprovedBy

CreatedAt
```

Status

```
Draft

Scanning

Approved

Rejected

Revoked

Archived
```

---

# 7. ENTITY: Manifest

Represents signed execution contract.

Properties

```
ManifestId

PluginVersionId

ManifestVersion

TargetCoreVersion

Permissions

Capabilities

ExecutionTimeout

MemoryLimit

CpuLimit

Signature

KeyId

IssuedAt

ExpiresAt
```

One PluginVersion

↓

One Manifest

---

# 8. ENTITY: Capability

Represents executable permissions.

Properties

```
CapabilityId

Name

Version

Category

Description

Enabled

CreatedAt
```

Examples

```
DatabaseCapability

StorageCapability

NetworkCapability

CacheCapability

QueueCapability
```

---

# 9. ENTITY: Execution

Represents one plugin execution.

Properties

```
ExecutionId

PluginVersionId

TraceId

CorrelationId

StartTime

EndTime

Duration

Status

ErrorCode

NodeId
```

Status

```
Running

Completed

Failed

Cancelled

Timeout
```

---

# 10. ENTITY: AuditLog

Stores immutable audit events.

Properties

```
AuditId

Timestamp

UserId

Action

Resource

ResourceId

IPAddress

Result

Metadata
```

Never updated.

Never deleted.

---

# 11. ENTITY: RuntimeNode

Represents one runtime instance.

Properties

```
NodeId

Hostname

Version

Status

StartedAt

LastHeartbeat
```

---

# 12. ENTITY: Revocation

Represents revoked plugins.

Properties

```
RevocationId

PluginVersionId

Reason

RevokedBy

RevokedAt

ExpiresAt
```

---

# 13. ENTITY: User

Administrative user.

Properties

```
UserId

Username

Email

DisplayName

Status
```

---

# 14. ENTITY: Role

RBAC Role.

Examples

```
Administrator

SecurityOfficer

Developer

Auditor
```

---

# 15. ENTITY: Approval

Approval workflow.

Properties

```
ApprovalId

PluginVersionId

Reviewer

Decision

Comment

ApprovedAt
```

Decision

```
Approved

Rejected

Pending
```

---

# 16. RELATIONSHIP DIAGRAM

```

Plugin
│
├────────────┐
│            │
▼            ▼

PluginVersion
│
├────────────┐
│            │
▼            ▼

Manifest     Approval
│
▼

Execution

Capability
▲
│

Manifest

AuditLog

```

---

# 17. DATABASE DESIGN

Recommended:

PostgreSQL

Reasons:

- JSONB support
- Transaction support
- Indexing
- Reliability
- Mature EF Core support

---

# 18. INDEXING

Recommended indexes.

Plugin

```
Name

Status
```

PluginVersion

```
PluginId

Version

Status
```

Manifest

```
PluginVersionId
```

Execution

```
TraceId

PluginVersionId

StartTime
```

AuditLog

```
Timestamp

Action

Resource
```

---

# 19. SOFT DELETE

Recommended.

Instead of delete:

```
DeletedAt

DeletedBy
```

Production audit history should remain intact.

---

# 20. VERSIONING

PluginVersion is immutable.

Never update.

Always create new version.

```
Plugin

↓

1.0

↓

1.1

↓

2.0

```

---

# 21. AUDIT STRATEGY

Audit every:

Approval

Signing

Deployment

Execution Failure

Revocation

Capability Change

Configuration Change

---

# 22. PERSISTENCE RULES

Plugin binaries

→ Object Storage

Manifest

→ Database + Storage

Audit

→ Database

Execution History

→ Database

Logs

→ Log System

Metrics

→ Metrics Backend

---

# 23. EVENTS

Future Event Bus

Examples

PluginApproved

PluginRevoked

PluginLoaded

PluginExecuted

PluginFailed

ManifestSigned

---

# 24. FUTURE EXTENSION

Data model should support:

Multi-Tenant

Plugin Marketplace

License Management

Billing

Usage Analytics

Quota Management

Remote Runtime

No schema redesign should be required.

---

# 25. DESIGN PRINCIPLES

Every entity:

Has immutable identity.

Every relationship:

Has explicit ownership.

Every state change:

Is auditable.

Every version:

Is immutable.

---

# 26. FINAL PRINCIPLE

The database stores facts.

The Runtime owns behavior.

The Manifest owns permissions.

The Capability System owns access control.

No component should violate these boundaries.

---

# 🏁 END OF DATA MODEL