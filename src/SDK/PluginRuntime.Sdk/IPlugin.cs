using System.Threading;
using System.Threading.Tasks;

namespace PluginRuntime.Sdk;

public interface IPlugin
{
    Task<PluginResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken);
}
