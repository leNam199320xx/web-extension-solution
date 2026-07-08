namespace PluginRuntime.Api.Modules.Gateway;

public static class GatewayModuleExtensions
{
    public static IServiceCollection AddGatewayModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Module services will be registered here
        return services;
    }

    public static WebApplication MapGatewayEndpoints(this WebApplication app)
    {
        // Module endpoints will be mapped here
        return app;
    }
}
