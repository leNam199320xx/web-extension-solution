namespace PluginRuntime.Api.Modules.Plugins;

public static class PluginsModuleExtensions
{
    public static IServiceCollection AddPluginsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Plugin module services will be registered here as integration progresses
        // The Plugins module integrates with existing plugin management code;
        // focus is on wiring the module boundary and enforcing plan limits.
        return services;
    }

    public static WebApplication MapPluginsEndpoints(this WebApplication app)
    {
        // Controller-based routing is used — endpoints are discovered via AddControllers()
        return app;
    }
}
