using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PluginRuntime.Core.Entities;
using PluginRuntime.Core.Enums;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;
using PluginRuntime.Runtime.Capabilities;
using PluginRuntime.Runtime.Execution;
using PluginRuntime.Runtime.Loading;
using PluginRuntime.Runtime.Pipeline;
using PluginRuntime.Security.Hashing;
using PluginRuntime.Security.KeyManagement;
using PluginRuntime.Security.Manifest;
using PluginRuntime.Security.Revocation;
using PluginRuntime.Security.Signing;

namespace PluginRuntime.IntegrationTests.Helpers;

/// <summary>
/// WebApplicationFactory that replaces all infrastructure dependencies with in-memory
/// or NSubstitute fakes so integration tests run without Docker/real databases.
/// The real security/runtime implementations are wired — only storage and DB are faked.
/// </summary>
public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    // ── Fakes exposed to test classes ───────────────────────────────
    public IPluginVersionRepository PluginVersionRepo { get; } =
        Substitute.For<IPluginVersionRepository>();

    public IManifestRepository ManifestRepo { get; } =
        Substitute.For<IManifestRepository>();

    public IObjectStorageService ObjectStorage { get; } =
        Substitute.For<IObjectStorageService>();

    // IPluginBinaryStore is a separate interface used by PluginLoader internally;
    // it mirrors IObjectStorageService.GetPluginBinaryAsync
    public IPluginBinaryStore BinaryStore { get; } =
        Substitute.For<IPluginBinaryStore>();

    public IRevocationRepository RevocationRepo { get; } =
        Substitute.For<IRevocationRepository>();

    public IAuditLogRepository AuditLogRepo { get; } =
        Substitute.For<IAuditLogRepository>();

    public IAuditLogger AuditLogger { get; } =
        Substitute.For<IAuditLogger>();

    public IObservabilityCollector ObservabilityCollector { get; } =
        Substitute.For<IObservabilityCollector>();

    public IHealthCheckService HealthCheckService { get; } =
        Substitute.For<IHealthCheckService>();

    public InMemoryKeyProvider KeyProvider { get; } = new();

    // In-memory rate limiter (allows all by default)
    public IRateLimiter RateLimiter { get; private set; } = new AllowAllRateLimiter();

    // In-memory cache (no Redis needed)
    public InMemoryCacheService Cache { get; } = new();

    // Captured audit entries for assertions in security tests
    public List<AuditEntry> CapturedAuditEntries { get; } = [];

    /// <summary>
    /// Call before CreateClient() to make all rate-limit checks deny.
    /// </summary>
    public void UseDenyAllRateLimiter() => RateLimiter = new DenyAllRateLimiter();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Disable JWT validation in test environment
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Authority"] = "https://test.auth",
                ["Jwt:Audience"] = "test-api",
                ["Jwt:Issuer"]   = "https://test.auth",
            });
        });

        builder.ConfigureServices(services =>
        {
            // ── Key provider ────────────────────────────────────────
            KeyProvider.AddKey(TestPluginFactory.TestKeyId, TestPluginFactory.PublicKeyBytes);
            ReplaceOrAdd<IKeyProvider>(services, KeyProvider);

            // ── Cache ───────────────────────────────────────────────
            ReplaceOrAdd<ICacheService>(services, Cache);

            // ── Rate limiter ────────────────────────────────────────
            ReplaceOrAdd<IRateLimiter>(services, RateLimiter);

            // ── Repositories (infrastructure fakes) ─────────────────
            ReplaceOrAdd(services, PluginVersionRepo);
            ReplaceOrAdd(services, ManifestRepo);
            ReplaceOrAdd(services, RevocationRepo);
            ReplaceOrAdd(services, AuditLogRepo);

            // ── Storage fakes (IObjectStorageService + IPluginBinaryStore) ──
            ReplaceOrAdd<IObjectStorageService>(services, ObjectStorage);
            ReplaceOrAdd<IPluginBinaryStore>(services, BinaryStore);

            // ── Audit logger — capture entries for assertions ────────
            AuditLogger
                .When(x => x.LogAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>()))
                .Do(ci => CapturedAuditEntries.Add(ci.Arg<AuditEntry>() ?? Arg.Any<AuditEntry>()));
            AuditLogger
                .LogAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            ReplaceOrAdd(services, AuditLogger);

            // ── Observability ────────────────────────────────────────
            ObservabilityCollector
                .RecordExecutionAsync(Arg.Any<Execution>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            ReplaceOrAdd(services, ObservabilityCollector);

            // ── Health checks ────────────────────────────────────────
            var healthyResult = new HealthCheckResult(true,
                new Dictionary<string, HealthCheckEntry>
                {
                    ["database"] = new("Healthy", null),
                    ["redis"]    = new("Healthy", null),
                    ["storage"]  = new("Healthy", null)
                });
            HealthCheckService.CheckHealthAsync(Arg.Any<CancellationToken>()).Returns(healthyResult);
            HealthCheckService.CheckReadinessAsync(Arg.Any<CancellationToken>()).Returns(healthyResult);
            ReplaceOrAdd(services, HealthCheckService);

            // ── Default revocation behaviour (not revoked) ───────────
            RevocationRepo
                .GetByVersionIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns((RevocationRecord?)null);

            // ── Real security implementations ────────────────────────
            ReplaceOrAdd<IManifestValidator>(services, new ManifestValidator());
            ReplaceOrAdd<IHashVerifier>(services, new HashVerifier());
            ReplaceOrAdd<ISignatureVerifier>(services, new SignatureVerifier(KeyProvider));
            ReplaceOrAdd<IRevocationChecker>(services,
                new RevocationChecker(Cache, RevocationRepo));

            // ── Real runtime implementations ─────────────────────────
            var capabilityResolver = new CapabilityResolver(
                new Dictionary<string, Func<Guid, ICapability>>(), // empty: no real capabilities in tests
                AuditLogger);
            ReplaceOrAdd<ICapabilityResolver>(services, capabilityResolver);
            ReplaceOrAdd<IExecutionGovernor>(services, new ExecutionGovernor());
            ReplaceOrAdd<IPluginLoader>(services, new PluginLoader(BinaryStore));

            // ── Execution pipeline (real implementation) ─────────────
            var pipeline = new ExecutionPipeline(
                new ManifestValidator(),
                new SignatureVerifier(KeyProvider),
                new HashVerifier(),
                new RevocationChecker(Cache, RevocationRepo),
                capabilityResolver,
                new PluginLoader(BinaryStore),
                new ExecutionGovernor(),
                ObservabilityCollector,
                PluginVersionRepo,
                ManifestRepo,
                ObjectStorage,
                AuditLogger);
            ReplaceOrAdd<IExecutionPipeline>(services, pipeline);
        });
    }

    // ---------------------------------------------------------------
    // Setup helpers for test scenarios
    // ---------------------------------------------------------------

    /// <summary>
    /// Configures fakes for a complete happy-path plugin execution scenario.
    /// Returns the pluginId so tests can POST to /api/v1/execute/{pluginId}.
    /// </summary>
    public Guid SetupHappyPathPlugin(
        string version = "1.0.0",
        string[]? capabilities = null)
    {
        var pluginId  = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var dllBytes  = TestPluginFactory.GetMinimalPluginAssemblyBytes();
        var sha256    = TestPluginFactory.ComputeSha256(dllBytes);
        var manifest  = TestPluginFactory.BuildSignedManifest(versionId, dllBytes,
            capabilities: capabilities);

        var pluginVersion = new PluginVersion(
            versionId:  versionId,
            pluginId:   pluginId,
            version:    version,
            storageUri: $"plugins/{pluginId}/{versionId}/plugin.dll",
            sha256:     sha256,
            entryPoint: "TestPlugin.SimplePlugin.dll",
            entryClass: "TestPlugin.SimplePlugin",
            status:     PluginVersionStatus.Approved);

        PluginVersionRepo
            .GetLatestApprovedAsync(pluginId, Arg.Any<CancellationToken>())
            .Returns(pluginVersion);
        PluginVersionRepo
            .GetByVersionAsync(pluginId, version, Arg.Any<CancellationToken>())
            .Returns(pluginVersion);
        ManifestRepo
            .GetByVersionIdAsync(versionId, Arg.Any<CancellationToken>())
            .Returns(manifest);
        // Both storage interfaces backed by same bytes
        ObjectStorage
            .GetPluginBinaryAsync(pluginId, versionId, Arg.Any<CancellationToken>())
            .Returns(dllBytes);
        BinaryStore
            .GetPluginBinaryAsync(pluginId, versionId, Arg.Any<CancellationToken>())
            .Returns(dllBytes);

        return pluginId;
    }

    /// <summary>
    /// Sets up a plugin with a TAMPERED DLL (hash mismatch).
    /// </summary>
    public Guid SetupTamperedBinaryPlugin()
    {
        var pluginId  = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var realBytes    = TestPluginFactory.GetMinimalPluginAssemblyBytes();
        var tamperedBytes = realBytes.Concat(new byte[] { 0xFF, 0xFF }).ToArray();
        var sha256    = TestPluginFactory.ComputeSha256(realBytes); // hash of ORIGINAL, not tampered
        var manifest  = TestPluginFactory.BuildSignedManifest(versionId, realBytes);

        var pluginVersion = new PluginVersion(
            versionId:  versionId,
            pluginId:   pluginId,
            version:    "1.0.0",
            storageUri: $"plugins/{pluginId}/{versionId}/plugin.dll",
            sha256:     sha256,      // correct hash of original
            entryPoint: "TestPlugin.SimplePlugin.dll",
            entryClass: "TestPlugin.SimplePlugin",
            status:     PluginVersionStatus.Approved);

        PluginVersionRepo
            .GetLatestApprovedAsync(pluginId, Arg.Any<CancellationToken>())
            .Returns(pluginVersion);
        ManifestRepo
            .GetByVersionIdAsync(versionId, Arg.Any<CancellationToken>())
            .Returns(manifest);
        ObjectStorage
            .GetPluginBinaryAsync(pluginId, versionId, Arg.Any<CancellationToken>())
            .Returns(tamperedBytes); // returns TAMPERED bytes → hash mismatch
        BinaryStore
            .GetPluginBinaryAsync(pluginId, versionId, Arg.Any<CancellationToken>())
            .Returns(tamperedBytes);

        return pluginId;
    }

    /// <summary>
    /// Sets up a plugin with an INVALID SIGNATURE.
    /// </summary>
    public Guid SetupForgedManifestPlugin()
    {
        var pluginId  = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var dllBytes  = TestPluginFactory.GetMinimalPluginAssemblyBytes();
        var sha256    = TestPluginFactory.ComputeSha256(dllBytes);

        // Build a manifest with a garbage (forged) signature
        var forgedManifest = TestPluginFactory.BuildForgedSignatureManifest(versionId, dllBytes);

        var pluginVersion = new PluginVersion(
            versionId:  versionId,
            pluginId:   pluginId,
            version:    "1.0.0",
            storageUri: $"plugins/{pluginId}/{versionId}/plugin.dll",
            sha256:     sha256,
            entryPoint: "TestPlugin.SimplePlugin.dll",
            entryClass: "TestPlugin.SimplePlugin",
            status:     PluginVersionStatus.Approved);

        PluginVersionRepo
            .GetLatestApprovedAsync(pluginId, Arg.Any<CancellationToken>())
            .Returns(pluginVersion);
        ManifestRepo
            .GetByVersionIdAsync(versionId, Arg.Any<CancellationToken>())
            .Returns(forgedManifest);
        ObjectStorage
            .GetPluginBinaryAsync(pluginId, versionId, Arg.Any<CancellationToken>())
            .Returns(dllBytes);
        BinaryStore
            .GetPluginBinaryAsync(pluginId, versionId, Arg.Any<CancellationToken>())
            .Returns(dllBytes);

        return pluginId;
    }

    /// <summary>
    /// Sets up a REVOKED plugin version.
    /// </summary>
    public Guid SetupRevokedPlugin()
    {
        var pluginId  = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var dllBytes  = TestPluginFactory.GetMinimalPluginAssemblyBytes();
        var sha256    = TestPluginFactory.ComputeSha256(dllBytes);
        var manifest  = TestPluginFactory.BuildSignedManifest(versionId, dllBytes);

        var pluginVersion = new PluginVersion(
            versionId:  versionId,
            pluginId:   pluginId,
            version:    "1.0.0",
            storageUri: $"plugins/{pluginId}/{versionId}/plugin.dll",
            sha256:     sha256,
            entryPoint: "TestPlugin.SimplePlugin.dll",
            entryClass: "TestPlugin.SimplePlugin",
            status:     PluginVersionStatus.Approved);

        PluginVersionRepo
            .GetLatestApprovedAsync(pluginId, Arg.Any<CancellationToken>())
            .Returns(pluginVersion);
        ManifestRepo
            .GetByVersionIdAsync(versionId, Arg.Any<CancellationToken>())
            .Returns(manifest);
        ObjectStorage
            .GetPluginBinaryAsync(pluginId, versionId, Arg.Any<CancellationToken>())
            .Returns(dllBytes);
        BinaryStore
            .GetPluginBinaryAsync(pluginId, versionId, Arg.Any<CancellationToken>())
            .Returns(dllBytes);

        // Mark as revoked (no expiry → permanent)
        RevocationRepo
            .GetByVersionIdAsync(versionId, Arg.Any<CancellationToken>())
            .Returns(new RevocationRecord(
                RevocationId: Guid.NewGuid(),
                VersionId:    versionId,
                Reason:       "Security vulnerability",
                RevokedBy:    Guid.NewGuid(),
                RevokedAt:    DateTime.UtcNow.AddDays(-1),
                ExpiresAt:    null));

        return pluginId;
    }

    // ---------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------

    /// <summary>Wires all three fakes for a plugin: VersionRepo, ManifestRepo, and BinaryStore.</summary>
    private void WirePluginFakes(
        Guid pluginId, Guid versionId, string version,
        PluginVersion pluginVersion, Core.Entities.Manifest manifest, byte[] dllBytes)
    {
        PluginVersionRepo
            .GetLatestApprovedAsync(pluginId, Arg.Any<CancellationToken>())
            .Returns(pluginVersion);
        PluginVersionRepo
            .GetByVersionAsync(pluginId, version, Arg.Any<CancellationToken>())
            .Returns(pluginVersion);
        ManifestRepo
            .GetByVersionIdAsync(versionId, Arg.Any<CancellationToken>())
            .Returns(manifest);
        // Both storage interfaces backed by same bytes
        ObjectStorage
            .GetPluginBinaryAsync(pluginId, versionId, Arg.Any<CancellationToken>())
            .Returns(dllBytes);
        BinaryStore
            .GetPluginBinaryAsync(pluginId, versionId, Arg.Any<CancellationToken>())
            .Returns(dllBytes);
    }

    private static void ReplaceOrAdd<T>(IServiceCollection services, T implementation)
        where T : class
    {
        var existing = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var d in existing) services.Remove(d);
        services.AddSingleton(implementation);
    }
}

// ---------------------------------------------------------------
// In-process rate limiter (always allows) — no Redis needed
// ---------------------------------------------------------------
internal sealed class AllowAllRateLimiter : IRateLimiter
{
    public Task<RateLimitResult> CheckAsync(
        string key, int maxRequests, TimeSpan window, CancellationToken ct)
        => Task.FromResult(new RateLimitResult(true, maxRequests - 1, TimeSpan.Zero));
}

// ---------------------------------------------------------------
// In-process rate limiter (always denies) — for 429 testing
// ---------------------------------------------------------------
internal sealed class DenyAllRateLimiter : IRateLimiter
{
    public Task<RateLimitResult> CheckAsync(
        string key, int maxRequests, TimeSpan window, CancellationToken ct)
        => Task.FromResult(new RateLimitResult(false, 0, TimeSpan.FromSeconds(60)));
}

// ---------------------------------------------------------------
// In-memory cache service — no Redis needed
// ---------------------------------------------------------------
public sealed class InMemoryCacheService : ICacheService
{
    private readonly Dictionary<string, (object Value, DateTime? ExpiresAt)> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class
    {
        if (_store.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt is null || entry.ExpiresAt > DateTime.UtcNow)
                return Task.FromResult(entry.Value as T);
            _store.Remove(key);
        }
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken ct) where T : class
    {
        _store[key] = (value, expiration.HasValue ? DateTime.UtcNow + expiration : null);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }
}
