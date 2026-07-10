using System.Net.Http.Json;

namespace PluginRuntime.Admin;

/// <summary>
/// Provides Bearer JWT tokens for API communication.
/// Auto-acquires admin token from Auth API on first use.
/// </summary>
public sealed class AuthTokenProvider
{
    private string? _token;
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private bool _initialized;

    public AuthTokenProvider(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _http = httpClientFactory.CreateClient("AuthClient");
        _config = config;
    }

    public string? GetToken()
    {
        if (!_initialized)
        {
            _initialized = true;
            // Auto-login as admin for development
            try
            {
                var baseUrl = _config["Api:BaseUrl"] ?? "http://localhost:6100";
                _http.BaseAddress = new Uri(baseUrl);
                var response = _http.PostAsJsonAsync("api/auth/login", new
                {
                    email = "admin@pluginruntime.internal",
                    password = "admin"
                }).GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadFromJsonAsync<AuthResponse>().GetAwaiter().GetResult();
                    _token = result?.Token;
                }
            }
            catch { /* Silently fail — admin will see unauthenticated state */ }
        }
        return _token;
    }

    public void SetToken(string token) => _token = token;

    public void ClearToken() { _token = null; _initialized = false; }

    private sealed record AuthResponse(string Token, string DisplayName, string Email, string Role, Guid? TenantId, DateTime ExpiresAt);
}
