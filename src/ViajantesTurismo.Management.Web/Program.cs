using SharedKernel.AspNetCore;
using SharedKernel.AspNetCore.Authentication.Bff;
using SharedKernel.Blazor;
using ViajantesTurismo.Branding.Contracts.Http;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Components.Pages.Catalog;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;
using ViajantesTurismo.Management.Web.Components.Pages.Operations;
using ViajantesTurismo.Management.Web.Components.Pages.Payments;
using ViajantesTurismo.Resources;
using SharedKernel.ServiceDefaults;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationDefaults(ViajantesTurismoServiceDefaults.Configure).AddViajantesTurismoTelemetry();
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

builder.Services.AddCountryService();
OperationsDashboardThresholds.AddOptions(builder.Services);

builder.Services.AddHttpClient<IToursApiClient, ToursApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddCustomerWorkflows();
builder.Services.AddHttpClient<IBookingsApiClient, BookingsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddPaymentWorkflows();
builder.Services.AddHttpClient<IDocumentsApiClient, DocumentsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IDocumentMaintenanceApiClient, DocumentMaintenanceApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IDocumentMaintenanceTimelineApiClient, DocumentMaintenanceApiClient>(
    client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IDocumentMaintenanceExecutionsApiClient, DocumentMaintenanceApiClient>(
    client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IDocumentMaintenanceControlApiClient, DocumentMaintenanceControlApiClient>(
    client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddHttpClient<IMessagingHealthApiClient, MessagingHealthApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
builder.Services.AddCatalogWorkflows();
builder.Services.AddHttpClient<IBrandingManagementClient, BrandingManagementClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"))
    .AddTokenExchangeHandler(ApiAudienceNames.Branding, scope: ApiAudienceNames.Branding);

var maintenanceEnabled = builder.AddHttpMaintenanceReadiness();
var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseManagementWebSecurityHeaders();
app.UseHttpMaintenance(maintenanceEnabled, browser: true);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseOutputCache();

app.MapManagementWebEndpoints();

app.MapHealthEndpoints();

await app.RunAsync();
