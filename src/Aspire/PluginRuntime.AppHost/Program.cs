var builder = DistributedApplication.CreateBuilder(args);

// ── Backend Services ──
var api = builder.AddProject<Projects.PluginRuntime_Api>("api")
    .WithEnvironment("DatabaseProvider", "Json")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var gateway = builder.AddProject<Projects.PublicApiGateway>("gateway")
    .WithEnvironment("Upstream__BaseUrl", "http://localhost:6100")
    .WithEnvironment("ConnectionStrings__Redis", "localhost:6379,abortConnect=false")
    .WithEnvironment("ConnectionStrings__PostgreSQL", "")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(api)
    .WaitFor(api);

// ── Frontend Portals ──
// Disable Aspire reverse proxy for frontend apps — Blazor handles client-side routing
var marketplace = builder.AddProject<Projects.PluginRuntime_Marketplace_Server>("marketplace")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
    .WithEndpoint("https", endpoint => endpoint.IsProxied = false);

var consumer = builder.AddProject<Projects.PluginRuntime_ConsumerPortal_Server>("consumer-portal")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
    .WithEndpoint("https", endpoint => endpoint.IsProxied = false);

var admin = builder.AddProject<Projects.PluginRuntime_Admin>("admin-portal")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
    .WithEndpoint("https", endpoint => endpoint.IsProxied = false);

await builder.Build().RunAsync();
