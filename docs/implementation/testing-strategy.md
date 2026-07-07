# 🧪 Testing Strategy

---

# 1. PURPOSE

Định nghĩa testing approach cho toàn bộ hệ thống plugin runtime.

---

# 2. TEST FRAMEWORK

- **Unit tests**: xUnit
- **Mocking**: Moq
- **Assertions**: FluentAssertions
- **Test data**: Bogus (faker)
- **Integration tests**: Testcontainers (PostgreSQL, Redis)
- **API tests**: WebApplicationFactory (in-memory)

---

# 3. TEST PYRAMID

```
         /  E2E  \          (few, slow, high confidence)
        /----------\
       / Integration \      (moderate, uses real infra)
      /----------------\
     /    Unit Tests     \  (many, fast, isolated)
    /______________________\
```

Target coverage:

- Unit: 80%+ of all code
- Integration: critical paths (execution pipeline, security validation)
- E2E: happy path + key failure scenarios

---

# 4. TEST CATEGORIES

## 4.1 Unit Tests

Focus: isolated business logic

Targets:
- ManifestValidator
- SignatureVerifier
- HashVerifier
- CapabilityResolver
- ExecutionPipeline (mocked dependencies)
- Domain entities, value objects

Rules:
- No I/O
- No database
- No network
- Fast (< 100ms per test)

Example:

```csharp
[Fact]
public async Task ManifestValidator_RejectsExpiredManifest()
{
    var manifest = new Manifest { ExpiresAt = DateTime.UtcNow.AddDays(-1) };
    var validator = new ManifestValidator();

    var result = await validator.ValidateAsync(manifest, CancellationToken.None);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Code == "MFT-003");
}
```

---

## 4.2 Integration Tests

Focus: component interaction with real infrastructure

Targets:
- EF Core repositories (Testcontainers PostgreSQL)
- Redis caching
- Plugin loading (AssemblyLoadContext)
- Full execution pipeline with real DB

Rules:
- Use Testcontainers for infrastructure
- Test real SQL queries
- Test real serialization
- Slower — run separately from unit tests

Example:

```csharp
public class PluginRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task CanStoreAndRetrievePlugin()
    {
        var context = CreateDbContext(_postgres.GetConnectionString());
        var repo = new PluginRepository(context);
        // ...
    }
}
```

---

## 4.3 API Tests

Focus: HTTP endpoint behavior

Targets:
- Request validation
- Authentication/Authorization
- Error response format
- Content negotiation

Tool: `WebApplicationFactory<Program>`

Example:

```csharp
[Fact]
public async Task ExecutePlugin_WithoutAuth_Returns401()
{
    var client = _factory.CreateClient();

    var response = await client.PostAsync("/api/v1/execute/test-plugin", null);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

---

## 4.4 Security Tests

Focus: security boundaries are enforced

Targets:
- Invalid signature → rejected
- Tampered hash → rejected
- Revoked plugin → rejected
- Capability bypass attempt → denied
- Expired manifest → rejected

These are the MOST IMPORTANT tests in the system.

---

# 5. TEST NAMING CONVENTION

Pattern: `{Method}_{Scenario}_{ExpectedResult}`

Examples:
- `ValidateManifest_WhenExpired_ReturnsInvalid`
- `ExecutePlugin_WhenCapabilityDenied_ThrowsCapabilityDeniedException`
- `LoadPlugin_WhenHashMismatch_RejectsExecution`

---

# 6. TEST DATA

Use Bogus for generating test data:

```csharp
var pluginFaker = new Faker<Plugin>()
    .RuleFor(p => p.PluginId, f => Guid.NewGuid())
    .RuleFor(p => p.Name, f => f.Commerce.ProductName())
    .RuleFor(p => p.Status, PluginStatus.Approved);
```

---

# 7. TEST ISOLATION RULES

- Each test runs independently
- No shared mutable state between tests
- Use `IAsyncLifetime` for setup/teardown
- Integration tests use fresh containers

---

# 8. CI INTEGRATION

```bash
# Run unit tests only (fast feedback)
dotnet test --filter "Category=Unit"

# Run integration tests (requires Docker)
dotnet test --filter "Category=Integration"

# Run all tests
dotnet test
```

---

# 9. WHAT NOT TO TEST

- Framework code (EF Core internals, ASP.NET Core pipeline)
- Third-party library behavior
- Trivial properties (getters/setters)
- Private methods directly (test through public API)

---

# 🏁 END
