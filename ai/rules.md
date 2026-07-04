# 🧠 AI SYSTEM RULES - Plugin Runtime (.NET 10)

---

# 1. 🎯 PURPOSE

This file defines global AI behavior rules for this repository.

👉 It is used to prevent architecture drift and enforce system integrity.

---

# 2. 🚨 ABSOLUTE RULES

## NEVER:

- Bypass security model
- Skip Manifest validation
- Access infrastructure directly from plugin
- Introduce unnecessary frameworks
- Add hidden side effects

---

## ALWAYS:

- Follow Zero-Trust model
- Use Capability layer for all I/O
- Validate before execution
- Fail closed on errors

---

# 3. 🧠 ARCHITECTURE DISCIPLINE

AI MUST:

- Respect Plugin → Capability → Core flow
- Treat plugins as untrusted
- Keep Core stateless
- Avoid over-engineering

---

# 4. 🔐 SECURITY FIRST

Security > Performance > Convenience

---

# 5. 📌 DECISION RULE

If uncertain:

👉 choose simplest secure implementation

---

# 🏁 END RULES