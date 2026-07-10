using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using PluginRuntime.Sdk;

namespace System.Auth;

/// <summary>
/// Auth plugin — loaded as a DLL via plugin system.
/// Handles register, login, and me actions.
/// Uses the Database capability to store/retrieve user accounts.
/// </summary>
public sealed class AuthPlugin : IPlugin
{
    private const string Secret = "default-development-secret-key-at-least-32-chars!";
    private const string Issuer = "PluginRuntime";
    private const string Audience = "PluginRuntime";

    public async Task<PluginResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken)
    {
        var action = "";
        if (context.Input.ValueKind == JsonValueKind.Object && context.Input.TryGetProperty("action", out var actionProp))
            action = actionProp.GetString() ?? "";

        return action switch
        {
            "register" => await HandleRegister(context, cancellationToken),
            "login" => await HandleLogin(context, cancellationToken),
            "me" => HandleMe(context),
            _ => Fail("INVALID_ACTION", $"Unknown action: {action}")
        };
    }

    private async Task<PluginResult> HandleRegister(PluginContext context, CancellationToken ct)
    {
        var email = GetString(context.Input, "email");
        var password = GetString(context.Input, "password");
        var displayName = GetString(context.Input, "displayName") ?? email;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Fail("VALIDATION", "Email and password are required.");

        if (password.Length < 6)
            return Fail("VALIDATION", "Password must be at least 6 characters.");

        // Check duplicate via capability (simplified — in real impl, uses IDatabaseCapability)
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var passwordHash = HashPassword(password);

        var token = GenerateToken(userId, email, displayName, "Tenant_Owner", tenantId, false);

        var output = new
        {
            token,
            displayName,
            email,
            role = "Tenant_Owner",
            tenantId,
            expiresAt = DateTime.UtcNow.AddHours(24).ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        return Success(output);
    }

    private async Task<PluginResult> HandleLogin(PluginContext context, CancellationToken ct)
    {
        var email = GetString(context.Input, "email");
        var password = GetString(context.Input, "password");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Fail("VALIDATION", "Email and password are required.");

        // In real implementation, lookup user via IDatabaseCapability
        // For now, verify against known test accounts
        var (valid, displayName, role, tenantId, isInternal) = VerifyTestAccount(email, password);

        if (!valid)
            return Fail("AUTH_FAILED", "Invalid email or password.");

        var userId = Guid.NewGuid();
        var token = GenerateToken(userId, email, displayName, role, tenantId, isInternal);

        var output = new
        {
            token,
            displayName,
            email,
            role,
            tenantId,
            expiresAt = DateTime.UtcNow.AddHours(24).ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        return Success(output);
    }

    private PluginResult HandleMe(PluginContext context)
    {
        // In real implementation, extract user from JWT in context headers
        var output = new
        {
            message = "Use the JWT token claims for user info. Call /api/auth/me via the controller."
        };
        return Success(output);
    }

    private static (bool Valid, string DisplayName, string Role, Guid? TenantId, bool IsInternal) VerifyTestAccount(string email, string password)
    {
        // Test accounts (matches UserAccount.json seed data)
        return (email.ToLower(), password) switch
        {
            ("admin@pluginruntime.internal", "admin") => (true, "Admin User", "Platform_Admin", Guid.Parse("b2c3d4e5-0001-0001-0001-000000000004"), true),
            ("alice@acme.com", "secret") => (true, "Alice Developer", "Tenant_Owner", Guid.Parse("b2c3d4e5-0001-0001-0001-000000000001"), false),
            ("bob@startuplabs.io", "secret") => (true, "Bob Startup", "Tenant_Owner", Guid.Parse("b2c3d4e5-0001-0001-0001-000000000002"), false),
            ("carol@enterprise-global.com", "secret") => (true, "Carol Enterprise", "Tenant_Owner", Guid.Parse("b2c3d4e5-0001-0001-0001-000000000003"), false),
            _ => (false, "", "", null, false)
        };
    }

    private static string GenerateToken(Guid userId, string email, string displayName, string role, Guid? tenantId, bool isInternal)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("name", displayName),
            new Claim("role", role),
            new Claim("tenant_id", tenantId?.ToString() ?? ""),
            new Claim("is_internal", isInternal.ToString().ToLower())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string? GetString(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexStringLower(bytes);
    }

    private static PluginResult Success(object data)
    {
        var json = JsonSerializer.SerializeToElement(data);
        return new PluginResult { Success = true, Data = json };
    }

    private static PluginResult Fail(string code, string message)
    {
        return new PluginResult { Success = false, ErrorCode = code, ErrorMessage = message };
    }
}
