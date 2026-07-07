using PluginRuntime.Core.Entities;
using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Core.Interfaces;

public interface IManifestValidator
{
    Task<ValidationResult> ValidateAsync(Manifest manifest, CancellationToken cancellationToken);
}
