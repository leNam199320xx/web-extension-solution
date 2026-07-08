using PluginRuntime.Api.Modules.Subscriptions.Services;

namespace PluginRuntime.Api.Modules.Subscriptions;

public static class SubscriptionsModuleExtensions
{
    public static IServiceCollection AddSubscriptionsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPluginPackageService, PluginPackageService>();
        services.AddScoped<IPlanSubscriptionService, PlanSubscriptionService>();
        services.AddScoped<IPackageSubscriptionService, PackageSubscriptionService>();

        return services;
    }

    public static WebApplication MapSubscriptionsEndpoints(this WebApplication app)
    {
        // Module endpoints will be mapped here (controller-based routing is used)
        return app;
    }
}
