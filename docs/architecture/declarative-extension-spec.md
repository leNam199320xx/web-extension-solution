# 📄 Declarative Extension Specification

---

# 1. PURPOSE

Defines a JSON-driven extension model for simple, common operations (queries, API proxying, data transforms) that don't require custom C# code. The platform provides built-in executors that interpret the JSON config at runtime.

For Code Extensions (DLL-based), see `docs/plugin/plugin-sdk-spec.md`.
For extension ecosystem, see `docs/architecture/extension-ecosystem.md`.

---

# 2. WHEN TO USE

| Use Declarative Extension | Use Code Extension |
|--------------------------|-------------------|
| Simple DB query (SELECT) | Complex business logic |
| DB insert/update with field mapping | Multi-step workflows with branching |
| Proxy external REST API | Custom data transformation logic |
| Read/write cache with simple key | Stateful processing across calls |
| Chain 2-3 simple operations | Integration with complex protocols |
| Data filtering/mapping | Custom error handling strategies |

Rule of thumb: if it can be expressed as config → declarative. If it needs `if/else/loop` → code.

---

# 3. EXTENSION TYPES (Updated Model)

```
Extension Types:
├── Code Extension (.plugin.zip)           → Custom C# code, IPlugin interface
└── Declarative Extension (.ext.json)      → JSON config, no code, platform executes
```

Both types:
- Go through the same permission model
- Are registered in Extension Registry
- Support Public/Private/Subscription visibility
- Are fully traceable and auditable
- Enforce Zero Trust (capabilities required)

---

# 4. DECLARATIVE EXTENSION FORMAT

## 4.1 File: `{extension-id}.ext.json`

```json
{
  "$schema": "https://plugin-runtime/schemas/declarative-extension-v1.json",
  "extension_id": "get-active-users",
  "type": "declarative",
  "version": "1.0.0",
  "display_name": "Get Active Users",
  "description": "Query active users from the main database",
  "author": "team@company.com",
  "visibility": "Public",
  "target_core_version": ">=1.0.0",

  "permissions": [
    "db:read:users"
  ],
  "capabilities": [
    "DatabaseCapability"
  ],

  "input_schema": {
    "type": "object",
    "properties": {
      "active": { "type": "boolean", "default": true },
      "limit": { "type": "integer", "minimum": 1, "maximum": 100, "default": 50 }
    }
  },

  "output_schema": {
    "type": "array",
    "items": {
      "type": "object",
      "properties": {
        "id": { "type": "string" },
        "name": { "type": "string" },
        "email": { "type": "string" }
      }
    }
  },

  "action": {
    "type": "db-query",
    "query": "SELECT id, name, email FROM users WHERE active = @active LIMIT @limit",
    "parameters": {
      "active": { "source": "input", "field": "active" },
      "limit": { "source": "input", "field": "limit" }
    }
  },

  "execution_policy": {
    "timeout_ms": 3000,
    "max_memory_mb": 64,
    "cache": {
      "enabled": true,
      "ttl_seconds": 60,
      "key_template": "get-active-users:{active}:{limit}"
    }
  }
}
```

---

# 5. SUPPORTED ACTION TYPES

## 5.1 `db-query` — SELECT data

```json
{
  "type": "db-query",
  "connection": "main",
  "query": "SELECT * FROM orders WHERE status = @status AND created_at > @since",
  "parameters": {
    "status": { "source": "input", "field": "status" },
    "since": { "source": "input", "field": "since", "type": "datetime" }
  },
  "output": {
    "type": "array"
  }
}
```

Rules:
- Only SELECT queries allowed
- All parameters MUST be parameterized (no string interpolation)
- Connection refers to a registered database connection name
- Requires `db:read:{table}` permission

---

## 5.2 `db-command` — INSERT/UPDATE/DELETE

```json
{
  "type": "db-command",
  "connection": "main",
  "query": "UPDATE orders SET status = @newStatus WHERE id = @orderId",
  "parameters": {
    "orderId": { "source": "input", "field": "orderId" },
    "newStatus": { "source": "input", "field": "status" }
  },
  "output": {
    "type": "affected-rows"
  }
}
```

Rules:
- Requires `db:write:{table}` permission
- DDL (CREATE, DROP, ALTER) is NEVER allowed
- Parameterized queries only

---

## 5.3 `http-proxy` — Call external API

```json
{
  "type": "http-proxy",
  "method": "POST",
  "url": "https://api.stripe.com/v1/charges",
  "headers": {
    "Authorization": { "source": "secret", "key": "stripe_api_key" },
    "Content-Type": "application/json"
  },
  "body": {
    "source": "input",
    "transform": {
      "amount": "$.amount",
      "currency": "$.currency",
      "source": "$.paymentToken"
    }
  },
  "response": {
    "extract": {
      "chargeId": "$.id",
      "status": "$.status"
    }
  },
  "timeout_ms": 5000
}
```

Rules:
- Requires `network:outbound:{url_pattern}` permission
- Only HTTPS allowed
- Headers with secrets reference secret store (never hardcoded)
- Response can be transformed/filtered before returning

---

## 5.4 `cache-get` — Read from cache

```json
{
  "type": "cache-get",
  "key_template": "user-profile:{userId}",
  "parameters": {
    "userId": { "source": "input", "field": "userId" }
  },
  "on_miss": "return-null"
}
```

Rules:
- Requires `cache:read:{prefix}` permission
- `on_miss`: `return-null` | `fallback-action` (chain to next action)

---

## 5.5 `cache-set` — Write to cache

```json
{
  "type": "cache-set",
  "key_template": "user-profile:{userId}",
  "value": { "source": "input", "field": "data" },
  "ttl_seconds": 300,
  "parameters": {
    "userId": { "source": "input", "field": "userId" }
  }
}
```

Rules:
- Requires `cache:write:{prefix}` permission

---

## 5.6 `transform` — Map/filter data

```json
{
  "type": "transform",
  "input_source": "input",
  "operations": [
    { "type": "select", "fields": ["id", "name", "email"] },
    { "type": "filter", "condition": "$.active == true" },
    { "type": "rename", "from": "id", "to": "userId" },
    { "type": "limit", "count": 10 }
  ]
}
```

Supported operations:
- `select` — pick specific fields
- `filter` — filter rows by condition (JSONPath expression)
- `rename` — rename field
- `limit` — limit result count
- `default` — add default values for missing fields
- `format` — format date/number fields

---

## 5.7 `compose` — Chain multiple actions

```json
{
  "type": "compose",
  "steps": [
    {
      "name": "check-cache",
      "action": {
        "type": "cache-get",
        "key_template": "order:{orderId}",
        "parameters": { "orderId": { "source": "input", "field": "orderId" } },
        "on_miss": "continue"
      }
    },
    {
      "name": "query-db",
      "condition": "steps.check-cache.result == null",
      "action": {
        "type": "db-query",
        "query": "SELECT * FROM orders WHERE id = @orderId",
        "parameters": { "orderId": { "source": "input", "field": "orderId" } }
      }
    },
    {
      "name": "update-cache",
      "condition": "steps.query-db.result != null",
      "action": {
        "type": "cache-set",
        "key_template": "order:{orderId}",
        "value": { "source": "steps.query-db.result" },
        "ttl_seconds": 120
      }
    }
  ],
  "output": {
    "source": "steps.check-cache.result ?? steps.query-db.result"
  }
}
```

Rules:
- Max 5 steps per compose
- Steps execute sequentially
- `condition` is optional (skip step if false)
- Steps can reference previous step results via `steps.{name}.result`
- No loops allowed (prevents infinite execution)

---

# 6. PARAMETER SOURCES

| Source | Description | Example |
|--------|-------------|---------|
| `input` | From execution request input | `{ "source": "input", "field": "userId" }` |
| `secret` | From platform secret store | `{ "source": "secret", "key": "api_key_name" }` |
| `context` | From execution context | `{ "source": "context", "field": "tenantId" }` |
| `static` | Hardcoded value | `{ "source": "static", "value": "active" }` |
| `steps` | From previous step (compose) | `{ "source": "steps.query-db.result" }` |

---

# 7. SECRET MANAGEMENT

Declarative extensions can reference secrets without exposing values:

```json
"headers": {
  "Authorization": { "source": "secret", "key": "stripe_api_key" }
}
```

- Secrets are stored in platform secret store (Key Vault)
- Extension owner uploads secrets via Admin Portal
- Secrets are scoped per extension (isolation)
- Secrets are never written to logs or responses
- Secret names must be declared in manifest

---

# 8. VERIFICATION (Simplified)

Declarative extensions go through a **lighter** verification pipeline:

| Stage | Check |
|-------|-------|
| Schema validation | JSON conforms to declarative extension schema |
| Permission check | Permissions match action requirements |
| SQL safety | No DDL, no dangerous patterns, parameterized only |
| URL validation | HTTP proxy URLs are HTTPS, no internal IPs |
| Secret references | All referenced secrets exist |
| Compose depth | Max 5 steps, no loops |
| Input/Output schema | Valid JSON Schema definitions |

**NOT needed** (vs Code Extension):
- ❌ IL scanning (no DLL)
- ❌ Dependency audit (no NuGet)
- ❌ Static analysis (no code)
- ❌ Sandbox execution (platform executor is trusted)

Verification time: **< 5 seconds** (vs up to 2 minutes for Code Extensions).

---

# 9. RUNTIME EXECUTION

Platform has a built-in **Declarative Extension Executor**:

```csharp
public interface IDeclarativeExecutor
{
    Task<ExecutionResult> ExecuteAsync(
        DeclarativeExtensionConfig config,
        JsonElement input,
        IPluginExecutionContext context,
        CancellationToken cancellationToken);
}
```

Flow:
1. Load `.ext.json` config
2. Validate input against `input_schema`
3. Check cache (if caching enabled)
4. Execute action(s) via built-in handlers
5. Transform output
6. Cache result (if enabled)
7. Return result

No AssemblyLoadContext needed. No plugin isolation needed (code is trusted platform code).

---

# 10. UPLOAD FLOW

```
Developer creates get-active-users.ext.json
    ↓
Upload via Admin Portal or CLI: `plugin upload-declarative ./get-active-users.ext.json`
    ↓
Simplified verification (< 5 seconds)
    ↓
Permission review (same as Code Extension)
    ↓
Registered in Extension Registry (type: declarative)
    ↓
Available for invocation
```

---

# 11. ADMIN PORTAL INTEGRATION

Declarative extensions show differently in the UI:

```
┌──────────────────────────────────────────────────────┐
│ 📄 get-active-users           v1.0.0   🟢 Active    │
│    [Declarative] Query active users                  │
│    Action: db-query → users table                    │
│    Cache: 60s TTL                                    │
│    Permissions: db:read:users                        │
│                                         [View ▶]    │
└──────────────────────────────────────────────────────┘
```

Detail view shows the full JSON config (read-only), plus:
- Live test interface (run with sample input, see output)
- Execution stats
- Cache hit rate

---

# 12. INLINE EDITING (Future)

Admin Portal could allow creating/editing declarative extensions directly in the browser:
- JSON editor with schema validation
- Parameter builder UI
- Test execution before publishing
- Version comparison

---

# 13. LIMITATIONS

Declarative extensions CANNOT:
- Execute arbitrary code
- Use conditional logic beyond simple `compose` conditions
- Loop over data
- Maintain state between executions
- Spawn background tasks
- Access capabilities not supported by action types

If you need any of these → use a Code Extension.

---

# 14. COMPARISON MATRIX

| Aspect | Declarative | Code |
|--------|-------------|------|
| Format | `.ext.json` | `.plugin.zip` |
| Language | JSON config | C# |
| Verification | < 5 seconds | < 2 minutes |
| Risk level | Low (no arbitrary code) | Variable (requires full scan) |
| Capabilities | db-query, db-command, http-proxy, cache, transform, compose | All (IPlugin interface) |
| Isolation | None needed (platform executor) | AssemblyLoadContext / Process |
| Performance | Optimized by platform | Depends on code quality |
| Development time | Minutes | Hours to days |
| Testing | Schema validation + live test | Unit + integration tests |
| Secrets | Reference by name | Reference by name (via capability) |

---

# 15. DATABASE UPDATE

Add `type` field to `extension_registry`:

```sql
ALTER TABLE extension_registry
    ADD COLUMN type VARCHAR(50) NOT NULL DEFAULT 'Code';

-- type: Code, Declarative
```

Add config storage for declarative extensions:

```sql
CREATE TABLE declarative_configs (
    config_id       UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    extension_id    VARCHAR(200)    NOT NULL REFERENCES extension_registry(extension_id),
    version         VARCHAR(50)     NOT NULL,
    config          JSONB           NOT NULL,
    input_schema    JSONB,
    output_schema   JSONB,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_declarative_version UNIQUE (extension_id, version)
);
```

---

# 16. DESIGN PRINCIPLE

> Simple things should be simple. Complex things should be possible.
>
> Declarative extensions make the 80% case trivial.
> Code extensions handle the 20% that needs custom logic.
> Both follow the same security and permission model.

---

# 🏁 END
