using PluginRuntime.Api.Modules.Tenants.Services;

namespace PluginRuntime.Api.Modules.Tenants;

public static class TenantsModuleExtensions
{
    public static IServiceCollection AddTenantsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IAuditService, AuditService>();
        return services;
    }

    public static WebApplication MapTenantsEndpoints(this WebApplication app)
    {
        // Controller endpoints are mapped via app.MapControllers() in Program.cs
        return app;
    }
}
