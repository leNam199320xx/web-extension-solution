# ADR-0004

Status: Accepted

---

# TITLE

Core Runtime Must Remain Stateless

---

# CONTEXT

Stateful runtimes complicate scaling.

---

# DECISION

Runtime owns execution only.

Persistent state belongs to external systems.

---

# CONSEQUENCES

Pros

Horizontal scaling

Easy deployment

Cloud native

Cons

Requires shared infrastructure

---

# REFERENCES

deployment-model.md