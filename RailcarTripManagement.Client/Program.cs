using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RailcarTripManagement.Client;
using RailcarTripManagement.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API calls
// TODO: Move API base URL to appsettings.json or environment configuration
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress) 
});

// Register application services
// TODO: Consider using Refit or typed HttpClient for API communication
builder.Services.AddScoped<IRailcarTripService, RailcarTripService>();

await builder.Build().RunAsync();
