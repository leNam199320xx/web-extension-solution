# 📦 Dependency Manifest - NuGet Packages

---

# 1. PURPOSE

Danh sách các NuGet packages cần thiết cho implementation.

---

# 2. FRAMEWORK

- Target Framework: `net10.0`
- Language: C# 13
- SDK: `Microsoft.NET.Sdk.Web` (API project), `Microsoft.NET.Sdk` (libraries)

---

# 3. CORE PACKAGES

## ASP.NET Core (API)

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.*" />
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.*" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.*" />
```

## Entity Framework Core (Infrastructure)

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.*" />
```

## Redis

```xml
<PackageReference Include="StackExchange.Redis" Version="2.*" />
```

## Observability

```xml
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.*" />
```

## Logging

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.*" />
<PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="4.*" />
```

## Security / Crypto

```xml
<PackageReference Include="System.Security.Cryptography.Cng" Version="5.*" />
```

Note: RSA/ECDSA/SHA-256 natively available in .NET BCL — no extra packages needed for core crypto.

## Validation

```xml
<PackageReference Include="FluentValidation" Version="11.*" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.*" />
```

## JSON

```xml
<PackageReference Include="System.Text.Json" Version="10.0.*" />
```

Note: Built into .NET — explicit reference only if needed for source generators.

---

# 4. TEST PACKAGES

```xml
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="Moq" Version="4.*" />
<PackageReference Include="FluentAssertions" Version="7.*" />
<PackageReference Include="Testcontainers" Version="4.*" />
<PackageReference Include="Testcontainers.PostgreSql" Version="4.*" />
<PackageReference Include="Testcontainers.Redis" Version="4.*" />
<PackageReference Include="Bogus" Version="35.*" />
```

---

# 5. OPTIONAL / FUTURE

```xml
<!-- Health checks -->
<PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.Redis" Version="8.*" />

<!-- Rate limiting (built into ASP.NET Core 10) -->
<!-- No package needed -->

<!-- MediatR (if CQRS needed later) -->
<PackageReference Include="MediatR" Version="12.*" />
```

---

# 6. VERSION POLICY

- Use **wildcard minor versions** (`10.0.*`) during development
- Pin exact versions before production release
- Audit dependencies quarterly for CVEs

---

# 🏁 END
