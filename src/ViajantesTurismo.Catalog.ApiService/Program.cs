using System.Reflection;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.Services.AddCatalogOpenApiDocuments();

if (!IsBuildTimeOpenApiGeneration())
{
    builder.AddCatalogInfrastructure();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapCatalogEndpoints();

app.MapDefaultEndpoints();

await app.RunAsync();

static bool IsBuildTimeOpenApiGeneration()
{
    return Assembly.GetEntryAssembly()?.GetName().Name is "dotnet-getdocument" or "GetDocument.Insider";
}
