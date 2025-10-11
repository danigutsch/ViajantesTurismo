using Projects;
using ViajantesTurismo.Resources;

var builder = DistributedApplication.CreateBuilder(args);

var databaseServer = builder.AddPostgres(ResourceNames.DatabaseServer)
    .WithPgWeb();
var database = databaseServer.AddDatabase(ResourceNames.Database);

var cache = builder.AddRedis(ResourceNames.Cache)
    .WithRedisInsight();

var migrationService = builder.AddProject<ViajantesTurismo_MigrationService>(ResourceNames.MigrationService)
    .WithReference(database)
    .WaitFor(database);

var apiService = builder.AddProject<ViajantesTurismo_ApiService>(ResourceNames.Api)
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(migrationService);

builder.AddProject<ViajantesTurismo_Web>(ResourceNames.WebApp)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
