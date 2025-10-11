using ViajantesTurismo.MigrationService;

var builder = Host.CreateApplicationBuilder(args);


builder.Services.AddHostedService<SeederWorker>();

var host = builder.Build();
host.Run();