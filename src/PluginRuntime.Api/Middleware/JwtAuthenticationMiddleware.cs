using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PluginRuntime.Api.Middleware;

/// <summary>
/// Validates JWT bearer tokens from the Authorization header.
/// Skips authentication for public endpoints: /health, /ready, /metrics, and Stripe webhooks.
/// On successful validation, populates HttpContext.User with claims.
/// </summary>
public sealed class JwtAuthenticationMiddleware : IMiddleware
{
    private static readonly HashSet<string> SkipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/ready",
        "/metrics",
        "/api/auth",
        "/api/v1",
        "/api/billing/webhooks/stripe",
        "/swagger"
    };

    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(IConfiguration configuration, ILogger<JwtAuthenticationMiddleware> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip auth for public endpoints
        if (ShouldSkip(path))
        {
            await next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "UA-AUTH-002",
                    message = "Authorization header required",
                    traceId = context.TraceIdentifier,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            });
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            var principal = ValidateToken(token);
            context.User = principal;
            await next(context);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "JWT validation failed");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "UA-AUTH-002",
                    message = "Invalid or expired token",
                    traceId = context.TraceIdentifier,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            });
        }
    }

    private ClaimsPrincipal ValidateToken(string token)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secret = jwtSettings["Secret"] ?? "default-development-secret-key-at-least-32-chars!";
        var issuer = jwtSettings["Issuer"] ?? "PluginRuntime";
        var audience = jwtSettings["Audience"] ?? "PluginRuntime";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token, validationParameters, out _);
    }

    private static bool ShouldSkip(string path)
    {
        return SkipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}
