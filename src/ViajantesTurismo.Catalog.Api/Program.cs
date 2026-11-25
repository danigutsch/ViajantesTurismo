using SharedKernel.AspNetCore;
using SharedKernel.AspNetCore.Authentication;
using SharedKernel.AspNetCore.RateLimiting;
using SharedKernel.AspNetCore.Seo;
using SharedKernel.HttpCaching.AspNetCore;
using SharedKernel.OpenApi;
using ViajantesTurismo.Branding.AspNetCore;
using ViajantesTurismo.Catalog.Api;
using ViajantesTurismo.Catalog.Api.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure;
using SharedKernel.ServiceDefaults;
using ViajantesTurismo.ServiceDefaults;

const string ApiRobotsTxt = "User-agent: *\nDisallow: /";

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.WebHost.ConfigureApiRequestLimits();
builder.AddApplicationDefaults(ViajantesTurismoServiceDefaults.Configure).AddViajantesTurismoTelemetry();
builder.AddConfiguredTrustedForwardedHeaders();
builder.Services.AddUnhandledExceptionHandler();
builder.Services.AddAuthenticationEventTelemetry();
builder.Services.AddRateLimitEventTelemetry();
builder.Services.AddCatalogOpenApiDocuments();
builder.Services.AddMultipartUploadTimeout();
builder.Services.AddOutputCache();
builder.Services.AddCatalogSecurityBaseline(builder.Configuration);
builder.Services.AddCatalogAndBrandingApiSecurity(builder.Configuration, builder.Environment);
builder.AddCatalogInfrastructure();
builder.AddBrandingModule(addOutboxRelay: !OpenApiGenerationMode.IsEnabled(builder.Environment));

var maintenanceEnabled = builder.AddHttpMaintenanceReadiness();
var app = builder.Build();

app.UseForwardedHeaders();

app.UseExceptionHandler();

app.MapConfiguredOpenApi();

app.UsePublicContentLanguageQueryAlias();
app.UseCors(CatalogSecurityBaseline.CorsPolicyName);
app.UseNoStoreResponses();

app.UseApiSecurity();
app.UseHttpMaintenance(maintenanceEnabled, browser: false);
app.UseRateLimiter();
app.UseApiRequestTimeouts();
app.UseOutputCache();
app.MapCatalogEndpoints();
app.MapBrandingModule();
app.MapRobotsTxt(ApiRobotsTxt);

app.MapHealthEndpoints();

await app.RunAsync();
