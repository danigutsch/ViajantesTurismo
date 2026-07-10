using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.MigrationService;
using ViajantesTurismo.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracingBuilder => { tracingBuilder.AddSource(SeederWorker.ActivitySourceName); });

builder.AddServiceDefaults();

builder.Services.AddDomainEventProcessing();
builder.AddSeeding();
builder.AddBrandingInfrastructure();
builder.AddCatalogInfrastructure();

builder.Services.AddHostedService<SeederWorker>();

var host = builder.Build();
await host.RunAsync();
