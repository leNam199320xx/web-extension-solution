# Demo Login Guide

How to log in and use the platform demo. No external services required.

## Start the Platform

```bash
# Double-click run-all.bat, or:
cd h:\namld1\web-extension-solution
run-all.bat
```

Services start at:
- **API Backend** — http://localhost:6100
- **Swagger UI** — http://localhost:6100/swagger
- **API Gateway** — http://localhost:6200
- **Marketplace Portal** — http://localhost:6300
- **Consumer Portal** — http://localhost:6400
- **Admin Portal** — http://localhost:6500

## Login via API

### Register a New Account

```bash
curl -X POST http://localhost:6100/api/auth/register ^
  -H "Content-Type: application/json" ^
  -d "{\"email\":\"you@example.com\",\"password\":\"yourpassword\",\"displayName\":\"Your Name\"}"
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "displayName": "Your Name",
  "email": "you@example.com",
  "role": "Tenant_Owner",
  "tenantId": "...",
  "expiresAt": "2024-07-10T10:00:00Z"
}
```

### Login with Existing Account

```bash
curl -X POST http://localhost:6100/api/auth/login ^
  -H "Content-Type: application/json" ^
  -d "{\"email\":\"alice@acme.com\",\"password\":\"secret\"}"
```

### Use the Token

```bash
curl http://localhost:6100/api/auth/me ^
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

## Pre-configured Test Accounts

| Email | Password | Role | Tenant |
|-------|----------|------|--------|
| `admin@pluginruntime.internal` | `admin` | Platform Admin | Platform Operations (Internal) |
| `alice@acme.com` | `secret` | Tenant Owner | Acme Corp (Pro plan) |
| `bob@startuplabs.io` | `secret` | Tenant Owner | Startup Labs (Free plan) |
| `carol@enterprise-global.com` | `secret` | Tenant Owner | Enterprise Global (Enterprise plan) |

## Login via Web Portals

### Marketplace Portal (http://localhost:6300)

1. Open http://localhost:6300
2. Click **Login** in the top bar
3. Use Quick Login buttons:
   - **Alice (Pro Tenant)** — developer with paid plan
   - **Bob (Free Tenant)** — developer with free plan
   - **Admin User** — platform administrator

### Consumer Portal (http://localhost:6400)

1. Open http://localhost:6400
2. Click **Login**
3. Choose a test account

## What You Can Do After Login

### As Alice (Pro Tenant)
- Browse plugins in Marketplace
- Upload new extensions
- View subscriptions
- Access up to 10,000 requests/day

### As Bob (Free Tenant)
- Browse plugins
- Limited to 100 requests/day
- Cannot subscribe to packages (Free plan)

### As Admin
- View all tenants at `/api/admin/tenants`
- List all plans at `/api/admin/plans`
- Approve/reject plugins
- Suspend tenants

## Using the API Gateway

The Gateway uses API keys (not JWT). Use the keys from the test data:

```bash
# As Acme Corp (Pro tenant)
curl http://localhost:6200/api/plugins ^
  -H "X-Api-Key: acme_pro_TeSt1234567890abcdefghijklmnop7k9"
```

## Swagger UI

Open http://localhost:6100/swagger to explore all API endpoints interactively.

To authenticate in Swagger:
1. Call `POST /api/auth/login` with test credentials
2. Copy the `token` from the response
3. Click "Authorize" button in Swagger
4. Enter: `Bearer <your-token>`
5. All subsequent requests will be authenticated
