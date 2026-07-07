namespace PluginRuntime.Admin;

/// <summary>
/// Provides Bearer JWT tokens for API communication.
/// In a real implementation, this would integrate with the authentication system.
/// </summary>
public sealed class AuthTokenProvider
{
    private string? _token;

    public string? GetToken() => _token;

    public void SetToken(string token) => _token = token;

    public void ClearToken() => _token = null;
}
