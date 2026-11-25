using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.IntegrationEventWorker;
using SharedKernel.ServiceDefaults;
using ViajantesTurismo.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddApplicationDefaults(ViajantesTurismoServiceDefaults.Configure).AddViajantesTurismoTelemetry();
builder.AddAdminIntegrationEventWorkerInfrastructure();
builder.Services.AddAdminWorkerIntegrationEventDelivery();

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
