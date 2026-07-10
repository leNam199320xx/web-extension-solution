using PluginRuntime.Marketplace.Models;

namespace PluginRuntime.Marketplace.Services;

public interface ISubscriptionService
{
    Task<SubscriptionResponseDto?> RequestSubscriptionAsync(SubscriptionRequestDto request, CancellationToken ct = default);
    Task<List<SubscriptionDto>> GetOutgoingRequestsAsync(CancellationToken ct = default);
    Task<List<SubscriptionDto>> GetIncomingRequestsAsync(CancellationToken ct = default);
    Task<SubscriptionResponseDto?> DecideSubscriptionAsync(SubscriptionDecisionDto decision, CancellationToken ct = default);
}
