# ⚙️ Configuration Model

---

# 1. PURPOSE

Định nghĩa cấu trúc configuration cho toàn bộ hệ thống runtime.

---

# 2. CONFIGURATION SOURCES (priority order)

1. Environment variables (highest)
2. User secrets (development only)
3. appsettings.{Environment}.json
4. appsettings.json (lowest)

---

# 3. APPSETTINGS STRUCTURE

```json
{
  "Runtime": {
    "DefaultTimeoutMs": 5000,
    "MaxMemoryMb": 256,
    "MaxConcurrentExecutions": 50,
    "PluginStoragePath": "/data/plugins",
    "EnableHotReload": true
  },

  "Security": {
    "SignatureAlgorithm": "RSA-SHA256",
    "KeyVaultUri": "https://your-vault.vault.azure.net/",
    "RevocationCheckEnabled": true,
    "ManifestExpirationBufferMinutes": 5
  },

  "Database": {
    "ConnectionString": "Host=localhost;Database=plugin_runtime;Username=app;Password=***",
    "MaxPoolSize": 100,
    "CommandTimeoutSeconds": 30,
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 3
  },

  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "plugin-runtime:",
    "DefaultExpirationMinutes": 60
  },

  "Observability": {
    "ServiceName": "plugin-runtime",
    "OtlpEndpoint": "http://localhost:4317",
    "LogLevel": "Information",
    "EnableMetrics": true,
    "EnableTracing": true
  },

  "Authentication": {
    "Authority": "https://your-idp.com",
    "Audience": "plugin-runtime-api",
    "RequireHttpsMetadata": true
  },

  "Storage": {
    "Provider": "FileSystem",
    "BasePath": "/data/plugin-storage",
    "MaxUploadSizeMb": 100
  },

  "RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "QueueLimit": 10
  }
}
```

---

# 4. ENVIRONMENT VARIABLES MAPPING

| Setting | Environment Variable |
|---------|---------------------|
| Database:ConnectionString | `DATABASE__CONNECTIONSTRING` |
| Redis:ConnectionString | `REDIS__CONNECTIONSTRING` |
| Security:KeyVaultUri | `SECURITY__KEYVAULTURI` |
| Authentication:Authority | `AUTHENTICATION__AUTHORITY` |
| Observability:OtlpEndpoint | `OBSERVABILITY__OTLPENDPOINT` |

Convention: double underscore (`__`) separates section hierarchy.

---

# 5. STRONGLY TYPED OPTIONS

```csharp
public class RuntimeOptions
{
    public int DefaultTimeoutMs { get; set; } = 5000;
    public int MaxMemoryMb { get; set; } = 256;
    public int MaxConcurrentExecutions { get; set; } = 50;
    public string PluginStoragePath { get; set; } = "/data/plugins";
    public bool EnableHotReload { get; set; } = true;
}

public class SecurityOptions
{
    public string SignatureAlgorithm { get; set; } = "RSA-SHA256";
    public string KeyVaultUri { get; set; } = "";
    public bool RevocationCheckEnabled { get; set; } = true;
    public int ManifestExpirationBufferMinutes { get; set; } = 5;
}

public class DatabaseOptions
{
    public string ConnectionString { get; set; } = "";
    public int MaxPoolSize { get; set; } = 100;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public bool EnableRetryOnFailure { get; set; } = true;
    public int MaxRetryCount { get; set; } = 3;
}
```

---

# 6. REGISTRATION PATTERN

```csharp
builder.Services.Configure<RuntimeOptions>(
    builder.Configuration.GetSection("Runtime"));

builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection("Security"));

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection("Database"));
```

---

# 7. SECRETS MANAGEMENT

## Development:

```bash
dotnet user-secrets set "Database:ConnectionString" "Host=..."
```

## Production:

- Azure Key Vault
- AWS Secrets Manager
- HashiCorp Vault
- Kubernetes Secrets

## NEVER:

- Commit secrets to source control
- Hardcode credentials in code
- Log connection strings

---

# 8. VALIDATION

Configuration validation at startup:

```csharp
builder.Services.AddOptionsWithValidateOnStart<RuntimeOptions>()
    .Bind(builder.Configuration.GetSection("Runtime"))
    .ValidateDataAnnotations();
```

If configuration is invalid → application MUST NOT start.

---

# 🏁 END
