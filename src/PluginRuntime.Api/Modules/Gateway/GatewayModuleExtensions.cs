using PluginRuntime.Api.Modules.Gateway.EventHandlers;
using PluginRuntime.Api.Modules.Gateway.Services;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Interfaces;
using StackExchange.Redis;

namespace PluginRuntime.Api.Modules.Gateway;

public static class GatewayModuleExtensions
{
    public static IServiceCollection AddGatewayModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Redis connection multiplexer for pub/sub (abortConnect=false for graceful degradation)
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false";
        if (!redisConnectionString.Contains("abortConnect", StringComparison.OrdinalIgnoreCase))
            redisConnectionString += ",abortConnect=false";

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        // Services
        services.AddScoped<IPluginAccessResolver, PluginAccessResolver>();
        services.AddScoped<IGatewayNotificationService, GatewayNotificationService>();

        // Event handlers
        services.AddScoped<IDomainEventHandler<PlanChanged>, PlanChangedHandler>();
        services.AddScoped<IDomainEventHandler<TenantStatusChanged>, TenantStatusChangedHandler>();
        services.AddScoped<IDomainEventHandler<KeyRevoked>, KeyRevokedHandler>();
        services.AddScoped<IDomainEventHandler<PackageSubscribed>, PackageSubscribedHandler>();
        services.AddScoped<IDomainEventHandler<PackageUnsubscribed>, PackageUnsubscribedHandler>();
        services.AddScoped<IDomainEventHandler<PackageCompositionChanged>, PackageCompositionChangedHandler>();

        return services;
    }

    public static WebApplication MapGatewayEndpoints(this WebApplication app)
    {
        // Gateway module has no public endpoints — it only reacts to domain events
        // and publishes Redis notifications for the external Public API Gateway.
        return app;
    }
}
