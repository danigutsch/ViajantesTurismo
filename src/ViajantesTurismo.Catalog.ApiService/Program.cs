using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.AddCatalogInfrastructure();

var app = builder.Build();

app.MapCatalogEndpoints();

app.MapDefaultEndpoints();

await app.RunAsync();
