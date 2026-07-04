# 🔑 Capability System - Secure Access Control Model (.NET 10)

---

# 1. 🎯 MỤC TIÊU

Hệ thống Capability được thiết kế để:

- Kiểm soát toàn bộ quyền truy cập của plugin
- Loại bỏ hoàn toàn direct access tới infrastructure
- Thay thế permission model truyền thống bằng capability proxy
- Đảm bảo Zero-Trust runtime execution

---

# 2. 🧠 CORE IDEA

## Nguyên tắc cốt lõi:

> Plugin KHÔNG BAO GIỜ truy cập tài nguyên trực tiếp

Thay vào đó:

```
Plugin → Capability Interface → Core Proxy → Infrastructure
```

---

## Ý nghĩa:

- Plugin = untrusted code
- Capability = controlled gateway
- Core = enforcement layer

---

# 3. 🚨 ZERO TRUST RULE

## MUST:

- Mọi hành vi truy cập tài nguyên phải qua capability
- Không có “hidden access path”
- Không có direct dependency tới DB / Network / OS

---

## NEVER:

- Plugin tự mở DB connection
- Plugin tự gọi HTTP client raw
- Plugin truy cập file system trực tiếp

---

# 4. 🧱 CAPABILITY ARCHITECTURE

```
                ┌────────────────────┐
                │     Plugin Code    │
                └─────────┬──────────┘
                          │
                          ▼
            ┌──────────────────────────┐
            │   Capability Context    │
            │ (Injected by Core)      │
            └─────────┬────────────────┘
                      │
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
┌────────────┐ ┌────────────┐ ┌────────────┐
│ Database   │ │ Network    │ │ Storage    │
│ Capability │ │ Capability │ │ Capability │
└────────────┘ └────────────┘ └────────────┘
```

---

# 5. 🔐 CAPABILITY CONTRACT

## Interface base:

```csharp
public interface ICapability
{
    string Name { get; }
}
```

---

## Example: Database Capability

```csharp
public interface IDatabaseCapability : ICapability
{
    Task<List<T>> QueryAsync<T>(string query);
}
```

---

## Example: Network Capability

```csharp
public interface INetworkCapability : ICapability
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
}
```

---

# 6. 📄 CAPABILITY ASSIGNMENT (FROM MANIFEST)

Capabilities are defined in Signed Manifest:

```json
{
  "permissions": [
    "db:read",
    "network:outbound"
  ]
}
```

---

## RULE:

👉 If not listed in manifest → capability is NOT injected

---

# 7. 🧠 CAPABILITY RESOLUTION FLOW

```
1. Load Manifest
2. Verify Signature
3. Read Permissions List
4. Map Permissions → Capability Implementations
5. Inject into PluginContext
```

---

# 8. 🔥 CAPABILITY INJECTION MODEL

## PluginContext:

```csharp
public class PluginContext
{
    public IReadOnlyDictionary<string, ICapability> Capabilities { get; }
}
```

---

## Usage in plugin:

```csharp
var db = context.Capabilities["Database"] as IDatabaseCapability;

var data = await db.QueryAsync<User>("SELECT * FROM Users");
```

---

# 9. 🧯 SECURITY ENFORCEMENT

## Core rules:

- Capability injection is read-only
- Plugin cannot modify granted capabilities
- No runtime escalation allowed

---

## Enforcement layer:

- ManifestValidator
- CapabilityResolver
- SecurityEngine

---

# 10. 🚫 FORBIDDEN BEHAVIORS

## ❌ Direct access:

```csharp
new SqlConnection()
HttpClient.Send()
File.ReadAllText()
```

---

## ❌ Bypass attempts:

- Reflection to access internal services
- Service locator abuse
- Static global state injection

---

# 11. ⏱ RUNTIME SAFETY

Each capability call is controlled:

- Timeout enforced
- Rate limiting optional
- Logging mandatory
- Error isolation per plugin

---

# 12. 🧱 EXTENSIBILITY MODEL

New capabilities can be added:

- IQueueCapability
- ICacheCapability
- IStorageCapability

---

## RULE:

👉 New capability MUST:

- Be registered in Core
- Be validated in Manifest schema
- Be enforced via CapabilityResolver

---

# 13. 🔐 SECURITY GUARANTEE

## System guarantees:

- Plugin cannot escape sandbox via capabilities
- No capability = no access
- All access paths are deterministic

---

# 14. 🎯 DESIGN PRINCIPLES

- Explicit > implicit
- Deny by default
- Least privilege always
- No hidden access paths

---

# 15. 🚀 FINAL MODEL SUMMARY

Capability system đảm bảo:

- Plugin chỉ làm đúng những gì được cấp quyền
- Core kiểm soát toàn bộ IO operations
- Security không phụ thuộc plugin code

---

# 🏁 END OF CAPABILITY SYSTEM