namespace PublicApiGateway.Services;

public interface IIpBlockingService
{
    Task<bool> IsBlockedAsync(string ipAddress, CancellationToken ct);
    Task RecordFailedAttemptAsync(string ipAddress, CancellationToken ct);
}
