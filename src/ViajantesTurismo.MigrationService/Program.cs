using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.MigrationService;
using ViajantesTurismo.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracingBuilder => { tracingBuilder.AddSource(SeederWorker.ActivitySourceName); });

builder.AddServiceDefaults();

builder.AddSeeding();

builder.Services.AddHostedService<SeederWorker>();

var host = builder.Build();
await host.RunAsync();
