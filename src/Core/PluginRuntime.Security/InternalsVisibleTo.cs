using System.Runtime.CompilerServices;

// Allow the integration test project to access internal members (e.g. GetCanonicalContent)
[assembly: InternalsVisibleTo("PluginRuntime.IntegrationTests")]
[assembly: InternalsVisibleTo("PluginRuntime.Security.Tests")]
