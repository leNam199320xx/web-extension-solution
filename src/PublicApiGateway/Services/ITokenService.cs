namespace PublicApiGateway.Services;

public interface ITokenService
{
    Task<string> GetServiceTokenAsync(CancellationToken ct);
}
