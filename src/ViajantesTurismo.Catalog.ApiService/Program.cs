using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.Services.AddCatalogOpenApiDocuments();
builder.Services.AddCatalogSecurityBaseline(builder.Configuration);
builder.Services.AddOutputCache();
builder.AddCatalogInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CatalogSecurityBaseline.CorsPolicyName);

app.UseRateLimiter();

app.UseOutputCache();

app.MapCatalogEndpoints();

app.MapDefaultEndpoints();

await app.RunAsync();
