# 🗄 Database Schema
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

Defines the physical database schema for PostgreSQL. For the logical data model, see `docs/data/data-model.md`.

---

# 2. DESIGN PRINCIPLES

- Database is a system of record, not a system of logic
- No stored procedures or triggers for business logic
- Immutable audit trail
- Soft delete where applicable
- JSONB for flexible metadata fields

---

# 3. TABLES

## 3.1 plugins

```sql
CREATE TABLE plugins (
    plugin_id       UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(200)    NOT NULL UNIQUE,
    display_name    VARCHAR(500)    NOT NULL,
    description     TEXT,
    owner_id        UUID            NOT NULL,
    status          VARCHAR(50)     NOT NULL DEFAULT 'Active',
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    deleted_at      TIMESTAMPTZ
);

-- status: Active, Suspended, Archived
```

---

## 3.2 plugin_versions

```sql
CREATE TABLE plugin_versions (
    version_id          UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    plugin_id           UUID            NOT NULL REFERENCES plugins(plugin_id),
    version             VARCHAR(50)     NOT NULL,
    storage_uri         VARCHAR(2000)   NOT NULL,
    sha256              VARCHAR(64)     NOT NULL,
    entry_point         VARCHAR(500)    NOT NULL,
    entry_class         VARCHAR(500)    NOT NULL,
    status              VARCHAR(50)     NOT NULL DEFAULT 'Draft',
    approved_by         UUID,
    approved_at         TIMESTAMPTZ,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_plugin_version UNIQUE (plugin_id, version)
);

-- status: Draft, Scanning, Approved, Rejected, Revoked, Archived
```

---

## 3.3 manifests

```sql
CREATE TABLE manifests (
    manifest_id         UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    version_id          UUID            NOT NULL UNIQUE REFERENCES plugin_versions(version_id),
    manifest_version    VARCHAR(20)     NOT NULL DEFAULT '1.0',
    target_core_version VARCHAR(100)    NOT NULL,
    permissions         JSONB           NOT NULL DEFAULT '[]',
    capabilities        JSONB           NOT NULL DEFAULT '[]',
    execution_timeout_ms INT           NOT NULL DEFAULT 5000,
    max_memory_mb       INT             NOT NULL DEFAULT 256,
    max_cpu_ms          INT             NOT NULL DEFAULT 2000,
    allow_parallel      BOOLEAN         NOT NULL DEFAULT FALSE,
    signature           TEXT            NOT NULL,
    signature_algorithm VARCHAR(50)     NOT NULL DEFAULT 'RSA-SHA256',
    public_key_id       VARCHAR(200)    NOT NULL,
    issued_at           TIMESTAMPTZ     NOT NULL,
    expires_at          TIMESTAMPTZ     NOT NULL,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);
```

---

## 3.4 capabilities

```sql
CREATE TABLE capabilities (
    capability_id   UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(200)    NOT NULL UNIQUE,
    version         VARCHAR(50)     NOT NULL DEFAULT '1.0',
    category        VARCHAR(100)    NOT NULL,
    description     TEXT,
    enabled         BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- Seed data: DatabaseCapability, NetworkCapability, StorageCapability, CacheCapability
```

---

## 3.5 executions

```sql
CREATE TABLE executions (
    execution_id        UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    plugin_id           UUID            NOT NULL,
    version_id          UUID            NOT NULL,
    trace_id            VARCHAR(200)    NOT NULL,
    correlation_id      VARCHAR(200),
    tenant_id           VARCHAR(200),
    user_id             VARCHAR(200),
    status              VARCHAR(50)     NOT NULL DEFAULT 'Running',
    error_code          VARCHAR(50),
    error_message       TEXT,
    start_time          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    end_time            TIMESTAMPTZ,
    duration_ms         INT,
    node_id             VARCHAR(200)
);

-- status: Running, Completed, Failed, Cancelled, Timeout
```

---

## 3.6 audit_logs

```sql
CREATE TABLE audit_logs (
    audit_id        UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    timestamp       TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    actor_id        VARCHAR(200)    NOT NULL,
    actor_type      VARCHAR(50)     NOT NULL DEFAULT 'User',
    action          VARCHAR(200)    NOT NULL,
    resource_type   VARCHAR(100)    NOT NULL,
    resource_id     VARCHAR(200)    NOT NULL,
    ip_address      VARCHAR(50),
    result          VARCHAR(50)     NOT NULL,
    metadata        JSONB
);

-- This table is APPEND-ONLY. No UPDATE or DELETE allowed.
-- actor_type: User, System, Service
-- result: Success, Failure, Denied
```

---

## 3.7 revocations

```sql
CREATE TABLE revocations (
    revocation_id   UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    version_id      UUID            NOT NULL REFERENCES plugin_versions(version_id),
    reason          TEXT            NOT NULL,
    revoked_by      UUID            NOT NULL,
    revoked_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    expires_at      TIMESTAMPTZ
);
```

---

## 3.8 approvals

```sql
CREATE TABLE approvals (
    approval_id     UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    version_id      UUID            NOT NULL REFERENCES plugin_versions(version_id),
    reviewer_id     UUID            NOT NULL,
    decision        VARCHAR(50)     NOT NULL,
    comment         TEXT,
    decided_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- decision: Approved, Rejected, Pending
```

---

## 3.9 runtime_nodes

```sql
CREATE TABLE runtime_nodes (
    node_id         VARCHAR(200)    PRIMARY KEY,
    hostname        VARCHAR(500)    NOT NULL,
    version         VARCHAR(50)     NOT NULL,
    status          VARCHAR(50)     NOT NULL DEFAULT 'Active',
    started_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    last_heartbeat  TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- status: Active, Draining, Offline
```

---

## 3.10 extension_registry

```sql
CREATE TABLE extension_registry (
    extension_id        VARCHAR(200)    PRIMARY KEY,
    plugin_id           UUID            NOT NULL REFERENCES plugins(plugin_id),
    display_name        VARCHAR(500)    NOT NULL,
    description         TEXT,
    author_id           UUID            NOT NULL,
    visibility          VARCHAR(50)     NOT NULL DEFAULT 'Private',
    category            VARCHAR(100),
    latest_version      VARCHAR(50),
    total_versions      INT             NOT NULL DEFAULT 0,
    subscriber_count    INT             NOT NULL DEFAULT 0,
    invocation_policy   JSONB,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- visibility: Private, Public, Subscription
-- invocation_policy example:
-- {
--   "rate_limit_per_caller": 100,
--   "max_concurrent_callers": 10,
--   "timeout_ms": 3000,
--   "allowed_input_schema": { ... },
--   "response_schema": { ... }
-- }
```

---

## 3.11 extension_subscriptions

```sql
CREATE TABLE extension_subscriptions (
    subscription_id     UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    source_extension_id VARCHAR(200)    NOT NULL,
    target_extension_id VARCHAR(200)    NOT NULL REFERENCES extension_registry(extension_id),
    status              VARCHAR(50)     NOT NULL DEFAULT 'Requested',
    reason              TEXT,
    expected_usage      JSONB,
    conditions          TEXT,
    decided_by          UUID,
    decided_at          TIMESTAMPTZ,
    expires_at          TIMESTAMPTZ,
    revoked_at          TIMESTAMPTZ,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_subscription UNIQUE (source_extension_id, target_extension_id)
);

-- status: Requested, Approved, Rejected, Revoked, Expired
-- expected_usage example:
-- { "callsPerDay": 1000, "peakConcurrency": 10 }
```

---

## 3.12 permission_reviews

```sql
CREATE TABLE permission_reviews (
    review_id           UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    version_id          UUID            NOT NULL REFERENCES plugin_versions(version_id),
    permissions         JSONB           NOT NULL,
    risk_summary        JSONB           NOT NULL,
    permission_diff     JSONB,
    overall_risk_level  VARCHAR(50)     NOT NULL,
    reviewer_id         UUID,
    decision            VARCHAR(50),
    comment             TEXT,
    conditions          JSONB,
    decided_at          TIMESTAMPTZ,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- decision: Approved, ApprovedWithConditions, Rejected, NeedsInfo
-- overall_risk_level: Low, Medium, High, Critical
```

---

## 3.13 declarative_configs

```sql
CREATE TABLE declarative_configs (
    config_id       UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    extension_id    VARCHAR(200)    NOT NULL REFERENCES extension_registry(extension_id),
    version         VARCHAR(50)     NOT NULL,
    config          JSONB           NOT NULL,
    input_schema    JSONB,
    output_schema   JSONB,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_declarative_version UNIQUE (extension_id, version)
);

-- Stores the full .ext.json config for declarative extensions
-- config: the entire declarative extension definition
-- input_schema / output_schema: extracted for validation at runtime
```

---

# 4. INDEXES

```sql
-- plugins
CREATE INDEX idx_plugins_name ON plugins(name);
CREATE INDEX idx_plugins_status ON plugins(status) WHERE deleted_at IS NULL;

-- plugin_versions
CREATE INDEX idx_versions_plugin_id ON plugin_versions(plugin_id);
CREATE INDEX idx_versions_status ON plugin_versions(status);

-- manifests
CREATE INDEX idx_manifests_version_id ON manifests(version_id);
CREATE INDEX idx_manifests_expires_at ON manifests(expires_at);

-- executions
CREATE INDEX idx_executions_trace_id ON executions(trace_id);
CREATE INDEX idx_executions_plugin_version ON executions(plugin_id, version_id);
CREATE INDEX idx_executions_start_time ON executions(start_time DESC);
CREATE INDEX idx_executions_status ON executions(status) WHERE status = 'Running';

-- audit_logs
CREATE INDEX idx_audit_timestamp ON audit_logs(timestamp DESC);
CREATE INDEX idx_audit_action ON audit_logs(action);
CREATE INDEX idx_audit_resource ON audit_logs(resource_type, resource_id);

-- revocations
CREATE INDEX idx_revocations_version ON revocations(version_id);

-- extension_registry
CREATE INDEX idx_registry_visibility ON extension_registry(visibility);
CREATE INDEX idx_registry_author ON extension_registry(author_id);
CREATE INDEX idx_registry_category ON extension_registry(category);

-- extension_subscriptions
CREATE INDEX idx_subscriptions_source ON extension_subscriptions(source_extension_id);
CREATE INDEX idx_subscriptions_target ON extension_subscriptions(target_extension_id);
CREATE INDEX idx_subscriptions_status ON extension_subscriptions(status)
    WHERE status IN ('Requested', 'Approved');

-- permission_reviews
CREATE INDEX idx_permission_reviews_version ON permission_reviews(version_id);
CREATE INDEX idx_permission_reviews_decision ON permission_reviews(decision)
    WHERE decision IS NULL;

-- declarative_configs
CREATE INDEX idx_declarative_extension ON declarative_configs(extension_id);
```

---

# 5. CONSTRAINTS & RULES

- `audit_logs`: No UPDATE or DELETE operations (enforced at application level + optional DB rule)
- `plugin_versions`: version is immutable once status = Approved
- `manifests`: signature field cannot be updated after creation
- `revocations`: once created, cannot be deleted
- `extension_subscriptions`: unique constraint per source+target pair; status transitions are one-directional
- `permission_reviews`: one review per version_id (latest review is authoritative)
- `extension_registry`: visibility change requires re-review of all active subscriptions

---

# 6. EF CORE MAPPING NOTES

- Use `HasColumnType("jsonb")` for JSONB columns
- Use `ValueConverter` for enum-to-string mapping
- Use `HasQueryFilter(p => p.DeletedAt == null)` for soft-delete
- Configure `audit_logs` as insert-only entity (no `Update()` support)

---

# 🏁 END
