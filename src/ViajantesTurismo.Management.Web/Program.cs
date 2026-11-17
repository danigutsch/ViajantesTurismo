using PandaBear.AspNetCore;
using PandaBear.AspNetCore.Authentication.Bff;
using PandaBear.Blazor;
using ViajantesTurismo.Branding.Contracts.Http;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Resources;
using PandaBear.ServiceDefaults;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedKernelServiceDefaults(ViajantesTurismoServiceDefaults.Configure).AddViajantesTurismoTelemetry();
builder.AddRedisOutputCache(ResourceNames.Cache);
builder.Services.AddOutputCache();
builder.AddConfiguredTrustedForwardedHeaders();
builder.Services.AddManagementAuthentication(builder.Configuration, builder.Environment);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAntiforgery();
builder.Services.AddCspNonce();
builder.Services.AddUiFailureReporting();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<CustomerCreationState>();
builder.Services.AddCountryService();

builder.Services.AddHttpClient<IToursApiClient, ToursApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddHttpClient<ICustomersApiClient, CustomersApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IBookingsApiClient, BookingsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IBookingPaymentsApiClient, BookingPaymentsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IPaymentInstructionsApiClient, PaymentInstructionsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IPaymentAccountsApiClient, PaymentAccountsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IDocumentsApiClient, DocumentsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddHttpClient<ICatalogToursApiClient, CatalogToursApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Catalog, scope: ApiAudienceNames.Catalog);
builder.Services.AddHttpClient<IPublicContentApiClient, PublicContentApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Catalog, scope: ApiAudienceNames.Catalog);
builder.Services.AddHttpClient<IBrandingManagementClient, BrandingManagementClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Branding, scope: ApiAudienceNames.Branding);

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseManagementWebSecurityHeaders();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseOutputCache();

app.MapManagementWebEndpoints();

app.MapSharedKernelDefaultEndpoints();

await app.RunAsync();
