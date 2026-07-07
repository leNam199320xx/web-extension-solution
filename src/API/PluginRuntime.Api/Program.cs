using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PluginRuntime.Api.Middleware;
using PluginRuntime.Api.Observability;
using PluginRuntime.Capabilities.Extension;

var builder = WebApplication.CreateBuilder(args);

// --- Structured JSON Logging ---
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions { Indented = false };
});

// Configure Kestrel with request body size limit (1 MB = 1048576 bytes)
builder.WebHost.UseKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1_048_576;
});

// --- Authentication ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience = builder.Configuration["Jwt:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// --- OpenTelemetry Tracing ---
builder.Services.AddPluginRuntimeTracing(builder.Configuration);

// --- Rate Limiting Configuration ---
builder.Services.Configure<RateLimitOptions>(
    builder.Configuration.GetSection(RateLimitOptions.SectionName));

// --- Health Checks ---
builder.Services.AddHealthChecks();

// --- Controllers with /api/v1 prefix via route convention ---
builder.Services.AddControllers();

// --- Subscription Service ---
builder.Services.AddScoped<SubscriptionService>();

// --- Metrics ---
builder.Services.AddPluginRuntimeMetrics();

var app = builder.Build();

// --- Middleware Pipeline (order matters) ---

// 1. Error handling must be first to catch all downstream errors
app.UseMiddleware<ErrorHandlingMiddleware>();

// 2. Rate limiting before authentication to protect against DDoS
app.UseMiddleware<RateLimitingMiddleware>();

// 3. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// --- Endpoints ---

// Health and readiness endpoints outside /api/v1 prefix, no auth required
app.MapHealthChecks("/health").AllowAnonymous();
app.MapGet("/ready", () => Results.Ok(new { status = "Ready" })).AllowAnonymous();

// Map controllers — all controllers use [Route("api/v1/[controller]")] for URI-based versioning
app.MapControllers();

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

// Required for integration testing with WebApplicationFactory
public partial class Program { }
