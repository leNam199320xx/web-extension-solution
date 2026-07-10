using System.Text.Json;
using PluginRuntime.Sdk;

namespace HelloWorld.Extension;

/// <summary>
/// Hello World extension — simplest possible plugin.
/// No capabilities required. Receives a name and returns a greeting.
/// </summary>
public class Plugin : IPlugin
{
    public Task<PluginResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken)
    {
        var name = "World";

        if (context.Input.ValueKind == JsonValueKind.Object
            && context.Input.TryGetProperty("name", out var nameProp)
            && nameProp.ValueKind == JsonValueKind.String)
        {
            name = nameProp.GetString() ?? "World";
        }

        var output = new
        {
            message = $"Hello, {name}! This is your first extension running on Plugin Runtime.",
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            extension = "com.samples.hello-world",
            version = "1.0.0"
        };

        var jsonData = JsonSerializer.SerializeToElement(output);

        return Task.FromResult(new PluginResult
        {
            Success = true,
            Data = jsonData
        });
    }
}
