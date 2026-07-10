using System.Diagnostics;

// Simple multi-project launcher — runs API + Gateway together without Aspire workload.
// No Docker, PostgreSQL, or Redis required. Uses JSON file storage.

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine("  Plugin Runtime Platform — Local Launcher");
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine();
Console.WriteLine("  Starting services:");
Console.WriteLine("    • API Backend     (JSON mode)  → http://localhost:5100");
Console.WriteLine("    • API Gateway                  → http://localhost:5200");
Console.WriteLine();
Console.WriteLine("  Press Ctrl+C to stop all services.");
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine();

var dotnet = "g:/net10/dotnet.exe";
var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));

var apiPath = Path.Combine(basePath, "PluginRuntime.Api");
var gatewayPath = Path.Combine(basePath, "PublicApiGateway");

// Start API
var apiProcess = StartProject(dotnet, apiPath, new Dictionary<string, string>
{
    ["ASPNETCORE_URLS"] = "http://localhost:5100",
    ["DatabaseProvider"] = "Json",
    ["JsonDataDirectory"] = Path.Combine(apiPath, "data"),
    ["ConnectionStrings__Redis"] = "localhost:6379,abortConnect=false",
    ["ASPNETCORE_ENVIRONMENT"] = "Development"
});

// Give API a moment to start
await Task.Delay(2000);

// Start Gateway
var gatewayProcess = StartProject(dotnet, gatewayPath, new Dictionary<string, string>
{
    ["ASPNETCORE_URLS"] = "http://localhost:5200",
    ["Upstream__BaseUrl"] = "http://localhost:5100",
    ["ConnectionStrings__Redis"] = "localhost:6379,abortConnect=false",
    ["ConnectionStrings__PostgreSQL"] = "",
    ["ASPNETCORE_ENVIRONMENT"] = "Development"
});

Console.WriteLine();
Console.WriteLine("  ✓ All services started.");
Console.WriteLine();
Console.WriteLine("  API:     http://localhost:5100");
Console.WriteLine("  Swagger: http://localhost:5100/swagger");
Console.WriteLine("  Gateway: http://localhost:5200");
Console.WriteLine("  Health:  http://localhost:5100/health");
Console.WriteLine();

// Wait for Ctrl+C
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException) { }

Console.WriteLine("\n  Shutting down...");
Kill(apiProcess);
Kill(gatewayProcess);
Console.WriteLine("  Done.");

static Process StartProject(string dotnetPath, string projectPath, Dictionary<string, string> env)
{
    var psi = new ProcessStartInfo
    {
        FileName = dotnetPath,
        Arguments = "run --no-build",
        WorkingDirectory = projectPath,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    foreach (var (key, value) in env)
        psi.EnvironmentVariables[key] = value;

    var process = Process.Start(psi)!;

    var name = Path.GetFileName(projectPath);
    _ = Task.Run(() => PipeOutput(process.StandardOutput, name));
    _ = Task.Run(() => PipeOutput(process.StandardError, name));

    Console.WriteLine($"  → Started {name} (PID {process.Id})");
    return process;
}

static async Task PipeOutput(System.IO.StreamReader reader, string prefix)
{
    while (await reader.ReadLineAsync() is { } line)
    {
        Console.WriteLine($"  [{prefix}] {line}");
    }
}

static void Kill(Process? p)
{
    try { p?.Kill(entireProcessTree: true); } catch { }
}
