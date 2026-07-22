using Duende.AccessTokenManagement.OpenIdConnect;
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
builder.Services.AddManagementAuthentication(builder.Configuration, builder.Environment);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAntiforgery();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<CustomerCreationState>();
builder.Services.AddScoped<ICountryService, CountryService>();

builder.Services.AddHttpClient<IToursApiClient, ToursApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddUserAccessTokenHandler()
    .AddKeycloakAudienceTokenExchangeHandler(ApiAudienceNames.Admin);
builder.Services.AddHttpClient<ICustomersApiClient, CustomersApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddUserAccessTokenHandler()
    .AddKeycloakAudienceTokenExchangeHandler(ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IBookingsApiClient, BookingsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddUserAccessTokenHandler()
    .AddKeycloakAudienceTokenExchangeHandler(ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IDocumentsApiClient, DocumentsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddUserAccessTokenHandler()
    .AddKeycloakAudienceTokenExchangeHandler(ApiAudienceNames.Admin);
builder.Services.AddHttpClient<ICatalogToursApiClient, CatalogToursApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"))
    .AddUserAccessTokenHandler()
    .AddKeycloakAudienceTokenExchangeHandler(ApiAudienceNames.Catalog);
builder.Services.AddHttpClient<IPublicContentApiClient, PublicContentApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"))
    .AddUserAccessTokenHandler()
    .AddKeycloakAudienceTokenExchangeHandler(ApiAudienceNames.Catalog);
builder.Services.AddHttpClient<IManagementBrandingApiClient, ManagementBrandingApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.BrandingApi}"))
    .AddUserAccessTokenHandler()
    .AddKeycloakAudienceTokenExchangeHandler(ApiAudienceNames.Branding);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseManagementWebSecurityHeaders();

app.UseOutputCache();

app.MapManagementWebEndpoints();

app.MapDefaultEndpoints();

await app.RunAsync();
