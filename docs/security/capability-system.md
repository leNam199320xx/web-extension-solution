# 🔑 Capability System - Secure Access Control Model (.NET 10)

---

# 1. PURPOSE

The Capability System ensures:
- Complete control over plugin resource access
- Elimination of direct infrastructure access
- Zero-Trust runtime execution

For capability interface contracts (code), see `docs/implementation/capability-interfaces.md`.
For manifest permission mapping, see `docs/plugin/manifest-spec.md`.

---

# 2. CORE IDEA

> Plugins NEVER access resources directly.

```
Plugin → Capability Interface → Core Proxy → Infrastructure
```

- Plugin = untrusted code
- Capability = controlled gateway
- Core = enforcement layer

---

# 3. ZERO TRUST RULE

**MUST:**
- All resource access goes through capabilities
- No "hidden access path" exists
- No direct dependency on DB / Network / OS from plugin code

**NEVER:**
- Plugin opens DB connection directly
- Plugin uses raw HttpClient
- Plugin accesses file system directly
- Plugin uses reflection to reach internal services

---

# 4. ARCHITECTURE

```
                ┌────────────────────┐
                │     Plugin Code    │
                └─────────┬──────────┘
                          │
                          ▼
            ┌──────────────────────────┐
            │   PluginExecutionContext │
            │   (Injected by Core)    │
            └─────────┬────────────────┘
                      │
        ┌─────────────┼─────────────┐──────────┐
        ▼             ▼             ▼           ▼
┌────────────┐ ┌────────────┐ ┌────────────┐ ┌──────────┐
│ Database   │ │ Network    │ │ Storage    │ │ Cache    │
│ Capability │ │ Capability │ │ Capability │ │Capability│
└────────────┘ └────────────┘ └────────────┘ └──────────┘
```

---

# 5. CAPABILITY ASSIGNMENT (From Manifest)

Capabilities are declared in the Signed Manifest:

```json
{
  "permissions": ["db:read", "network:outbound"],
  "capabilities": ["DatabaseCapability", "NetworkCapability"]
}
```

Rule: If not listed in manifest → capability is NOT injected into context.

---

# 6. CAPABILITY RESOLUTION FLOW

```
1. Load Manifest
2. Verify Signature (security pipeline)
3. Read permissions list
4. Map permissions → capability implementations
5. Create scoped capability instances
6. Inject into PluginExecutionContext
```

---

# 7. USAGE IN PLUGIN

```csharp
public async Task<PluginResult> Execute(IPluginExecutionContext context)
{
    var db = context.Capabilities["Database"] as IDatabaseCapability;
    var users = await db.QueryAsync<User>(
        "SELECT * FROM users WHERE active = @active",
        new { active = true },
        context.CancellationToken);

    return PluginResult.Ok(users);
}
```

---

# 8. SECURITY ENFORCEMENT

- Capability injection is read-only (plugin cannot modify)
- No runtime escalation allowed
- Each capability call is logged
- Rate limiting optional per capability
- Error isolation per plugin (one plugin's failure doesn't affect another)

---

# 9. FORBIDDEN BEHAVIORS

```csharp
// ❌ FORBIDDEN — direct access
new SqlConnection(connectionString);
new HttpClient().SendAsync(request);
File.ReadAllText(path);

// ❌ FORBIDDEN — bypass attempts
typeof(InternalService).GetMethod("Execute").Invoke(...)
ServiceLocator.GetService<IDbContext>()
```

---

# 10. EXTENSIBILITY

New capabilities can be added:
- `IQueueCapability`
- `INotificationCapability`
- Custom business capabilities

Adding a new capability requires:
1. Define interface in `Capabilities.Abstractions`
2. Implement in dedicated project
3. Register in Core DI container
4. Add to manifest schema validation
5. Update capability resolver

---

# 11. SECURITY GUARANTEE

- Plugin cannot escape sandbox via capabilities
- No capability = no access (deny by default)
- All access paths are deterministic and auditable
- Capabilities are scoped per plugin (namespaced data)

---

# 12. DESIGN PRINCIPLES

- Explicit > implicit
- Deny by default
- Least privilege always
- No hidden access paths
- Every access is logged

---

# 🏁 END
