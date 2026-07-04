# ADR-0002

Status: Accepted

---

# TITLE

Adopt Capability-Based Security

---

# CONTEXT

Plugins require controlled access to infrastructure.

Direct access creates excessive coupling and security risks.

---

# DECISION

Every infrastructure resource is exposed through capabilities.

Examples:

DatabaseCapability

StorageCapability

NetworkCapability

QueueCapability

---

# ALTERNATIVES

Reflection

Rejected

Reason:

Impossible to govern.

Service Locator

Rejected

Reason:

No permission boundary.

---

# CONSEQUENCES

Pros

Least Privilege

Easy auditing

Centralized enforcement

Cons

More abstraction

More interfaces

---

# REFERENCES

capability-system.md

manifest-spec.md