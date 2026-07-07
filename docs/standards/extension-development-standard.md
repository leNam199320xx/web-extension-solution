# 📋 Extension Development Standard

---

# 1. PURPOSE

Defines mandatory standards that ALL plugin/extension developers must follow before submitting a plugin to the platform. Non-compliance results in automatic rejection during the verification pipeline.

For SDK usage, see `docs/plugin/plugin-sdk-spec.md`.
For packaging format, see `docs/implementation/plugin-packaging.md`.
For the automated verification engine, see `docs/architecture/verification-engine-spec.md`.

---

# 2. WHO THIS APPLIES TO

- Third-party extension developers
- Internal teams building plugins
- Any code that implements `IPlugin` and runs inside the runtime

---

# 3. PROJECT STRUCTURE STANDARD

Every extension package MUST follow this structure:

```
my-extension/
├── src/
│   └── MyExtension/
│       ├── MyExtension.csproj
│       └── PluginEntry.cs          → IPlugin implementation
├── tests/
│   └── MyExtension.Tests/
│       ├── MyExtension.Tests.csproj
│       └── PluginEntryTests.cs
├── manifest.json                    → Plugin manifest (unsigned)
├── README.md                        → Plugin documentation
└── CHANGELOG.md                     → Version history (recommended)
```

---

# 4. CODING STANDARDS

## 4.1 Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Namespace | `{Company}.{PluginName}` | `Acme.PaymentProcessor` |
| Entry class | `PluginEntry` or `{Name}Plugin` | `PaymentPlugin` |
| Project file | `{PluginName}.csproj` | `PaymentProcessor.csproj` |

## 4.2 Required Patterns

- All I/O operations MUST use `CancellationToken` from context
- All capability access MUST go through `context.Capabilities`
- All logging MUST use `context.Logger`
- Return `PluginResult.Ok()` or `PluginResult.Fail()` — never throw unhandled exceptions
- Handle errors gracefully within the plugin

## 4.3 Code Quality

- No compiler warnings allowed
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- No `#pragma warning disable` unless justified with comment
- Maximum cyclomatic complexity: 10 per method
- Maximum method length: 50 lines (excluding comments)
- Maximum file length: 300 lines

---

# 5. FORBIDDEN PATTERNS (Auto-Verified)

The following will cause **automatic rejection** during upload:

## 5.1 Direct Infrastructure Access

```csharp
// ❌ FORBIDDEN
new SqlConnection(...)
new NpgsqlConnection(...)
new HttpClient(...)
File.ReadAllText(...)
Directory.GetFiles(...)
Environment.GetEnvironmentVariable(...)
Process.Start(...)
```

## 5.2 Reflection Abuse

```csharp
// ❌ FORBIDDEN
Type.GetType("internal.type")
Assembly.Load(...)
MethodInfo.Invoke(...)
Activator.CreateInstance(internalType)
typeof(InternalClass).GetField(..., BindingFlags.NonPublic)
```

## 5.3 Threading Abuse

```csharp
// ❌ FORBIDDEN
new Thread(...)
ThreadPool.QueueUserWorkItem(...)
Task.Run(...) // without using context.CancellationToken
new Timer(...)
```

## 5.4 Global State

```csharp
// ❌ FORBIDDEN
static mutable fields
static collections that persist between executions
Singleton patterns that cache state
```

## 5.5 Forbidden NuGet Packages

| Package | Reason |
|---------|--------|
| System.Data.SqlClient | Direct DB access |
| Npgsql | Direct DB access |
| MySql.Data | Direct DB access |
| MongoDB.Driver | Direct DB access |
| Microsoft.Data.SqlClient | Direct DB access |
| Dapper (standalone) | Direct DB access |
| RestSharp | Direct HTTP access |
| Flurl.Http | Direct HTTP access |
| System.IO.FileSystem | Direct file access |

## 5.6 Forbidden Namespaces (in IL)

- `System.Net.Sockets`
- `System.IO` (except `System.IO.MemoryStream`, `System.IO.Stream`)
- `System.Diagnostics.Process`
- `System.Reflection.Emit`
- `System.Runtime.InteropServices`

---

# 6. MANIFEST STANDARDS

Every extension MUST include a valid `manifest.json`:

## Required Fields

| Field | Validation |
|-------|-----------|
| `extension_id` | Lowercase alphanumeric + hyphens, 3-50 chars |
| `version` | Valid SemVer (MAJOR.MINOR.PATCH) |
| `display_name` | Non-empty, max 100 chars |
| `description` | Non-empty, max 500 chars |
| `author` | Valid email format |
| `entry_point` | Must reference existing DLL in package |
| `entry_class` | Fully qualified class name implementing IPlugin |
| `target_core_version` | Valid SemVer range expression |
| `permissions` | Array of known permission strings |
| `execution_policy.timeout_ms` | 100 ≤ value ≤ 30000 |
| `execution_policy.max_memory_mb` | 16 ≤ value ≤ 512 |

## Permission Naming

Permissions follow the pattern: `{resource}:{action}`

Valid examples:
- `db:read`, `db:write`
- `network:outbound`
- `cache:read`, `cache:write`
- `storage:read`, `storage:write`

---

# 7. RESOURCE SCOPE DECLARATION

## 7.1 Purpose

Extensions MUST declare **exactly which resources** they need access to — not just the capability type, but the specific scope. This enables fine-grained access control and audit.

## 7.2 Manifest Resource Scopes

The manifest `permissions` array now supports scoped declarations:

```json
{
  "permissions": [
    "db:read:orders",
    "db:write:orders",
    "db:read:products",
    "network:outbound:https://api.payment-provider.com/*",
    "network:outbound:https://api.shipping.com/v2/*",
    "storage:read:/plugins/payment-service/config/*",
    "storage:write:/plugins/payment-service/data/*",
    "cache:read:payment-*",
    "cache:write:payment-*"
  ]
}
```

## 7.3 Permission Format

```
{capability}:{action}:{resource_scope}
```

| Part | Description | Examples |
|------|-------------|---------|
| capability | Resource type | `db`, `network`, `storage`, `cache` |
| action | Operation type | `read`, `write`, `outbound`, `execute` |
| resource_scope | Specific target | table name, URL pattern, path, key prefix |

## 7.4 Database Scope Rules

| Permission | Access granted |
|-----------|---------------|
| `db:read:orders` | SELECT on `orders` table only |
| `db:write:orders` | INSERT/UPDATE/DELETE on `orders` table only |
| `db:read:*` | SELECT on all tables (requires justification) |
| `db:execute:get_order_total` | Execute stored procedure `get_order_total` only |

Rules:
- Each table/schema must be declared explicitly
- Wildcard `*` requires written justification in manifest `description`
- Cross-schema access requires separate permission per schema
- DDL operations (CREATE, DROP, ALTER) are NEVER allowed

## 7.5 Network Scope Rules

| Permission | Access granted |
|-----------|---------------|
| `network:outbound:https://api.example.com/*` | All paths under that domain |
| `network:outbound:https://api.example.com/v2/payments` | Single endpoint only |
| `network:outbound:*` | All outbound (RESTRICTED — requires security review) |

Rules:
- Only HTTPS URLs allowed (no HTTP, no raw IP)
- Each external API must be declared separately
- Wildcard domains are forbidden (`*.com` is invalid)
- Internal/private network ranges are NEVER allowed
- Maximum 10 outbound URL patterns per plugin

## 7.6 Storage Scope Rules

| Permission | Access granted |
|-----------|---------------|
| `storage:read:/plugins/{pluginId}/config/*` | Read config files in own namespace |
| `storage:write:/plugins/{pluginId}/data/*` | Write data files in own namespace |
| `storage:read:/shared/templates/*` | Read from shared folder (if approved) |

Rules:
- Storage paths are always scoped under `/plugins/{pluginId}/` by default
- Accessing paths outside own namespace requires explicit approval
- Path traversal patterns (`..`) are blocked by runtime
- Maximum total storage: defined in manifest `execution_policy`

## 7.7 Cache Scope Rules

| Permission | Access granted |
|-----------|---------------|
| `cache:read:payment-*` | Read cache keys matching prefix |
| `cache:write:payment-*` | Write cache keys matching prefix |
| `cache:read:*` | Read all cache keys (discouraged) |

Rules:
- Cache keys are automatically namespaced: `{pluginId}:{key}`
- Plugin cannot read/write other plugins' cache keys
- Wildcard `*` still scoped to plugin's namespace

## 7.8 Verification

During upload verification:
- All permissions in manifest are validated for correct format
- Unknown resource types → rejected
- Overly broad scopes (`*`) → flagged for manual review
- Scope consistency checked (e.g., using `db:read:orders` but no `DatabaseCapability` declared → error)

During runtime:
- Every capability call is checked against declared scope
- Access outside declared scope → `CapabilityDeniedException`
- All scope violations are logged as security events

## 7.9 Least Privilege Principle

> Request only what you need. Nothing more.

Extensions that request overly broad permissions:
- May face longer approval times
- May be rejected if justification is insufficient
- Will be flagged in security audit reports

---

# 8. TESTING STANDARDS

## 8.1 Minimum Requirements

- At least 1 unit test per public method
- Minimum 70% code coverage
- All tests pass (zero failures)
- Test project MUST be included in the package or verifiable via CI report

## 8.2 Required Test Cases

Every extension MUST test:

| Scenario | Why |
|----------|-----|
| Happy path execution | Proves plugin works |
| Missing capability handling | Proves graceful degradation |
| CancellationToken respect | Proves cooperative cancellation |
| Invalid input handling | Proves no unhandled exceptions |
| Empty/null input | Proves defensive coding |

## 8.3 Test Naming

Pattern: `{Method}_{Scenario}_{ExpectedResult}`

Example: `Execute_WhenDatabaseUnavailable_ReturnsFailResult`

---

# 9. DOCUMENTATION STANDARDS

## 8.1 README.md (Required)

Must contain:
- Plugin name and description
- What it does (business purpose)
- Required capabilities (what permissions it needs and why)
- Input/Output format (JSON examples)
- Configuration (if any)
- Known limitations

## 8.2 CHANGELOG.md (Recommended)

Follow Keep a Changelog format for each version.

---

# 10. SECURITY STANDARDS

## 9.1 Mandatory

- No hardcoded secrets or API keys
- No logging of sensitive data (PII, credentials)
- Parameterized queries only (via IDatabaseCapability)
- Input validation on all external data from `context.Input`
- No deserialization of untrusted types

## 9.2 Secret Detection

The verification engine scans for:
- API key patterns (`sk-*`, `pk-*`, `AKIA*`)
- Connection strings
- JWT tokens
- Private keys (PEM format)
- Password literals

Any detection = automatic rejection.

---

# 11. SIZE LIMITS

| Constraint | Limit |
|-----------|-------|
| Total package (ZIP) | 100 MB |
| Single DLL | 50 MB |
| Total DLL count | 20 |
| manifest.json | 64 KB |
| README.md | 100 KB |
| Source files (if included) | 500 KB per file |

---

# 12. VERSIONING STANDARDS

- Follow Semantic Versioning strictly
- Never reuse a version number (versions are immutable)
- Breaking changes = MAJOR increment
- New features = MINOR increment
- Bug fixes = PATCH increment

---

# 13. SUBMISSION CHECKLIST

Before upload, extension developer MUST verify:

- [ ] `manifest.json` is valid and complete
- [ ] Resource scopes declared with least-privilege (no unnecessary wildcards)
- [ ] Entry class implements `IPlugin`
- [ ] All capabilities used are declared in manifest
- [ ] No forbidden APIs or packages
- [ ] Tests exist and pass
- [ ] Coverage ≥ 70%
- [ ] README.md exists with required sections
- [ ] No secrets in code
- [ ] No compiler warnings
- [ ] Package size within limits
- [ ] Version number is new (not previously used)

---

# 14. VERIFICATION OUTCOME

| Result | Meaning |
|--------|---------|
| ✅ PASS | Extension moves to approval queue |
| ⚠️ WARNING | Non-blocking issues noted, moves to approval with flags |
| ❌ FAIL | Rejected with detailed error report |

Failed extensions receive a structured report with:
- Stage that failed
- Specific rule violated
- Line/file location (where possible)
- Suggested fix

---

# 🏁 END
