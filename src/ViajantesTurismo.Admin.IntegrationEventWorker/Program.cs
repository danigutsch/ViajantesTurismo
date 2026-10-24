using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.IntegrationEventWorker;
using ViajantesTurismo.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddAdminIntegrationEventWorkerInfrastructure();
builder.Services.AddAdminWorkerIntegrationEventDelivery();

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
