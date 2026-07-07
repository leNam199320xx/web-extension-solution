namespace PluginRuntime.Core.ValueObjects;

public record PluginEvent(string EventType, string PluginId, string? Version, DateTime Timestamp);
