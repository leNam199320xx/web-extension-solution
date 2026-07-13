using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Middleware;
using PluginRuntime.Api.Modules.Auth;
using PluginRuntime.Api.Modules.Billing;
using PluginRuntime.Api.Modules.Gateway;
using PluginRuntime.Api.Modules.Plugins;
using PluginRuntime.Api.Modules.Plugins.Controllers;
using PluginRuntime.Api.Modules.Subscriptions;
using PluginRuntime.Api.Modules.Tenants;
using PluginRuntime.Api.Shared.Infrastructure;
using PluginRuntime.Api.Shared.Infrastructure.Persistence;
using PluginRuntime.Api.Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// --- Infrastructure ---

// Multi-provider database: reads "DatabaseProvider" from config (PostgreSQL | SQLite | Json)
builder.Services.AddDatabaseProvider(builder.Configuration);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// --- Shared Services ---

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<ICurrentTenantContext, CurrentTenantContext>();
builder.Services.AddSingleton<MetricsCollector>();

// --- OpenAPI/Swagger ---

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "PluginRuntime Unified API",
        Version = "v1",
        Description = "Unified API Architecture — Modular Monolith hosting Plugins, Tenants, Billing, Subscriptions, and Gateway modules."
    });

    options.TagActionsBy(api =>
    {
        var path = api.RelativePath ?? string.Empty;
        return path switch
        {
            _ when path.StartsWith("api/plugins") => ["Plugins"],
            _ when path.StartsWith("api/tenants") => ["Tenants"],
            _ when path.StartsWith("api/billing") => ["Billing"],
            _ when path.StartsWith("api/subscriptions") => ["Subscriptions"],
            _ when path.StartsWith("api/admin") => ["Admin"],
            _ => ["Infrastructure"]
        };
    });
});

// --- Middleware ---

builder.Services.AddTransient<GlobalExceptionMiddleware>();
builder.Services.AddTransient<JwtAuthenticationMiddleware>();
builder.Services.AddTransient<TenantContextMiddleware>();
builder.Services.AddTransient<SecurityHeadersMiddleware>();
builder.Services.AddTransient<InputSanitizationMiddleware>();
builder.Services.AddTransient<RequestLoggingMiddleware>();

// --- OpenTelemetry ---

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("PluginRuntime.Api");
    });

// --- Module Registration ---

builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddPluginsModule(builder.Configuration);
builder.Services.AddTenantsModule(builder.Configuration);
builder.Services.AddBillingModule(builder.Configuration);
builder.Services.AddSubscriptionsModule(builder.Configuration);
builder.Services.AddGatewayModule(builder.Configuration);

var app = builder.Build();

// --- Middleware Pipeline ---
// Order: SecurityHeaders → GlobalException → InputSanitization → RequestLogging → JwtAuth → TenantContext

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PluginRuntime Unified API v1");
});

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<InputSanitizationMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseMiddleware<TenantContextMiddleware>();

// --- Endpoints ---

app.MapControllers();
app.MapAuthEndpoints();
app.MapPluginsEndpoints();
app.MapTenantsEndpoints();
app.MapBillingEndpoints();
app.MapSubscriptionsEndpoints();
app.MapGatewayEndpoints();

app.Run();

public partial class Program { }
