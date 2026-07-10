namespace PluginRuntime.Marketplace.Models;

public sealed record ExtensionSummaryDto(
    Guid ExtensionId, string Name, string Author, string Category,
    string LatestVersion, string RiskLevel, string ShortDescription, int SubscriberCount);

public sealed record ExtensionDetailDto(
    Guid ExtensionId, string Name, string Author, string Category,
    string LatestVersion, string RiskLevel, string Description, string? ReadmeContent,
    PermissionBreakdownDto Permissions, List<VersionHistoryDto> Versions,
    string Visibility, int SubscriberCount, DateTime CreatedAt);

public sealed record VersionHistoryDto(string Version, string Status, DateTime CreatedAt, string? RejectionReason);

public sealed record PermissionBreakdownDto(List<PermissionGroupDto> Groups);

public sealed record PermissionGroupDto(string RiskLevel, List<PermissionItemDto> Permissions);

public sealed record PermissionItemDto(string Scope, string MappedCapability, string RiskLevel, string Justification);

public sealed record SubscriptionDto(
    Guid SubscriptionId, Guid TargetExtensionId, string TargetExtensionName,
    string Status, DateTime RequestedAt, string? Reason, ExpectedUsageDto? ExpectedUsage);

public sealed record SubscriptionRequestDto(Guid TargetExtensionId, string Reason, ExpectedUsageDto ExpectedUsage);

public sealed record ExpectedUsageDto(int RequestsPerDay, string UsagePattern);

public sealed record SubscriptionDecisionDto(Guid SubscriptionId, bool Approved, string? Reason, DateTime? ExpiresAt);

public sealed record SubscriptionResponseDto(Guid SubscriptionId, string Status);

public sealed record UploadResultDto(Guid PluginVersionId, string Status, string? Message);

public sealed record ManifestPreviewDto(
    string ExtensionId, string Version, List<string> Permissions, List<string> Capabilities);

public sealed record PublisherProfileDto(
    Guid PublisherId, string DisplayName, string? Description,
    DateTime JoinedAt, List<ExtensionSummaryDto> Extensions);

public sealed record UserProfileDto(string DisplayName, string Email, string? PublisherDescription);

public sealed record UpdateProfileDto(string DisplayName, string? PublisherDescription);

public sealed record ApiKeyDto(Guid KeyId, string Name, string Prefix, DateTime CreatedAt, DateTime? LastUsedAt);

public sealed record ApiKeyListDto(List<ApiKeyDto> Keys);

public sealed record EcosystemStatsDto(int TotalExtensions, int TotalPublishers, int TotalSubscriptions);

public sealed record PaginatedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

public sealed record ExtensionQuery(string? SearchText, string? Category, string? RiskLevel, int Page = 1, int PageSize = 20);

public sealed record SearchCriteria(string? Text, string? Category, string? RiskLevel, string? CapabilityType);

public sealed record SearchResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

public sealed record ApiErrorResponse(ApiError Error);

public sealed record ApiError(string Code, string Category, string Message, string? TraceId);
