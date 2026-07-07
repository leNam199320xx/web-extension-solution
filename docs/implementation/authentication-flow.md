# 🔐 Authentication & Authorization Flow

---

# 1. PURPOSE

Định nghĩa chi tiết authentication và authorization cho Runtime API.

---

# 2. AUTHENTICATION MODEL

## Protocol: OAuth 2.0 + JWT Bearer Token

```
Client → Identity Provider (IdP) → Access Token (JWT)
Client → Runtime API (with Bearer token)
Runtime API → Validate JWT → Process request
```

---

# 3. JWT TOKEN STRUCTURE

```json
{
  "sub": "user-123",
  "iss": "https://your-idp.com",
  "aud": "plugin-runtime-api",
  "exp": 1704067200,
  "iat": 1704063600,
  "roles": ["Developer", "Admin"],
  "permissions": ["plugin:execute", "plugin:manage"]
}
```

---

# 4. AUTHORIZATION MODEL (RBAC)

## Roles:

| Role | Description | Permissions |
|------|-------------|-------------|
| Developer | Plugin developer | plugin:execute, plugin:upload |
| Admin | Platform administrator | plugin:manage, plugin:approve, plugin:revoke |
| SecurityOfficer | Security reviewer | plugin:approve, plugin:revoke, audit:read |
| Auditor | Read-only audit | audit:read, plugin:list |

---

# 5. ENDPOINT AUTHORIZATION

| Endpoint | Required Permission |
|----------|-------------------|
| POST /api/v1/execute/{pluginId} | plugin:execute |
| GET /api/v1/plugins | plugin:list |
| POST /api/v1/plugins/upload | plugin:upload |
| POST /api/v1/plugins/approve/{id} | plugin:approve |
| POST /api/v1/plugins/revoke/{id} | plugin:revoke |
| POST /api/v1/plugins/reload/{id} | plugin:manage |
| GET /api/v1/audit | audit:read |

---

# 6. IMPLEMENTATION

## Middleware pipeline:

```
Request
  ↓
Authentication Middleware (JWT validation)
  ↓
Authorization Middleware (policy check)
  ↓
Rate Limiting Middleware
  ↓
Controller
```

## ASP.NET Core setup:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = configuration["Authentication:Authority"];
        options.Audience = configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = true;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanExecute", p => p.RequireClaim("permissions", "plugin:execute"))
    .AddPolicy("CanManage", p => p.RequireClaim("permissions", "plugin:manage"))
    .AddPolicy("CanApprove", p => p.RequireClaim("permissions", "plugin:approve"));
```

---

# 7. SERVICE-TO-SERVICE AUTH

Internal service calls (nếu có):

- Use client credentials flow (OAuth 2.0)
- Short-lived tokens
- Mutual TLS (optional, high-security)

---

# 8. SECURITY RULES

## MUST:

- Validate token on every request
- Check token expiration
- Verify issuer and audience
- Enforce HTTPS only
- Log authentication failures

## MUST NOT:

- Accept expired tokens
- Trust tokens without signature validation
- Store tokens in logs
- Use symmetric signing for production

---

# 🏁 END
