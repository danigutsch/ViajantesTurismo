using SharedKernel.AspNetCore;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.ServiceDefaults;

const string ApiRobotsTxt = "User-agent: *\nDisallow: /";

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.Services.AddConfiguredTrustedForwardedHeaders(builder.Configuration.GetSection("Security:ForwardedHeaders"));
builder.Services.AddCatalogOpenApiDocuments();
builder.Services.AddOutputCache();
builder.Services.AddCatalogSecurityBaseline(builder.Configuration);
builder.AddCatalogInfrastructure();

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UsePublicContentLanguageQueryAlias();
app.UseCors(CatalogSecurityBaseline.CorsPolicyName);

app.UseRateLimiter();
app.UseOutputCache();
app.MapCatalogEndpoints();
app.MapRobotsTxt(ApiRobotsTxt);

app.MapDefaultEndpoints();

await app.RunAsync();
