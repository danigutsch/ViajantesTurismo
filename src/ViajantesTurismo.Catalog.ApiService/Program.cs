using SharedKernel.AspNetCore;
using SharedKernel.HttpCaching.AspNetCore;
using SharedKernel.OpenApi;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

const string ApiRobotsTxt = "User-agent: *\nDisallow: /";

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.AddConfiguredTrustedForwardedHeaders();
builder.Services.AddCatalogOpenApiDocuments();
builder.Services.AddOutputCache();
builder.Services.AddCatalogSecurityBaseline(builder.Configuration);
builder.Services.AddApiSecurity(
        builder.Configuration,
        builder.Environment,
        ApiAudienceNames.Catalog,
        CatalogAuthorization.PermissionsByRole)
    .AddPolicy(CatalogAuthorization.CatalogRead, policy => policy.RequirePermission(CatalogAuthorization.CatalogRead))
    .AddPolicy(CatalogAuthorization.CatalogWrite, policy => policy.RequirePermission(CatalogAuthorization.CatalogWrite))
    .AddPolicy(CatalogAuthorization.CatalogPublish, policy => policy.RequirePermission(CatalogAuthorization.CatalogPublish))
    .AddPolicy(CatalogAuthorization.MediaAi, policy => policy.RequirePermission(CatalogAuthorization.MediaAi));
builder.AddCatalogInfrastructure();

var app = builder.Build();

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
app.MapRobotsTxt(ApiRobotsTxt);

app.MapDefaultEndpoints();

await app.RunAsync();
