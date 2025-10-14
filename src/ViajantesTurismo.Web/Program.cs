using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;
using ViajantesTurismo.Web;
using ViajantesTurismo.Web.Components;
using ViajantesTurismo.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<CustomerCreationState>();
builder.Services.AddScoped<CountryService>();

builder.Services.AddHttpClient<ToursApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"));
builder.Services.AddHttpClient<CustomersApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();