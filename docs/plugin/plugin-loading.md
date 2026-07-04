# 🔌 Plugin Loading Model
## Metadata-Driven Plugin Runtime (.NET 10)

---

# 1. PURPOSE

This document defines how plugins are loaded into the Core Runtime.

It covers:

- Loading pipeline
- Validation before load
- Assembly resolution
- Version handling
- Security enforcement points

---

# 2. CORE PRINCIPLE

> Plugins are NEVER trusted at load time.

Loading a plugin does NOT mean execution permission is granted.

Every plugin MUST pass full validation before being loaded.

---

# 3. LOADING PIPELINE

```
Request → Manifest Load → Validation → Isolation Setup → Assembly Load → Ready State
```

---

# 4. STEP-BY-STEP FLOW

## Step 1 — Plugin Request

Runtime receives:

- PluginId
- Version
- Execution Context

---

## Step 2 — Manifest Resolution

Load:

- Signed manifest
- Plugin metadata
- Version info

If not found:

→ Reject loading

---

## Step 3 — Security Validation

Mandatory checks:

- Signature verification
- SHA256 hash validation
- Version compatibility check
- Revocation status check

Failure:

→ Abort loading

---

## Step 4 — Dependency Resolution

Resolve:

- NuGet dependencies (if allowed)
- Shared libraries
- Runtime dependencies

Rules:

- Only approved dependencies allowed
- No dynamic external download at runtime

---

## Step 5 — Isolation Preparation

Before loading:

- Create isolated context
- Assign memory limits
- Prepare execution sandbox

---

## Step 6 — Assembly Loading

Load using:

- AssemblyLoadContext (default or isolated)
- OR container runtime (future)
- OR WASM runtime (future extension)

Rules:

- No global assembly injection
- No shared static state

---

## Step 7 — Plugin Registration

Register:

- Plugin metadata
- Capabilities
- Entry point (HandleAsync)

---

## Step 8 — Ready State

Plugin becomes:

```
Loaded → Not Executed yet
```

---

# 5. LOADING RULES

- No execution during loading phase
- No network access during load
- No capability access during load
- Load is deterministic and repeatable

---

# 6. ERROR HANDLING

If any step fails:

- Plugin is NOT loaded
- State is NOT cached
- Audit log is written
- Error is returned

---

# 7. PERFORMANCE CONSTRAINTS

Target:

- Cold load < 500ms
- Warm load < 100ms

---

# 8. SECURITY PRINCIPLE

> Loading a plugin is a privileged operation, but NOT a trusted operation.

---

# 🏁 END OF PLUGIN LOADING MODEL