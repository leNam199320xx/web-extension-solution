using MudBlazor.Services;
using PluginRuntime.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// SignalR — required for real-time updates in Monitoring and Dashboard
builder.Services.AddSignalR();

builder.Services.AddHttpClient<PluginRuntimeApiClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:6100";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient("AuthClient");
builder.Services.AddScoped<AuthTokenProvider>();

// SignalR hub connection factory — scoped so each browser tab gets its own connection
builder.Services.AddScoped<RuntimeHubConnection>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<PluginRuntime.Admin.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }
