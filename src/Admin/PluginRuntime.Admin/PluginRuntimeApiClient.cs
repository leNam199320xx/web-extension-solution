using System.Net.Http.Headers;

namespace PluginRuntime.Admin;

/// <summary>
/// Typed HttpClient for communicating with PluginRuntime.Api.
/// Automatically attaches Bearer JWT token to all requests.
/// </summary>
public sealed class PluginRuntimeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthTokenProvider _tokenProvider;

    public PluginRuntimeApiClient(HttpClient httpClient, AuthTokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
    }

    public HttpClient Http
    {
        get
        {
            var token = _tokenProvider.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
            return _httpClient;
        }
    }
}
