using ViajantesTurismo.Catalog.Infrastructure;
using PandaBear.ServiceDefaults;
using ViajantesTurismo.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSharedKernelServiceDefaults(ViajantesTurismoServiceDefaults.Configure).AddViajantesTurismoTelemetry();
builder.AddCatalogIntegrationEventWorkerInfrastructure();

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
