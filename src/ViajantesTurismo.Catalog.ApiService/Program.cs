using System.Reflection;
using SharedKernel.AspNetCore;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

const string ApiRobotsTxt = "User-agent: *\nDisallow: /";

var builder = WebApplication.CreateSlimBuilder(args);
var isOpenApiDocumentGeneration = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.Services.AddConfiguredTrustedForwardedHeaders(builder.Configuration.GetSection("Security:ForwardedHeaders"));
builder.Services.AddCatalogOpenApiDocuments();
builder.Services.AddOutputCache();
builder.Services.AddCatalogSecurityBaseline(builder.Configuration);
if (!isOpenApiDocumentGeneration)
{
    builder.Services.AddApiBearerAuthentication(
            builder.Configuration,
            builder.Environment,
            ApiAudienceNames.Catalog,
            CatalogAuthorization.PermissionsByRole)
        .AddPolicy(CatalogAuthorization.CatalogRead, policy => policy.RequirePermission(CatalogAuthorization.CatalogRead))
        .AddPolicy(CatalogAuthorization.CatalogWrite, policy => policy.RequirePermission(CatalogAuthorization.CatalogWrite))
        .AddPolicy(CatalogAuthorization.MediaAi, policy => policy.RequirePermission(CatalogAuthorization.MediaAi));
}
builder.AddCatalogInfrastructure();

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UsePublicContentLanguageQueryAlias();
app.UseCors(CatalogSecurityBaseline.CorsPolicyName);

if (!isOpenApiDocumentGeneration)
{
    app.UseAuthentication();
    app.UseAuthorization();
}
app.UseRateLimiter();
app.UseOutputCache();
app.MapCatalogEndpoints();
app.MapRobotsTxt(ApiRobotsTxt);

app.MapDefaultEndpoints();

await app.RunAsync();
