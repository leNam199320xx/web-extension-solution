# 🚫 ANTI-PATTERNS - Plugin Runtime System

---

# 1. ❌ ARCHITECTURE ANTI-PATTERNS

## NEVER:

- Business logic inside Core API
- Plugins directly accessing DB
- Mixing infrastructure + domain logic
- Over-layered architecture (DDD overkill)

---

# 2. ❌ SECURITY ANTI-PATTERNS

- Skipping Manifest validation
- Trusting plugin input
- Allowing direct system calls
- Hardcoded secrets in plugin

---

# 3. ❌ .NET ANTI-PATTERNS

- Blocking async calls
- Global static state in Core
- Missing CancellationToken
- Excessive reflection usage

---

# 4. ❌ DESIGN ANTI-PATTERNS

- Over-abstraction
- Unnecessary patterns (Factory everywhere)
- God services
- Tight coupling between plugin and core

---

# 5. ❌ RUNTIME ANTI-PATTERNS

- No timeout enforcement
- No resource limits
- No execution isolation
- No observability tracking

---

# 6. 🎯 RULE OF THUMB

If it increases complexity without improving security or clarity → DO NOT DO IT

---

# 🏁 END ANTI-PATTERNS