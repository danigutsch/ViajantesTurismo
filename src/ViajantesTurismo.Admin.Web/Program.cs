using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Admin.Web.Components;
using ViajantesTurismo.Admin.Web.Services;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<CustomerCreationState>();
builder.Services.AddScoped<CountryService>();

builder.Services.AddHttpClient<IToursApiClient, ToursApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"));
builder.Services.AddHttpClient<ICustomersApiClient, CustomersApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"));
builder.Services.AddHttpClient<IBookingsApiClient, BookingsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"));

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

await app.RunAsync();
