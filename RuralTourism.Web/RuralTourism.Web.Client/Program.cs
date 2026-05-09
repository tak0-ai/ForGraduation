using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RuralTourism.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var apiBaseUrl = builder.Configuration["BackendApi:BaseUrl"]?.TrimEnd('/');
var baseAddress = string.IsNullOrWhiteSpace(apiBaseUrl) ? builder.HostEnvironment.BaseAddress : apiBaseUrl;

builder.Services.AddTransient<BanCheckHandler>();
builder.Services.AddScoped(sp => 
{
    var baseUri = new Uri(baseAddress);
    var handler = sp.GetRequiredService<BanCheckHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = baseUri };
});

builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ResourceService>();
builder.Services.AddScoped<AdminReportService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<TourPlanService>();

await builder.Build().RunAsync();
