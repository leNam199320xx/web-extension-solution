# 🌐 Runtime API Specification (.NET 10)

---

# 1. 🎯 PURPOSE

Defines API surface for executing plugins.

---

# 2. 🚀 BASE ENDPOINTS

## Execute Plugin

```
POST /api/v1/execute/{pluginId}
```

---

### Request

```json
{
  "input": {},
  "metadata": {
    "traceId": "abc-123"
  }
}
```

---

### Response

```json
{
  "success": true,
  "data": {},
  "traceId": "abc-123"
}
```

---

# 3. 📦 PLUGIN MANAGEMENT

## List plugins

```
GET /api/v1/plugins
```

---

## Reload plugin

```
POST /api/v1/plugins/reload/{pluginId}
```

---

## Revoke plugin

```
POST /api/v1/plugins/revoke/{pluginId}
```

---

# 4. 🔁 EXECUTION FLOW (API SIDE)

```
Request → Auth Middleware
        → TraceId generation
        → PluginExecutor
        → Runtime Engine
        → Response mapping
```

---

# 5. 🔐 ERROR FORMAT

```json
{
  "error": "CapabilityDenied",
  "message": "Access denied to DatabaseCapability",
  "traceId": "abc-123"
}
```

---

# 6. ⏱ RULES

- All requests MUST be traced
- All executions MUST be logged
- All failures MUST return structured error

---

# 7. 🧠 DESIGN PRINCIPLE

> API layer is ONLY a gateway, never business logic

---

# 🏁 END SPEC