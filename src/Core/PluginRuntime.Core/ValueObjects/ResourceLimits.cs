namespace PluginRuntime.Core.ValueObjects;

public record ResourceLimits(int TimeoutMs, int MaxMemoryMb, int MaxCpuMs);
