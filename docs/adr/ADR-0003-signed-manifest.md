# ADR-0003

Status: Accepted

---

# TITLE

Use Signed Manifest instead of Trusting Plugin Metadata

---

# CONTEXT

Plugins cannot define their own permissions.

Otherwise privilege escalation becomes possible.

---

# DECISION

Permissions exist only inside Signed Manifest.

Plugin binaries contain zero security metadata.

---

# CONSEQUENCES

Pros

Immutable trust chain

Centralized governance

Manifest versioning

Cons

Approval workflow required

---

# REFERENCES

manifest-spec.md

security-enforcement-spec.md