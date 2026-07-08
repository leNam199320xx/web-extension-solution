namespace PluginRuntime.Api.Modules.Plugins;

public static class PluginsModuleExtensions
{
    public static IServiceCollection AddPluginsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Module services will be registered here
        return services;
    }

    public static WebApplication MapPluginsEndpoints(this WebApplication app)
    {
        // Module endpoints will be mapped here
        return app;
    }
}
