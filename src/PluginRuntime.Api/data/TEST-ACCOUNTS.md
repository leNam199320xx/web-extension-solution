# Test Accounts

Các tài khoản demo dùng để test local. Khi chạy ở chế độ Development, 
hệ thống chấp nhận JWT token được ký bằng secret key bên dưới.

## JWT Configuration (Development)

```json
{
  "Jwt": {
    "Secret": "default-development-secret-key-at-least-32-chars!",
    "Issuer": "PluginRuntime",
    "Audience": "PluginRuntime"
  }
}
```

## Test Accounts

### 1. Platform Admin

| Field | Value |
|-------|-------|
| Name | Admin User |
| Email | admin@pluginruntime.internal |
| Role | Platform_Admin |
| Tenant | Platform Operations |
| Tenant ID | `b2c3d4e5-0001-0001-0001-000000000004` |

**JWT Claims:**
```json
{
  "sub": "admin-user-001",
  "name": "Admin User",
  "email": "admin@pluginruntime.internal",
  "role": "Platform_Admin",
  "tenant_id": "b2c3d4e5-0001-0001-0001-000000000004",
  "plan_id": "a1b2c3d4-0001-0001-0001-000000000004",
  "is_internal": "true"
}
```

---

### 2. Pro Tenant User (Acme Corp)

| Field | Value |
|-------|-------|
| Name | Alice Developer |
| Email | alice@acme.com |
| Role | Tenant_Owner |
| Tenant | Acme Corp |
| Tenant ID | `b2c3d4e5-0001-0001-0001-000000000001` |
| Plan | Pro |
| API Key (for gateway) | `acme_pro_TeSt1234567890abcdefghijklmnop7k9` |

**JWT Claims:**
```json
{
  "sub": "user-alice-001",
  "name": "Alice Developer",
  "email": "alice@acme.com",
  "role": "Tenant_Owner",
  "tenant_id": "b2c3d4e5-0001-0001-0001-000000000001",
  "plan_id": "a1b2c3d4-0001-0001-0001-000000000002",
  "is_internal": "false"
}
```

---

### 3. Free Tenant User (Startup Labs)

| Field | Value |
|-------|-------|
| Name | Bob Startup |
| Email | bob@startuplabs.io |
| Role | Tenant_Owner |
| Tenant | Startup Labs |
| Tenant ID | `b2c3d4e5-0001-0001-0001-000000000002` |
| Plan | Free |
| API Key (for gateway) | `startup_TeSt1234567890abcdefghijklmnoq2w4` |

**JWT Claims:**
```json
{
  "sub": "user-bob-002",
  "name": "Bob Startup",
  "email": "bob@startuplabs.io",
  "role": "Tenant_Owner",
  "tenant_id": "b2c3d4e5-0001-0001-0001-000000000002",
  "plan_id": "a1b2c3d4-0001-0001-0001-000000000001",
  "is_internal": "false"
}
```

---

### 4. Enterprise Tenant User (Enterprise Global)

| Field | Value |
|-------|-------|
| Name | Carol Enterprise |
| Email | carol@enterprise-global.com |
| Role | Tenant_Owner |
| Tenant | Enterprise Global |
| Tenant ID | `b2c3d4e5-0001-0001-0001-000000000003` |
| Plan | Enterprise |
| API Key (for gateway) | `ent_prod_TeSt1234567890abcdefghijklmnoz8y6` |

**JWT Claims:**
```json
{
  "sub": "user-carol-003",
  "name": "Carol Enterprise",
  "email": "carol@enterprise-global.com",
  "role": "Tenant_Owner",
  "tenant_id": "b2c3d4e5-0001-0001-0001-000000000003",
  "plan_id": "a1b2c3d4-0001-0001-0001-000000000003",
  "is_internal": "false"
}
```

---

### 5. Suspended Tenant User

| Field | Value |
|-------|-------|
| Name | Dave Suspended |
| Email | dave@suspended.co |
| Role | Tenant_Owner |
| Tenant | Suspended Co |
| Tenant ID | `b2c3d4e5-0001-0001-0001-000000000005` |
| Plan | Pro (suspended) |
| Status | ⚠️ Account suspended — requests will be rejected |

---

## Generating Test JWT Tokens

Dùng script PowerShell hoặc tool online (jwt.io) để tạo token với claims ở trên.

**PowerShell (cần .NET SDK):**

```powershell
# Tạo token cho Alice (Pro tenant)
$secret = "default-development-secret-key-at-least-32-chars!"
$header = @{ alg = "HS256"; typ = "JWT" } | ConvertTo-Json -Compress
$payload = @{
    sub = "user-alice-001"
    name = "Alice Developer"
    email = "alice@acme.com"
    role = "Tenant_Owner"
    tenant_id = "b2c3d4e5-0001-0001-0001-000000000001"
    plan_id = "a1b2c3d4-0001-0001-0001-000000000002"
    is_internal = "false"
    iss = "PluginRuntime"
    aud = "PluginRuntime"
    exp = [int](Get-Date).AddHours(24).ToUniversalTime().Subtract([datetime]'1970-01-01').TotalSeconds
} | ConvertTo-Json -Compress

# Encode (simplified — use a proper JWT library in production)
```

**Hoặc dùng curl với API key (qua Gateway):**

```bash
# Không cần JWT — dùng API key trực tiếp qua Public API Gateway
curl https://localhost:5002/api/plugins \
  -H "X-Api-Key: acme_pro_TeSt1234567890abcdefghijklmnop7k9"
```

## Quick Test Matrix

| Scenario | Account | Expected |
|----------|---------|----------|
| Admin operations | Admin User | ✅ Full access to /api/admin/* |
| Normal tenant CRUD | Alice (Pro) | ✅ Access own resources only |
| Free plan limits | Bob (Free) | ❌ Cannot subscribe to packages |
| Enterprise unlimited | Carol (Enterprise) | ✅ No rate limits or quotas |
| Suspended account | Dave (Suspended) | ❌ All requests rejected |
| Cross-tenant access | Alice accessing Bob's data | ❌ 403 UA-AUTH-001 |
| API key via gateway | Any API key above | ✅ Authenticates at gateway level |
