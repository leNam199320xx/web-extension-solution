using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PluginRuntime.Marketplace;
using PluginRuntime.Marketplace.Auth;
using PluginRuntime.Marketplace.Search;
using PluginRuntime.Marketplace.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// MudBlazor
builder.Services.AddMudServices();

// HTTP Client (base for auth + API calls)
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:6100";
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl), Timeout = TimeSpan.FromSeconds(15) });

// Authentication — calls real Auth API (system.auth plugin)
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ApiAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthStateProvider>());

// Services
builder.Services.AddScoped<IExtensionService, ExtensionService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ISearchEngine, SearchEngine>();

await builder.Build().RunAsync();
