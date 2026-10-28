using Duende.AccessTokenManagement.OpenIdConnect;
using PandaBear.AspNetCore;
using PandaBear.Blazor;
using PandaBear.HttpClients;
using ViajantesTurismo.Branding.Contracts.Http;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Services;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisOutputCache(ResourceNames.Cache);
builder.Services.AddHttpClientDefaults();
builder.AddConfiguredTrustedForwardedHeaders();
builder.Services.AddManagementAuthentication(builder.Configuration, builder.Environment);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAntiforgery();
builder.Services.AddMetrics();
builder.Services.AddUiFailureReporting();
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
builder.Services.AddHttpClient<IBookingPaymentsApiClient, BookingPaymentsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddUserAccessTokenHandler()
    .AddKeycloakAudienceTokenExchangeHandler(ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IPaymentInstructionsApiClient, PaymentInstructionsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddUserAccessTokenHandler()
    .AddKeycloakAudienceTokenExchangeHandler(ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IPaymentAccountsApiClient, PaymentAccountsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
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
builder.Services.AddHttpClient<IBrandingManagementClient, BrandingManagementClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"))
    .AddUserAccessTokenHandler()
    .AddKeycloakAudienceTokenExchangeHandler(ApiAudienceNames.Branding);

var app = builder.Build();

app.UseForwardedHeaders();

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
