using Microsoft.EntityFrameworkCore;
using PluginRuntime.Capabilities.Extension;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Audit;
using PluginRuntime.Infrastructure.Caching;
using PluginRuntime.Infrastructure.EventBus;
using PluginRuntime.Infrastructure.Health;
using PluginRuntime.Infrastructure.Observability;
using PluginRuntime.Infrastructure.Persistence;
using PluginRuntime.Infrastructure.RateLimiting;
using PluginRuntime.Infrastructure.Resilience;
using PluginRuntime.Infrastructure.Storage;
using PluginRuntime.Runtime.Capabilities;
using PluginRuntime.Runtime.Execution;
using PluginRuntime.Runtime.HotReload;
using PluginRuntime.Runtime.Loading;
using PluginRuntime.Runtime.Pipeline;
using PluginRuntime.Security.Hashing;
using PluginRuntime.Security.KeyManagement;
using PluginRuntime.Security.Manifest;
using PluginRuntime.Security.Revocation;
using PluginRuntime.Security.Signing;
using StackExchange.Redis;

namespace PluginRuntime.Api.Configuration;

public static class ServiceRegistration
{
    public static IServiceCollection AddPluginRuntimeServices(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Infrastructure: Database ---
        var connectionString = configuration.GetConnectionString("PostgreSQL") ?? "Host=localhost;Database=plugin_runtime;";
        services.AddDbContext<PluginRuntimeDbContext>(options =>
            options.UseNpgsql(connectionString));

        // --- Infrastructure: Redis ---
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            try { return ConnectionMultiplexer.Connect(redisConnectionString); }
            catch { return null!; } // Health checks will catch this
        });
        services.Configure<RedisCacheOptions>(configuration.GetSection("Redis"));
        services.AddSingleton<ICacheService, RedisCacheService>();

        // --- Infrastructure: Object Storage ---
        services.AddSingleton(new ObjectStorageOptions
        {
            BasePath = configuration["Storage:BasePath"] ?? "./plugin-storage",
            MaxFileSizeBytes = 50L * 1024 * 1024
        });
        services.AddSingleton<ObjectStorageService>();
        services.AddSingleton<IObjectStorageService>(sp => sp.GetRequiredService<ObjectStorageService>());
        services.AddSingleton<IPluginBinaryStore>(sp => sp.GetRequiredService<ObjectStorageService>());

        // --- Infrastructure: Rate Limiting ---
        var useRedis = configuration.GetValue<bool>("Scalability:UseRedis");
        if (useRedis)
            services.AddSingleton<IRateLimiter, RedisRateLimiter>();
        else
            services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();

        // --- Infrastructure: Event Bus ---
        if (useRedis)
            services.AddSingleton<IPluginEventBus, RedisEventBus>();
        else
            services.AddSingleton<IPluginEventBus, InMemoryEventBus>();

        // --- Infrastructure: Health ---
        services.AddScoped<IHealthCheckService, InfrastructureHealthCheckService>();

        // --- Infrastructure: Audit ---
        services.AddScoped<IAuditLogger, AuditLoggerService>();

        // --- Infrastructure: Observability ---
        services.AddScoped<IObservabilityCollector, ObservabilityCollector>();

        // --- Infrastructure: Guard ---
        services.AddSingleton<InfrastructureGuard>();

        // --- Security ---
        services.AddSingleton<IKeyProvider, InMemoryKeyProvider>();
        services.AddScoped<IManifestValidator, ManifestValidator>();
        services.AddScoped<ISignatureVerifier, SignatureVerifier>();
        services.AddScoped<IHashVerifier, HashVerifier>();
        services.AddScoped<IRevocationChecker, RevocationChecker>();

        // --- Runtime ---
        services.AddSingleton<PluginLoader>();
        services.AddSingleton<IPluginLoader>(sp => sp.GetRequiredService<PluginLoader>());
        services.AddScoped<IExecutionGovernor, ExecutionGovernor>();
        services.AddScoped<IExecutionPipeline, ExecutionPipeline>();
        services.AddSingleton<HotReloadManager>();

        // --- Capabilities ---
        services.AddScoped<ICapabilityResolver>(sp =>
        {
            var auditLogger = sp.GetRequiredService<IAuditLogger>();
            var factories = new Dictionary<string, Func<Guid, ICapability>>();
            return new CapabilityResolver(factories, auditLogger);
        });

        // --- Repositories (assembly scanning) ---
        services.ScanRepositories(
            typeof(PluginRuntimeDbContext).Assembly,
            "PluginRuntime.Infrastructure.Repositories");

        // --- Inter-Extension ---
        services.AddScoped<SubscriptionService>();

        return services;
    }

    /// <summary>
    /// Scans the given assembly for repository implementations and registers them
    /// as scoped services against their Core.Interfaces counterparts.
    /// </summary>
    private static void ScanRepositories(
        this IServiceCollection services,
        System.Reflection.Assembly implementationAssembly,
        string namespaceFilter)
    {
        var repositoryTypes = implementationAssembly.GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace?.Contains(namespaceFilter) == true);

        foreach (var implType in repositoryTypes)
        {
            var interfaces = implType.GetInterfaces()
                .Where(i => i.Namespace?.Contains("PluginRuntime.Core.Interfaces") == true);

            foreach (var iface in interfaces)
            {
                services.AddScoped(iface, implType);
            }
        }
    }
}
