using PluginRuntime.Api.Modules.Billing.BackgroundServices;
using PluginRuntime.Api.Modules.Billing.Configuration;
using PluginRuntime.Api.Modules.Billing.EventHandlers;
using PluginRuntime.Api.Modules.Billing.Services;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Billing;

public static class BillingModuleExtensions
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IDomainEventHandler<TenantCreated>, TenantCreatedHandler>();

        // Background services
        services.AddHostedService<InvoiceGenerationService>();

        return services;
    }

    public static WebApplication MapBillingEndpoints(this WebApplication app)
    {
        // Module endpoints will be mapped here
        return app;
    }
}
