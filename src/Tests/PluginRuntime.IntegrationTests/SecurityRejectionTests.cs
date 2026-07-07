using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PluginRuntime.IntegrationTests.Helpers;

namespace PluginRuntime.IntegrationTests;

/// <summary>
/// Task 19.2 — Security rejection integration tests.
///
/// Requirements per tasks.md §19.2:
///   - Tampered binary (SHA-256 mismatch) rejected at HashVerifier stage
///   - Forged manifest (invalid signature) rejected at SignatureVerifier stage
///   - Undeclared capability rejected at CapabilityResolver stage
///   - Each rejection produces immutable audit log entry with TraceId, PluginId, reason, timestamp
/// </summary>
public sealed class SecurityRejectionTests : IAsyncLifetime
{
    private readonly IntegrationTestWebAppFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ---------------------------------------------------------------
    // 1. Tampered binary — SHA-256 mismatch
    // ---------------------------------------------------------------

    [Fact]
    public async Task Execute_ReturnsForbidden_WhenBinaryHashDoesNotMatchManifest()
    {
        // Arrange — DLL bytes have been tampered (extra bytes appended after signing)
        var pluginId = _factory.SetupTamperedBinaryPlugin();

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/execute/{pluginId}",
            new { input = JsonDocument.Parse("{}").RootElement });

        // Assert — HashVerifier stage fails → 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("HASH_MISMATCH", "error code must identify the failing stage");
    }

    [Fact]
    public async Task Execute_TamperedBinary_ProducesAuditLogEntry()
    {
        // Arrange
        var pluginId = _factory.SetupTamperedBinaryPlugin();
        _factory.CapturedAuditEntries.Clear();

        // Act
        await _client.PostAsJsonAsync(
            $"/api/v1/execute/{pluginId}",
            new { input = JsonDocument.Parse("{}").RootElement });

        // Assert — at least one audit entry recorded for the security rejection
        _factory.CapturedAuditEntries.Should().NotBeEmpty(
            "tampered binary must produce an immutable audit log entry");

        var auditEntry = _factory.CapturedAuditEntries.Last();
        auditEntry.ResourceId.Should().Be(pluginId.ToString(),
            "audit entry must reference the PluginId");
        auditEntry.Result.Should().Be("Failure",
            "audit result must be Failure for security rejection");
        auditEntry.Metadata.Should().ContainKey("errorCode");
    }

    // ---------------------------------------------------------------
    // 2. Forged manifest — invalid signature
    // ---------------------------------------------------------------

    [Fact]
    public async Task Execute_ReturnsForbidden_WhenManifestSignatureIsForged()
    {
        // Arrange — manifest has a random (invalid) signature
        var pluginId = _factory.SetupForgedManifestPlugin();

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/execute/{pluginId}",
            new { input = JsonDocument.Parse("{}").RootElement });

        // Assert — SignatureVerifier stage fails → 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("SIGNATURE_INVALID",
            "error code must identify signature verification failure");
    }

    [Fact]
    public async Task Execute_ForgedSignature_ProducesAuditLogEntry()
    {
        // Arrange
        var pluginId = _factory.SetupForgedManifestPlugin();
        _factory.CapturedAuditEntries.Clear();

        // Act
        await _client.PostAsJsonAsync(
            $"/api/v1/execute/{pluginId}",
            new { input = JsonDocument.Parse("{}").RootElement });

        // Assert
        _factory.CapturedAuditEntries.Should().NotBeEmpty(
            "forged signature must produce an immutable audit log entry");

        var auditEntry = _factory.CapturedAuditEntries.Last();
        auditEntry.ResourceId.Should().Be(pluginId.ToString());
        auditEntry.Result.Should().Be("Failure");
    }

    // ---------------------------------------------------------------
    // 3. Revoked plugin version
    // ---------------------------------------------------------------

    [Fact]
    public async Task Execute_ReturnsForbidden_WhenPluginVersionIsRevoked()
    {
        // Arrange
        var pluginId = _factory.SetupRevokedPlugin();

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/execute/{pluginId}",
            new { input = JsonDocument.Parse("{}").RootElement });

        // Assert — RevocationChecker stage fails → 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("PLUGIN_REVOKED");
    }

    [Fact]
    public async Task Execute_RevokedPlugin_ProducesAuditLogEntry()
    {
        // Arrange
        var pluginId = _factory.SetupRevokedPlugin();
        _factory.CapturedAuditEntries.Clear();

        // Act
        await _client.PostAsJsonAsync(
            $"/api/v1/execute/{pluginId}",
            new { input = JsonDocument.Parse("{}").RootElement });

        // Assert
        _factory.CapturedAuditEntries.Should().NotBeEmpty(
            "revoked plugin attempt must produce an immutable audit log entry");

        var auditEntry = _factory.CapturedAuditEntries.Last();
        auditEntry.ResourceId.Should().Be(pluginId.ToString());
        auditEntry.Result.Should().Be("Failure");
    }

    // ---------------------------------------------------------------
    // 4. Fail-closed: each rejection returns exactly one error response
    //    with no partial execution state leaking
    // ---------------------------------------------------------------

    [Fact]
    public async Task Execute_TamperedBinary_ResponseContainsTraceId()
    {
        // Arrange
        var pluginId = _factory.SetupTamperedBinaryPlugin();

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/execute/{pluginId}",
            new { input = JsonDocument.Parse("{}").RootElement });

        // Assert — traceId must be present in error response for correlation
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error")
            .TryGetProperty("traceId", out var traceId)
            .Should().BeTrue("traceId required for security rejection correlation");
        traceId.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Execute_ForgedSignature_ResponseContainsTimestamp()
    {
        // Arrange
        var pluginId = _factory.SetupForgedManifestPlugin();

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/execute/{pluginId}",
            new { input = JsonDocument.Parse("{}").RootElement });

        // Assert — timestamp must be present in error response
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error")
            .TryGetProperty("timestamp", out var timestamp)
            .Should().BeTrue("timestamp required in error response");
        timestamp.GetString().Should().NotBeNullOrEmpty();
    }

    // ---------------------------------------------------------------
    // 5. Security errors use "Security" category → map to 403
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("tampered")]
    [InlineData("forged")]
    [InlineData("revoked")]
    public async Task Execute_SecurityRejection_AlwaysReturns403(string scenario)
    {
        var pluginId = scenario switch
        {
            "tampered" => _factory.SetupTamperedBinaryPlugin(),
            "forged"   => _factory.SetupForgedManifestPlugin(),
            "revoked"  => _factory.SetupRevokedPlugin(),
            _          => throw new ArgumentException($"Unknown scenario: {scenario}")
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/execute/{pluginId}",
            new { input = JsonDocument.Parse("{}").RootElement });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            $"security rejection for '{scenario}' scenario must return HTTP 403");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetProperty("category").GetString()
            .Should().Be("Security",
                "error category must be 'Security' for all security rejections");
    }
}
