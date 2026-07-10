using PublicApiGateway.BackgroundServices;
using PublicApiGateway.Configuration;
using PublicApiGateway.Middleware;
using PublicApiGateway.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---

builder.Services.Configure<GatewayOptions>(builder.Configuration.GetSection(GatewayOptions.SectionName));
builder.Services.Configure<UpstreamOptions>(builder.Configuration.GetSection(UpstreamOptions.SectionName));

// --- Infrastructure ---

var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect($"{redisConnectionString},abortConnect=false"));

builder.Services.AddHttpClient("Upstream");

// --- Services ---

builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IRateLimitService, RateLimitService>();
builder.Services.AddScoped<IQuotaService, QuotaService>();
builder.Services.AddScoped<IIpBlockingService, IpBlockingService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<UsageMeteringService>();
builder.Services.AddSingleton<IUsageMeteringService>(sp => sp.GetRequiredService<UsageMeteringService>());

// --- Background Services ---

builder.Services.AddHostedService<UsageMeteringBackgroundService>();

// --- Middleware ---

builder.Services.AddTransient<GlobalExceptionMiddleware>();
builder.Services.AddTransient<SecurityHardeningMiddleware>();
builder.Services.AddTransient<CorrelationIdMiddleware>();
builder.Services.AddTransient<ApiKeyAuthenticationMiddleware>();
builder.Services.AddTransient<RateLimitingMiddleware>();
builder.Services.AddTransient<QuotaEnforcementMiddleware>();
builder.Services.AddTransient<RequestForwardingMiddleware>();

// --- OpenTelemetry ---

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("PublicApiGateway");
    });

var app = builder.Build();

// --- Middleware Pipeline (order is critical) ---
// 1. Security Hardening
// 2. Global Exception Handler
// 3. Correlation ID
// 4. API Key Authentication
// 5. Rate Limiting
// 6. Quota Enforcement
// 7. Request Forwarding

app.UseMiddleware<SecurityHardeningMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<QuotaEnforcementMiddleware>();
app.UseMiddleware<RequestForwardingMiddleware>();

// --- Infrastructure Endpoints (no auth required) ---

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/ready", () => Results.Ok(new { Status = "Ready" }));

app.Run();

public partial class Program { }
