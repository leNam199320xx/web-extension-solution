using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace PluginRuntime.Marketplace.Auth;

/// <summary>
/// Auth state provider using real Auth API. Persists JWT token in sessionStorage.
/// </summary>
public sealed class ApiAuthStateProvider : AuthenticationStateProvider
{
    private const string TokenKey = "auth_token";

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private ClaimsPrincipal _user = new(new ClaimsIdentity());
    private string? _token;
    private bool _initialized;

    public string? Token => _token;
    public bool IsAuthenticated => _user.Identity?.IsAuthenticated ?? false;

    public ApiAuthStateProvider(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
            if (!response.IsSuccessStatusCode) return "Invalid email or password.";

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result is null) return "Login failed.";

            _token = result.Token;
            _user = ParseToken(result.Token);

            // Persist to sessionStorage
            await _js.InvokeVoidAsync("sessionStorage.setItem", TokenKey, _token);

            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task LogoutAsync()
    {
        _token = null;
        _user = new ClaimsPrincipal(new ClaimsIdentity());
        await _js.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // On first call, try to restore from sessionStorage
        if (!_initialized)
        {
            _initialized = true;
            try
            {
                var stored = await _js.InvokeAsync<string?>("sessionStorage.getItem", TokenKey);
                if (!string.IsNullOrEmpty(stored))
                {
                    _token = stored;
                    _user = ParseToken(stored);
                }
            }
            catch
            {
                // JS interop not available yet (prerendering)
            }
        }

        return new AuthenticationState(_user);
    }

    private static ClaimsPrincipal ParseToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var claims = jwt.Claims.ToList();

        var nameClaim = claims.FirstOrDefault(c => c.Type == "name");
        if (nameClaim != null) claims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));

        var roleClaim = claims.FirstOrDefault(c => c.Type == "role");
        if (roleClaim != null) claims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
    }

    private sealed record AuthResponse(string Token, string DisplayName, string Email, string Role, Guid? TenantId, DateTime ExpiresAt);
}
