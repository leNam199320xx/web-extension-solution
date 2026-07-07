# 🗄 Database Migration Strategy

---

# 1. PURPOSE

Định nghĩa cách quản lý database schema changes an toàn.

---

# 2. TOOL

- Entity Framework Core Migrations
- Code-first approach
- PostgreSQL target

---

# 3. MIGRATION COMMANDS

```bash
# Create new migration
dotnet ef migrations add <MigrationName> \
    --project src/Infrastructure/PluginRuntime.Infrastructure \
    --startup-project src/API/PluginRuntime.Api

# Apply migrations
dotnet ef database update \
    --project src/Infrastructure/PluginRuntime.Infrastructure \
    --startup-project src/API/PluginRuntime.Api

# Generate SQL script (for production)
dotnet ef migrations script \
    --project src/Infrastructure/PluginRuntime.Infrastructure \
    --startup-project src/API/PluginRuntime.Api \
    --idempotent \
    --output migrations.sql
```

---

# 4. MIGRATION RULES

## MUST:

- Every migration is idempotent
- Every migration is backward-compatible (support N-1 runtime version)
- Every migration has rollback capability
- Always generate SQL script for production (never auto-migrate in prod)
- Name migrations descriptively: `AddPluginStatusIndex`, `CreateExecutionTable`

## MUST NOT:

- Drop columns without deprecation period
- Rename columns (add new + migrate data + drop old)
- Run migrations directly in production EF Core CLI
- Delete data as part of schema migration

---

# 5. MIGRATION NAMING CONVENTION

Pattern: `{Timestamp}_{Action}{Entity}{Detail}`

Examples:
- `20260101_CreatePluginTable`
- `20260102_AddPluginStatusIndex`
- `20260103_CreateManifestTable`
- `20260104_AddExecutionTraceIdColumn`

---

# 6. PRODUCTION DEPLOYMENT

```
1. Generate idempotent SQL script
2. Review script in PR
3. Apply to staging
4. Validate staging behavior
5. Apply to production (during maintenance window if breaking)
6. Verify
```

---

# 7. ROLLBACK STRATEGY

Every migration file should have a corresponding `Down()`:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable("Plugins", ...);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable("Plugins");
}
```

For data migrations: use separate one-time scripts, not EF migrations.

---

# 8. INITIAL SCHEMA (Phase 1)

Tables to create in first migration:

1. `plugins` — Plugin registry
2. `plugin_versions` — Immutable version records
3. `manifests` — Signed manifest storage
4. `capabilities` — Registered capabilities
5. `executions` — Execution history
6. `audit_logs` — Immutable audit trail
7. `revocations` — Revoked plugins

---

# 9. INDEX STRATEGY

Create indexes for:

- `plugins(name, status)` — lookup by name
- `plugin_versions(plugin_id, version)` — version resolution
- `executions(trace_id)` — trace lookup
- `executions(plugin_version_id, start_time)` — history queries
- `audit_logs(timestamp, action)` — audit queries

---

# 🏁 END
