using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddCatalogIntegrationEventWorkerInfrastructure();

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
