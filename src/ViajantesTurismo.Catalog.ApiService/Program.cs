using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.Services.AddCatalogOpenApiDocuments();
builder.Services.AddOutputCache();
builder.Services.AddCatalogSecurityBaseline(builder.Configuration);
builder.AddCatalogInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UsePublicContentLanguageQueryAlias();
app.UseOutputCache();
app.UseCors(CatalogSecurityBaseline.CorsPolicyName);

app.UseRateLimiter();
app.MapCatalogEndpoints();

app.MapDefaultEndpoints();

await app.RunAsync();
