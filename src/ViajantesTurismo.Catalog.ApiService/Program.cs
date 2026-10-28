using PandaBear.AspNetCore;
using PandaBear.HttpCaching.AspNetCore;
using PandaBear.OpenApi;
using ViajantesTurismo.Branding.AspNetCore;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.ServiceDefaults;

const string ApiRobotsTxt = "User-agent: *\nDisallow: /";

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.AddConfiguredTrustedForwardedHeaders();
builder.Services.AddUnhandledExceptionHandler();
builder.Services.AddSecurityEventTelemetry();
builder.Services.AddCatalogOpenApiDocuments();
builder.Services.AddOutputCache();
builder.Services.AddCatalogSecurityBaseline(builder.Configuration);
builder.Services.AddCatalogAndBrandingApiSecurity(builder.Configuration, builder.Environment);
builder.AddCatalogInfrastructure();
builder.AddBrandingModule(addOutboxRelay: !OpenApiGenerationMode.IsEnabled(builder.Environment));

var app = builder.Build();

app.UseExceptionHandler();

app.UseForwardedHeaders();

app.MapConfiguredOpenApi();

app.UsePublicContentLanguageQueryAlias();
app.UseCors(CatalogSecurityBaseline.CorsPolicyName);
app.Use(static (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1/catalog", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.OnStarting(static state =>
        {
            HttpCacheHeaders.SetNoStore((HttpContext)state);
            return Task.CompletedTask;
        }, context);
    }

    return next(context);
});

app.UseApiSecurity();
app.UseRateLimiter();
app.UseOutputCache();
app.MapCatalogEndpoints();
app.MapBrandingModule();
app.MapRobotsTxt(ApiRobotsTxt);

app.MapDefaultEndpoints();

await app.RunAsync();
