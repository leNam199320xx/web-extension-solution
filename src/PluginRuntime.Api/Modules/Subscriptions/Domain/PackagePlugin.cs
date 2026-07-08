namespace PluginRuntime.Api.Modules.Subscriptions.Domain;

/// <summary>
/// Join entity representing a plugin included in a package.
/// </summary>
public sealed class PackagePlugin
{
    public Guid PackageId { get; private set; }
    public Guid PluginId { get; private set; }
    public DateTime AddedAt { get; private set; }

    private PackagePlugin() { }

    public static PackagePlugin Create(Guid packageId, Guid pluginId)
    {
        return new PackagePlugin
        {
            PackageId = packageId,
            PluginId = pluginId,
            AddedAt = DateTime.UtcNow
        };
    }
}
