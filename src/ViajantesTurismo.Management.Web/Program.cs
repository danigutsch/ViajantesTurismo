using SharedKernel.Branding;
using SharedKernel.HttpClients;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Services;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisOutputCache(ResourceNames.Cache);
builder.Services.AddHttpClientDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<CustomerCreationState>();
builder.Services.AddScoped<ICountryService, CountryService>();

builder.Services.AddHttpClient<IToursApiClient, ToursApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"));
builder.Services.AddHttpClient<ICustomersApiClient, CustomersApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"));
builder.Services.AddHttpClient<IBookingsApiClient, BookingsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"));
builder.Services.AddHttpClient<ICatalogToursApiClient, CatalogToursApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"));
builder.Services.AddHttpClient<IPublicContentApiClient, PublicContentApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"));
builder.Services.AddHttpClient<IBrandingApiClient, BrandingApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.BrandingApi}"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseManagementWebSecurityHeaders();

app.UseOutputCache();

app.MapManagementWebEndpoints();

app.MapDefaultEndpoints();

await app.RunAsync();
