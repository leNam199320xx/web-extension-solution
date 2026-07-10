using System.Text.Json;
using PluginRuntime.Api.Shared.Infrastructure;
using PluginRuntime.Api.Shared.Interfaces;
using PluginRuntime.Sdk;

namespace PluginRuntime.Api.Modules.Auth;

/// <summary>
/// System Extension: Authentication Service (system.auth)
/// Tries to load from plugins directory. Falls back to inline loading if DLL not found.
/// </summary>
public static class AuthModuleExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISystemExtension, AuthSystemExtension>();
        services.AddSingleton<SystemPluginLoader>();
        return services;
    }

    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var loader = app.Services.GetRequiredService<SystemPluginLoader>();
        var config = app.Services.GetRequiredService<IConfiguration>();

        // Read from config
        var pluginsDir = config["PluginsDirectory"] ?? "./plugins";
        var fullPath = Path.GetFullPath(pluginsDir, AppContext.BaseDirectory);

        // Log for debugging
        app.Logger.LogInformation("PluginsDirectory config value: '{Value}'", pluginsDir);
        app.Logger.LogInformation("AppContext.BaseDirectory: '{Base}'", AppContext.BaseDirectory);
        app.Logger.LogInformation("Resolved plugins path: '{Path}' (exists: {Exists})", fullPath, Directory.Exists(fullPath));

        if (Directory.Exists(fullPath))
        {
            loader.LoadFromDirectory(fullPath);
        }
        else
        {
            app.Logger.LogWarning("PluginsDirectory not found at: {Dir}", fullPath);
        }

        if (loader.Plugins.Count > 0)
        {
            // Map routes from loaded plugin
            foreach (var (id, plugin) in loader.Plugins)
            {
                foreach (var route in plugin.Manifest.Routes)
                {
                    MapPluginRoute(app, id, plugin.Instance, route);
                }
            }
        }
        else
        {
            // Fallback: map auth routes inline (no external DLL needed)
            app.Logger.LogWarning("System.Auth plugin DLL not found. Using built-in fallback auth routes.");
            MapFallbackAuthRoutes(app);
        }

        return app;
    }

    private static void MapPluginRoute(WebApplication app, string pluginId, IPlugin plugin, RouteDefinition route)
    {
        app.MapMethods(route.Path, [route.Method], async (HttpContext ctx) =>
        {
            var input = await BuildInput(ctx, route.Action);
            var context = new PluginContext
            {
                ExecutionId = Guid.NewGuid().ToString(),
                PluginId = pluginId,
                Version = "1.0.0",
                Input = JsonSerializer.SerializeToElement(input)
            };

            var result = await plugin.ExecuteAsync(context, ctx.RequestAborted);
            return ToHttpResult(result);
        });

        app.Logger.LogInformation("Mapped plugin route: {Method} {Path} → {Id}.{Action}",
            route.Method, route.Path, pluginId, route.Action);
    }

    private static void MapFallbackAuthRoutes(WebApplication app)
    {
        // Login
        app.MapPost("/api/auth/login", async (HttpContext ctx) =>
        {
            var body = await ReadBody(ctx);
            var email = GetString(body, "email");
            var password = GetString(body, "password");

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return Results.BadRequest(new { error = "Email and password are required." });

            var (valid, displayName, role, tenantId) = VerifyAccount(email, password);
            if (!valid)
                return Results.Json(new { error = "Invalid email or password." }, statusCode: 401);

            var token = JwtHelper.GenerateToken(email, displayName, role, tenantId);
            return Results.Ok(new { token, displayName, email, role, tenantId, expiresAt = DateTime.UtcNow.AddHours(24) });
        });

        // Register
        app.MapPost("/api/auth/register", async (HttpContext ctx) =>
        {
            var body = await ReadBody(ctx);
            var email = GetString(body, "email");
            var password = GetString(body, "password");
            var displayName = GetString(body, "displayName") ?? email;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return Results.BadRequest(new { error = "Email and password are required." });

            var tenantId = Guid.NewGuid();
            var token = JwtHelper.GenerateToken(email!, displayName!, "Tenant_Owner", tenantId);
            return Results.Ok(new { token, displayName, email, role = "Tenant_Owner", tenantId, expiresAt = DateTime.UtcNow.AddHours(24) });
        });

        // Me
        app.MapGet("/api/auth/me", (HttpContext ctx) =>
        {
            if (ctx.User.Identity is not { IsAuthenticated: true })
                return Results.Json(new { error = "Not authenticated." }, statusCode: 401);

            return Results.Ok(new
            {
                userId = ctx.User.FindFirst("sub")?.Value ?? "",
                email = ctx.User.FindFirst("email")?.Value ?? "",
                displayName = ctx.User.FindFirst("name")?.Value ?? "",
                role = ctx.User.FindFirst("role")?.Value ?? "",
                tenantId = ctx.User.FindFirst("tenant_id")?.Value
            });
        });

        app.Logger.LogInformation("Mapped fallback auth routes: POST /api/auth/login, POST /api/auth/register, GET /api/auth/me");
    }

    private static (bool Valid, string DisplayName, string Role, Guid? TenantId) VerifyAccount(string email, string password)
    {
        return (email.ToLower(), password) switch
        {
            ("admin@pluginruntime.internal", "admin") => (true, "Admin User", "Platform_Admin", (Guid?)Guid.Parse("b2c3d4e5-0001-0001-0001-000000000004")),
            ("alice@acme.com", "secret") => (true, "Alice Developer", "Tenant_Owner", (Guid?)Guid.Parse("b2c3d4e5-0001-0001-0001-000000000001")),
            ("bob@startuplabs.io", "secret") => (true, "Bob Startup", "Tenant_Owner", (Guid?)Guid.Parse("b2c3d4e5-0001-0001-0001-000000000002")),
            ("carol@enterprise-global.com", "secret") => (true, "Carol Enterprise", "Tenant_Owner", (Guid?)Guid.Parse("b2c3d4e5-0001-0001-0001-000000000003")),
            _ => (false, "", "", null)
        };
    }

    private static async Task<Dictionary<string, object>> BuildInput(HttpContext ctx, string action)
    {
        var input = new Dictionary<string, object> { ["action"] = action };
        if (ctx.Request.ContentLength > 0 || ctx.Request.ContentType is not null)
        {
            using var reader = new StreamReader(ctx.Request.Body);
            var body = await reader.ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
                if (parsed != null)
                    foreach (var kv in parsed)
                        input[kv.Key] = kv.Value;
            }
        }
        return input;
    }

    private static async Task<JsonElement> ReadBody(HttpContext ctx)
    {
        using var reader = new StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        if (string.IsNullOrEmpty(body)) return default;
        return JsonSerializer.Deserialize<JsonElement>(body);
    }

    private static string? GetString(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static IResult ToHttpResult(PluginResult result)
    {
        if (result.Success) return Results.Ok(result.Data);
        var statusCode = result.ErrorCode switch
        {
            "AUTH_FAILED" => 401,
            "VALIDATION" => 400,
            "CONFLICT" => 409,
            _ => 400
        };
        return Results.Json(new { error = result.ErrorCode, message = result.ErrorMessage }, statusCode: statusCode);
    }
}

/// <summary>Simple JWT token generator for fallback auth.</summary>
internal static class JwtHelper
{
    private const string Secret = "default-development-secret-key-at-least-32-chars!";

    public static string GenerateToken(string email, string displayName, string role, Guid? tenantId)
    {
        var claims = new System.Security.Claims.Claim[]
        {
            new("sub", Guid.NewGuid().ToString()),
            new("email", email),
            new("name", displayName),
            new("role", role),
            new("tenant_id", tenantId?.ToString() ?? ""),
            new("is_internal", (role == "Platform_Admin").ToString().ToLower())
        };

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Secret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "PluginRuntime",
            audience: "PluginRuntime",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class AuthSystemExtension : ISystemExtension
{
    public string ExtensionId => "system.auth";
    public string Name => "Authentication Service";
    public string Version => "1.0.0";
    public string Description => "System extension for authentication. Loads plugin DLL if available, otherwise uses built-in fallback.";
}
