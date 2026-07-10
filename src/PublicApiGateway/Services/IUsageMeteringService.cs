using PublicApiGateway.Models;

namespace PublicApiGateway.Services;

public interface IUsageMeteringService
{
    void Enqueue(UsageRecord record);
}
