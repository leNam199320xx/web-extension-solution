using System.Text;
using PluginRuntime.Capabilities.Abstractions;
using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Capabilities.Network;

/// <summary>
/// Provides controlled HTTP access for plugins with domain allowlisting,
/// response size limits, and timeout enforcement.
/// </summary>
public class NetworkCapability : INetworkCapability
{
    private readonly Guid _pluginId;
    private readonly HttpClient _httpClient;
    private readonly IReadOnlySet<string> _allowedDomains;
    private const int MaxResponseSizeBytes = 10 * 1024 * 1024; // 10 MB

    public string Name => "network";
    public string Version => "1.0";

    public NetworkCapability(Guid pluginId, HttpClient httpClient, IReadOnlySet<string> allowedDomains)
    {
        _pluginId = pluginId;
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _allowedDomains = allowedDomains ?? throw new ArgumentNullException(nameof(allowedDomains));
    }

    public async Task<NetworkResponse> SendAsync(NetworkRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Url))
            throw new ArgumentException("URL must not be empty.", nameof(request));

        // Validate domain is in allowed list
        var uri = new Uri(request.Url);
        if (!IsDomainAllowed(uri.Host))
        {
            throw new InvalidOperationException(
                $"Domain '{uri.Host}' is not in the allowed domains list for plugin '{_pluginId}'.");
        }

        // Create HTTP request
        using var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), uri);

        // Add headers
        foreach (var header in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Add body if present
        if (request.Body is not null)
        {
            httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
        }

        // Enforce timeout from NetworkRequest.TimeoutMs via linked CancellationTokenSource
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(request.TimeoutMs));

        // Send request with response headers read first (streaming body)
        using var httpResponse = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            timeoutCts.Token);

        // Check Content-Length header if available for early rejection
        if (httpResponse.Content.Headers.ContentLength > MaxResponseSizeBytes)
        {
            throw new InvalidOperationException(
                $"Response size exceeds maximum of {MaxResponseSizeBytes / (1024 * 1024)} MB.");
        }

        // Read response body with streaming size limit enforcement
        var responseBody = await ReadResponseWithLimitAsync(httpResponse, timeoutCts.Token);

        // Build response headers
        var responseHeaders = new Dictionary<string, string>();
        foreach (var header in httpResponse.Headers)
        {
            responseHeaders[header.Key] = string.Join(", ", header.Value);
        }
        foreach (var header in httpResponse.Content.Headers)
        {
            responseHeaders[header.Key] = string.Join(", ", header.Value);
        }

        return new NetworkResponse
        {
            StatusCode = (int)httpResponse.StatusCode,
            Body = responseBody,
            Headers = responseHeaders
        };
    }

    private bool IsDomainAllowed(string host)
    {
        // Check exact match
        if (_allowedDomains.Contains(host))
            return true;

        // Check wildcard: if allowed_domains contains "*.example.com", allow "sub.example.com"
        foreach (var domain in _allowedDomains)
        {
            if (domain.StartsWith("*.") &&
                host.EndsWith(domain[1..], StringComparison.OrdinalIgnoreCase) &&
                host.Length > domain.Length - 1)
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<string> ReadResponseWithLimitAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var buffer = new char[8192];
        var totalBytesRead = 0L;
        var result = new StringBuilder();

        int charsRead;
        while ((charsRead = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            // Estimate bytes read (UTF-8 chars can be multi-byte, but this is a safe approximation
            // for size limiting since we're counting the body content)
            totalBytesRead += Encoding.UTF8.GetByteCount(buffer, 0, charsRead);

            if (totalBytesRead > MaxResponseSizeBytes)
            {
                throw new InvalidOperationException(
                    $"Response size exceeds maximum of {MaxResponseSizeBytes / (1024 * 1024)} MB.");
            }

            result.Append(buffer, 0, charsRead);
        }

        return result.ToString();
    }
}
