using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.MigrationService;
using ViajantesTurismo.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddSeeding();

builder.Services.AddHostedService<SeederWorker>();

var host = builder.Build();
host.Run();
