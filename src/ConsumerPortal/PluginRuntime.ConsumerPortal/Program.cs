using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PluginRuntime.ConsumerPortal;
using PluginRuntime.ConsumerPortal.Auth;
using PluginRuntime.ConsumerPortal.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

// HTTP Client
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:6100";
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl), Timeout = TimeSpan.FromSeconds(15) });

// Authentication — calls real Auth API (system.auth plugin)
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ApiAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthStateProvider>());

// Services
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IUsageService, UsageService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ISupportService, SupportService>();

await builder.Build().RunAsync();
