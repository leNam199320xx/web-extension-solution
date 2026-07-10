namespace PluginRuntime.ConsumerPortal.Models.Requests;

public sealed record TenantRegistrationRequest(string Name, string ContactEmail, string? CompanyName);
public sealed record UpdateProfileRequest(string Name, string ContactEmail, string? CompanyName);
public sealed record NotificationPreferencesRequest(bool UsageAlerts, bool BillingNotifications, bool KeyExpirationReminders);
public sealed record PlanChangeRequest(Guid NewPlanId);
public sealed record GenerateKeyRequest(string? Name, int? ExpirationDays);
public sealed record SupportTicketRequest(string Subject, string Category, string Priority, string Message);
