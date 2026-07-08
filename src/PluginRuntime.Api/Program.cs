using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Middleware;
using PluginRuntime.Api.Modules.Billing;
using PluginRuntime.Api.Modules.Gateway;
using PluginRuntime.Api.Modules.Plugins;
using PluginRuntime.Api.Modules.Subscriptions;
using PluginRuntime.Api.Modules.Tenants;
using PluginRuntime.Api.Shared.Infrastructure;
using PluginRuntime.Api.Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// --- Infrastructure ---

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// --- Shared Services ---

builder.Services.AddControllers();
builder.Services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<ICurrentTenantContext, CurrentTenantContext>();
builder.Services.AddTransient<GlobalExceptionMiddleware>();

// --- Module Registration ---

builder.Services.AddPluginsModule(builder.Configuration);
builder.Services.AddTenantsModule(builder.Configuration);
builder.Services.AddBillingModule(builder.Configuration);
builder.Services.AddSubscriptionsModule(builder.Configuration);
builder.Services.AddGatewayModule(builder.Configuration);

var app = builder.Build();

// --- Middleware Pipeline ---

app.UseMiddleware<GlobalExceptionMiddleware>();
// JWT Auth placeholder — will be configured in a future task
// Tenant context resolution placeholder — will be configured in a future task

// --- Endpoints ---

app.MapControllers();
app.MapPluginsEndpoints();
app.MapTenantsEndpoints();
app.MapBillingEndpoints();
app.MapSubscriptionsEndpoints();
app.MapGatewayEndpoints();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/ready", () => Results.Ok(new { Status = "Ready" }));

app.Run();

public partial class Program { }
