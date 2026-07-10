using Microsoft.AspNetCore.SignalR.Client;

namespace PluginRuntime.Admin;

/// <summary>
/// Manages a SignalR connection to the PluginRuntime.Api runtime hub.
/// Implements reconnection logic: 5-second interval, maximum 10 attempts.
/// </summary>
public sealed class RuntimeHubConnection : IAsyncDisposable
{
    private const int MaxReconnectAttempts = 10;
    private static readonly TimeSpan ReconnectInterval = TimeSpan.FromSeconds(5);

    private HubConnection? _connection;
    private readonly IConfiguration _config;
    private readonly ILogger<RuntimeHubConnection> _logger;

    private int _reconnectAttempts;
    private CancellationTokenSource? _reconnectCts;

    public HubConnectionState State =>
        _connection?.State ?? HubConnectionState.Disconnected;

    public bool IsConnected => State == HubConnectionState.Connected;

    /// <summary>Fired whenever the connection state changes.</summary>
    public event Func<HubConnectionState, Task>? StateChanged;

    public RuntimeHubConnection(IConfiguration config, ILogger<RuntimeHubConnection> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Builds and starts the hub connection. Safe to call multiple times —
    /// returns immediately if already connected.
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        EnsureConnectionBuilt();

        if (_connection!.State != HubConnectionState.Disconnected)
            return;

        await _connection.StartAsync(ct);
        _reconnectAttempts = 0;

        if (StateChanged is not null)
            await StateChanged.Invoke(HubConnectionState.Connected);
    }

    private void EnsureConnectionBuilt()
    {
        if (_connection is not null) return;

        var hubUrl = _config["Api:HubUrl"] ?? "https://localhost:5001/hubs/runtime";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect(new ReconnectPolicy(MaxReconnectAttempts, ReconnectInterval))
            .Build();

        _connection.Reconnecting += async _ =>
        {
            _reconnectAttempts++;
            _logger.LogWarning("SignalR reconnecting (attempt {Attempt}/{Max})", _reconnectAttempts, MaxReconnectAttempts);
            if (StateChanged is not null)
                await StateChanged.Invoke(HubConnectionState.Reconnecting);
        };

        _connection.Reconnected += async _ =>
        {
            _reconnectAttempts = 0;
            _logger.LogInformation("SignalR reconnected.");
            if (StateChanged is not null)
                await StateChanged.Invoke(HubConnectionState.Connected);
        };

        _connection.Closed += async ex =>
        {
            _logger.LogError(ex, "SignalR connection closed.");
            if (StateChanged is not null)
                await StateChanged.Invoke(HubConnectionState.Disconnected);
            await AttemptManualReconnectAsync();
        };
    }

    /// <summary>
    /// Registers a handler for a hub message. Can be called before or after StartAsync.
    /// If called before StartAsync, the connection object is built lazily.
    /// </summary>
    public IDisposable On<T>(string methodName, Action<T> handler)
    {
        EnsureConnectionBuilt();
        return _connection!.On(methodName, handler);
    }

    /// <summary>
    /// Invokes a server method on the hub.
    /// </summary>
    public async Task SendAsync(string methodName, object? arg, CancellationToken ct = default)
    {
        if (_connection is null || !IsConnected)
            throw new InvalidOperationException("Hub connection is not established.");
        await _connection.SendAsync(methodName, arg, ct);
    }

    // ---------------------------------------------------------------
    // Manual reconnect loop — fires when WithAutomaticReconnect gives up
    // ---------------------------------------------------------------
    private async Task AttemptManualReconnectAsync()
    {
        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();
        var ct = _reconnectCts.Token;

        var localAttempts = 0;
        while (localAttempts < MaxReconnectAttempts && !ct.IsCancellationRequested)
        {
            await Task.Delay(ReconnectInterval, ct).ConfigureAwait(false);
            localAttempts++;
            _logger.LogWarning("Manual reconnect attempt {Attempt}/{Max}", localAttempts, MaxReconnectAttempts);

            try
            {
                if (_connection is not null)
                {
                    await _connection.StartAsync(ct);
                    _logger.LogInformation("Manual reconnect succeeded.");
                    if (StateChanged is not null)
                        await StateChanged.Invoke(HubConnectionState.Connected);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manual reconnect attempt {Attempt} failed.", localAttempts);
            }
        }

        _logger.LogError("All {Max} manual reconnect attempts exhausted.", MaxReconnectAttempts);
    }

    public async ValueTask DisposeAsync()
    {
        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    // ---------------------------------------------------------------
    // Custom reconnect policy: retry exactly MaxReconnectAttempts times,
    // each after ReconnectInterval.
    // ---------------------------------------------------------------
    private sealed class ReconnectPolicy : IRetryPolicy
    {
        private readonly int _maxAttempts;
        private readonly TimeSpan _interval;

        public ReconnectPolicy(int maxAttempts, TimeSpan interval)
        {
            _maxAttempts = maxAttempts;
            _interval = interval;
        }

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            if (retryContext.PreviousRetryCount >= _maxAttempts)
                return null; // Stop automatic reconnects; Closed event will trigger manual loop.
            return _interval;
        }
    }
}
