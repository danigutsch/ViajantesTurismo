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
builder.AddAdminSeeding();
builder.AddBrandingInfrastructure();
builder.AddCatalogSeeding();

builder.Services.AddHostedService<SeederWorker>();

var host = builder.Build();
await host.RunAsync();
