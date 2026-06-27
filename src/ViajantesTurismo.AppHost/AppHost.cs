using Projects;
using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

var builder = DistributedApplication.CreateBuilder(args);

var databaseServer = builder.AddPostgres(ResourceNames.DatabaseServer)
    .WithPgWeb();
var database = databaseServer.AddDatabase(ResourceNames.Database);

var cache = builder.AddRedis(ResourceNames.Cache)
    .WithRedisInsight();

var migrationService = builder.AddDevelopmentDotNetProject<ViajantesTurismo_MigrationService>(ResourceNames.MigrationService)
    .WithReference(database)
    .WaitFor(database);

var apiService = builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Admin_ApiService>(ResourceNames.Api)
    .WithHttpHealthCheck(EndpointPaths.Health)
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(migrationService);

var catalogApiService = builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Catalog_ApiService>(ResourceNames.CatalogApi)
    .WithHttpHealthCheck(EndpointPaths.Health)
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(migrationService);

builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Management_Web>(ResourceNames.WebApp)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck(EndpointPaths.Health)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(catalogApiService)
    .WaitFor(catalogApiService);

builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Public_Web>(ResourceNames.PublicWebApp)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck(EndpointPaths.Health)
    .WithReference(catalogApiService)
    .WaitFor(catalogApiService);

builder.AddAdminPerformanceSmoke(apiService);

await builder.Build().RunAsync();
