# 🔄 Migration Strategy
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

Defines how database schema evolves safely over time.

---

# 2. CORE PRINCIPLE

> Migrations must be backward-compatible or explicitly versioned.

No destructive changes without controlled rollout.

---

# 3. MIGRATION MODEL

The system uses:

- EF Core migrations (.NET 10)
- Versioned migration scripts
- Blue/Green deployment support

---

# 4. MIGRATION TYPES

## 4.1 Additive Migration (Safe)

- Add new tables
- Add new columns
- Extend JSON fields

No downtime required.

---

## 4.2 Transformative Migration (Risky)

- Data restructuring
- Column type changes
- Requires dual-write strategy

---

## 4.3 Destructive Migration (Restricted)

- Drop columns/tables
- Requires ADR approval
- Requires full backup

---

# 5. MIGRATION FLOW

```
Dev → Migration Script → CI Validation → Staging → Production Rollout
```

---

# 6. ZERO DOWNTIME RULE

All migrations MUST support:

- Backward compatibility
- Rolling deployment
- Safe rollback

---

# 7. ROLLBACK STRATEGY

Rollback supported via:

- Previous migration version
- Snapshot restore (if needed)
- Feature flags

---

# 8. DATABASE VERSIONING

Each schema version is tracked:

```
SchemaVersion
- Version
- AppliedAt
- Description
```

---

# 9. DISTRIBUTED SYSTEM RULE

Migrations must:

- Not lock critical tables for long periods
- Support horizontal scaling nodes
- Be idempotent

---

# 10. DESIGN PRINCIPLE

> Migration is a controlled evolution, not a rewrite.

---

# 🏁 END OF MIGRATION STRATEGY