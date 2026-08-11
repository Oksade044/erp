using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ERP.Web;
using ERP.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API eyni origin-dədir (Caddy /api/v1-i backend-ə proxy edir) → BaseAddress = host.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<WebApiClient>();

var host = builder.Build();
await host.Services.GetRequiredService<AppState>().InitAsync();
await host.RunAsync();
