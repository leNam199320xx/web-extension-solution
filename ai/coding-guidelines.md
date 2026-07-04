# 🧱 CODING GUIDELINES - .NET 10 Plugin Runtime

---

# 1. 🎯 GOAL

Ensure consistent, production-grade code across Core + Plugins.

---

# 2. 🧠 CODE STYLE

## MUST:

- Clean, explicit code
- Minimal abstraction
- Small services
- Clear naming

---

## AVOID:

- Over-engineering
- Deep inheritance
- Generic frameworks without need

---

# 3. ⚙️ ARCHITECTURE RULES

- Core = orchestration only
- Plugins = untrusted execution units
- All access via Capability layer

---

# 4. 🔐 SECURITY RULES

- Validate input at boundaries
- Never trust plugin input
- Always enforce Manifest rules

---

# 5. 📦 .NET RULES

- Prefer async/await
- Use CancellationToken everywhere
- Avoid blocking calls
- Use dependency injection only in Core

---

# 6. 🚀 PERFORMANCE RULES

- Avoid unnecessary allocations
- Use pooling where needed
- Keep hot path minimal

---

# 🏁 END GUIDELINES