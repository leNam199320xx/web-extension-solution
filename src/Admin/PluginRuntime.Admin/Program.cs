using MudBlazor.Services;
using PluginRuntime.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddHttpClient<PluginRuntimeApiClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:5001";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<AuthTokenProvider>();

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
