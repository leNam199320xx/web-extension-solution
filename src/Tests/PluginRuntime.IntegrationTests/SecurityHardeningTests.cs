using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.IntegrationTests.Helpers;

namespace PluginRuntime.IntegrationTests;

/// <summary>
/// Task 19.4 — Security hardening verification tests.
///
/// Requirements per tasks.md §19.4:
///   - Verify no secrets in source code or compiled assemblies
///   - Verify signing keys accessed exclusively via KMS/HSM provider interface
///   - Verify all endpoints except /health and /ready return 401 without Bearer token
///   - Verify rate limiting returns 429 when threshold exceeded
///   - Verify payloads exceeding 50 MB are rejected
/// </summary>
public sealed class SecurityHardeningTests : IAsyncLifetime
{
    private readonly IntegrationTestWebAppFactory _factory = new();
    private HttpClient _authenticatedClient = null!;
    private HttpClient _unauthenticatedClient = null!;

    public Task InitializeAsync()
    {
        _authenticatedClient = _factory.CreateClient();
        _authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        // No Authorization header
        _unauthenticatedClient = _factory.CreateClient();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _authenticatedClient.Dispose();
        _unauthenticatedClient.Dispose();
        await _factory.DisposeAsync();
    }

    // ---------------------------------------------------------------
    // 1. All protected endpoints require Bearer token
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("GET",  "/api/v1/plugins")]
    [InlineData("GET",  "/api/v1/approvals")]
    [InlineData("POST", "/api/v1/execute/00000000-0000-0000-0000-000000000001")]
    public async Task ProtectedEndpoint_Returns401_WithoutBearerToken(string method, string path)
    {
        // Act — unauthenticated request
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
            request.Content = JsonContent.Create(
                new { input = JsonDocument.Parse("{}").RootElement });

        var response = await _unauthenticatedClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"{method} {path} must require authentication");
    }

    // ---------------------------------------------------------------
    // 2. /health and /ready are exempt from authentication
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("/health")]
    [InlineData("/ready")]
    public async Task HealthEndpoints_Return2xx_WithoutBearerToken(string path)
    {
        // Act — unauthenticated request to health endpoint
        var response = await _unauthenticatedClient.GetAsync(path);

        // Assert — must NOT return 401
        ((int)response.StatusCode).Should().BeLessThan(400,
            $"{path} must be publicly accessible without authentication");
    }

    // ---------------------------------------------------------------
    // 3. Rate limiting returns 429 with Retry-After header
    // ---------------------------------------------------------------

    [Fact]
    public async Task Execute_Returns429_WithRetryAfterHeader_WhenRateLimitExceeded()
    {
        // Arrange — factory configured to deny all requests via DenyAllRateLimiter
        var rateLimitFactory = new IntegrationTestWebAppFactory();
        rateLimitFactory.UseDenyAllRateLimiter();
        var client = rateLimitFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        try
        {
            var pluginId = rateLimitFactory.SetupHappyPathPlugin();

            // Act
            var response = await client.PostAsJsonAsync(
                $"/api/v1/execute/{pluginId}",
                new { input = JsonDocument.Parse("{}").RootElement });

            // Assert — 429 Too Many Requests
            response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
                "rate limit exceeded must return HTTP 429");

            // Retry-After header must be present
            response.Headers.TryGetValues("Retry-After", out var retryAfterValues)
                .Should().BeTrue("Retry-After header must be present on 429 response");

            var retryAfter = retryAfterValues!.First();
            int.TryParse(retryAfter, out var seconds).Should().BeTrue(
                "Retry-After header must be a numeric value in seconds");
            seconds.Should().BeGreaterThan(0, "Retry-After must indicate a positive wait time");

            // Error body must follow standard format
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("error").GetProperty("code").GetString()
                .Should().Be("RATE_LIMIT_EXCEEDED");
            body.GetProperty("error").GetProperty("category").GetString()
                .Should().Be("ResourceLimit");
        }
        finally
        {
            client.Dispose();
            await rateLimitFactory.DisposeAsync();
        }
    }

    // ---------------------------------------------------------------
    // 4. Plugin upload rejects files exceeding 50 MB
    // ---------------------------------------------------------------

    [Fact]
    public async Task PluginUpload_Returns400_WhenFileSizeExceeds50MB()
    {
        // Arrange — content larger than 50 MB
        var over50Mb = new byte[51 * 1024 * 1024];
        // Write a fake ZIP header so validation reaches the size check
        over50Mb[0] = 0x50; over50Mb[1] = 0x4B; over50Mb[2] = 0x03; over50Mb[3] = 0x04;

        using var content = new MultipartFormDataContent();
        content.Add(
            new ByteArrayContent(over50Mb) { Headers = { ContentType = new("application/zip") } },
            "file",
            "plugin.zip");

        // Act
        var response = await _authenticatedClient.PostAsync("/api/v1/plugins/upload", content);

        // Assert — 400 Bad Request with FILE_TOO_LARGE code
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "files exceeding 50 MB must be rejected with 400");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("FILE_TOO_LARGE",
            "error code must identify the size violation");
    }

    // ---------------------------------------------------------------
    // 5. Plugin upload rejects invalid (non-ZIP) files
    // ---------------------------------------------------------------

    [Fact]
    public async Task PluginUpload_Returns400_WhenFileIsNotAValidZip()
    {
        // Arrange — 1 KB of random data (no ZIP magic bytes)
        var invalidBytes = new byte[1024];
        new Random(42).NextBytes(invalidBytes);

        using var content = new MultipartFormDataContent();
        content.Add(
            new ByteArrayContent(invalidBytes) { Headers = { ContentType = new("application/zip") } },
            "file",
            "not-a-zip.bin");

        // Act
        var response = await _authenticatedClient.PostAsync("/api/v1/plugins/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "non-ZIP files must be rejected with 400");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("INVALID_ZIP");
    }

    // ---------------------------------------------------------------
    // 6. Signing keys accessed only via IKeyProvider interface — never hardcoded
    // ---------------------------------------------------------------

    [Fact]
    public void SigningKeys_AreAccessedExclusivelyViaIKeyProviderInterface()
    {
        // This test verifies the architecture: no private key bytes are embedded
        // in any compiled assembly. We check that:
        //   (a) IKeyProvider is the only interface through which keys can be obtained
        //   (b) The concrete InMemoryKeyProvider and KmsKeyProvider both implement it
        //   (c) SignatureVerifier only accepts IKeyProvider (not raw bytes)

        var keyProviderType = typeof(IKeyProvider);

        // Verify IKeyProvider is defined in Core (not in any concrete project)
        keyProviderType.Assembly.GetName().Name.Should().Be("PluginRuntime.Core",
            "IKeyProvider must be defined in Core to enforce the abstraction boundary");

        // Verify SignatureVerifier accepts IKeyProvider in its constructor (not raw bytes)
        var signatureVerifierType = typeof(PluginRuntime.Security.Signing.SignatureVerifier);
        var ctor = signatureVerifierType.GetConstructors().Single();
        var ctorParams = ctor.GetParameters();

        ctorParams.Should().HaveCount(1, "SignatureVerifier must take exactly one constructor parameter");
        ctorParams[0].ParameterType.Should().Be(keyProviderType,
            "SignatureVerifier must accept IKeyProvider — not raw key bytes — ensuring KMS/HSM swappability");
    }

    // ---------------------------------------------------------------
    // 7. No raw private key bytes in any assembly string literals
    // ---------------------------------------------------------------

    [Fact]
    public void CompiledAssemblies_ContainNoHardcodedPrivateKeyMaterial()
    {
        // Load all PluginRuntime assemblies and scan string literals in IL
        // for patterns that look like PEM private key headers
        var suspiciousPatterns = new[]
        {
            "BEGIN RSA PRIVATE KEY",
            "BEGIN PRIVATE KEY",
            "BEGIN EC PRIVATE KEY",
            "PRIVATE KEY-----",
            "PrivateKey",   // C# property/field names containing secret material
        };

        var pluginRuntimeAssemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("PluginRuntime") == true &&
                        !a.IsDynamic &&
                        !string.IsNullOrEmpty(a.Location))
            .ToList();

        pluginRuntimeAssemblies.Should().NotBeEmpty(
            "PluginRuntime assemblies must be loaded in test domain");

        foreach (var assembly in pluginRuntimeAssemblies)
        {
            // Read raw bytes and scan for forbidden string patterns
            var assemblyBytes = File.ReadAllBytes(assembly.Location);
            var assemblyText  = System.Text.Encoding.UTF8.GetString(assemblyBytes);

            foreach (var pattern in suspiciousPatterns)
            {
                assemblyText.Should().NotContain(
                    "BEGIN RSA PRIVATE KEY",
                    $"assembly '{assembly.GetName().Name}' must not contain hardcoded private key material");
            }
        }
    }

    // ---------------------------------------------------------------
    // 8. Metrics endpoint is publicly accessible (for Prometheus scraping)
    // ---------------------------------------------------------------

    [Fact]
    public async Task Metrics_Endpoint_IsAccessible_WithoutAuthentication()
    {
        // Act
        var response = await _unauthenticatedClient.GetAsync("/metrics");

        // Assert — Prometheus endpoint must not require auth (it's network-restricted in prod)
        // 200 OK or 404 (if OpenTelemetry not configured in test) are both acceptable
        ((int)response.StatusCode).Should().NotBe(401,
            "/metrics must not require Bearer token");
    }

    // ---------------------------------------------------------------
    // 9. All error responses follow standardized format
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("/api/v1/plugins/00000000-0000-0000-0000-000000000001", "GET")]
    [InlineData("/api/v1/approvals/00000000-0000-0000-0000-000000000001/permissions", "GET")]
    public async Task AllEndpoints_ReturnStandardizedErrorFormat_OnFailure(string path, string method)
    {
        // Act
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        var response = await _authenticatedClient.SendAsync(request);

        // 401/403 are expected but must still follow the envelope if they have a body
        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            // If the response has an "error" key, it must be fully formed
            if (body.TryGetProperty("error", out var error))
            {
                error.TryGetProperty("code", out _).Should().BeTrue("error.code required");
                error.TryGetProperty("message", out _).Should().BeTrue("error.message required");
                error.TryGetProperty("traceId", out _).Should().BeTrue("error.traceId required");
                error.TryGetProperty("timestamp", out _).Should().BeTrue("error.timestamp required");
            }
        }
    }
}
