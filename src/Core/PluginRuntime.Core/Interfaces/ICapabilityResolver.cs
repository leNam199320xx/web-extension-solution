using PluginRuntime.Core.Entities;

namespace PluginRuntime.Core.Interfaces;

public interface ICapabilityResolver
{
    IReadOnlyDictionary<string, ICapability> Resolve(Manifest manifest, ValueObjects.ExecutionContext executionContext);
}
