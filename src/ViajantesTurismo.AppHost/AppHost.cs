using Projects;
using ViajantesTurismo.Resources;

var builder = DistributedApplication.CreateBuilder(args);

var databaseServer = builder.AddPostgres(ResourceNames.DatabaseServer);
var database = databaseServer.AddDatabase(ResourceNames.Database);

var cache = builder.AddRedis(ResourceNames.Cache);

var apiService = builder.AddProject<ViajantesTurismo_ApiService>(ResourceNames.Api)
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<ViajantesTurismo_Web>(ResourceNames.WebApp)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();