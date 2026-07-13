# Quick Start — 5 Minutes to Get Running

Pick your role and follow the steps below.

---

## I'm a Plugin Developer

**Goal:** Publish your first extension on the platform.

1. **Sign up** on the Marketplace Portal
2. **Create a manifest** — a JSON file describing your extension, its capabilities, and permissions
3. **Write your plugin** — implement `IPlugin` from the SDK, use capabilities through `PluginContext`
4. **Package** — zip your `manifest.json` + DLL as `my-plugin.plugin.zip`
5. **Upload** — drag and drop on the Marketplace Portal → Upload page
6. **Wait for approval** — a platform admin reviews your permissions
7. **Done!** — your extension is live and callable

📖 Full guide: [Extension Development](extension-development.md)

---

## I'm an API Consumer

**Goal:** Call extensions through the API.

1. **Sign up** on the Consumer Portal — pick a plan (Free to start)
2. **Copy your API key** — shown once at registration
3. **Make a call:**
   ```bash
   curl https://gateway.example.com/api/plugins/execute \
     -H "X-Api-Key: your-key-here" \
     -d '{"extension_id": "com.example.hello", "input": {}}'
   ```
4. **Browse packages** — subscribe to plugin packages for access to premium extensions
5. **Monitor usage** — check your dashboard for daily requests and quota

📖 Full guide: [Subscription & Usage](subscription-and-usage.md)

---

## I'm a Platform Admin

**Goal:** Keep the platform running smoothly.

1. **Log in** to the Admin Portal with your Platform_Admin credentials
2. **Review pending plugins** — check permissions, security scan results, approve or reject
3. **Monitor tenants** — view active tenants, plans, and usage
4. **Handle issues** — suspend problematic tenants, revoke compromised keys
5. **Manage packages** — create plugin bundles, set pricing, deactivate old packages

📖 Full guide: [Platform Administration](platform-administration.md)

---

## Running the Platform Locally

```bash
# Option 1: Full stack with Aspire Dashboard (no Docker required)
cd src/Aspire/PluginRuntime.AppHost
dotnet run
# Dashboard opens at http://localhost:6000
# Services:
#   API Backend       → http://localhost:6100 (Swagger: /swagger)
#   API Gateway       → http://localhost:6200
#   Marketplace       → http://localhost:6300
#   Consumer Portal   → http://localhost:6400
#   Admin Portal      → http://localhost:6500

# Option 2: All services via batch file (no Aspire, no Docker)
run-all.bat
# Same ports as above (6100–6500)

# Option 3: Just the API (uses JSON storage, no DB needed)
cd src/PluginRuntime.Api
set ASPNETCORE_URLS=http://localhost:6100
dotnet run
```

---

## What's Next?

| Want to... | Read |
|-----------|------|
| Build an extension | [Extension Development](extension-development.md) |
| Use extensions via API | [Subscription & Usage](subscription-and-usage.md) |
| Administer the platform | [Platform Administration](platform-administration.md) |
