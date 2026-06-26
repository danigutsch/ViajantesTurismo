using Projects;
using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

var builder = DistributedApplication.CreateBuilder(args);

var databaseServer = builder.AddPostgres(ResourceNames.DatabaseServer)
    .WithPgWeb();
var database = databaseServer.AddDatabase(ResourceNames.Database);

var cache = builder.AddRedis(ResourceNames.Cache)
    .WithRedisInsight();

var migrationService = builder.AddProject<ViajantesTurismo_MigrationService>(
        ResourceNames.MigrationService,
        launchProfileName: null)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithReference(database)
    .WaitFor(database);

var apiService = builder.AddProject<ViajantesTurismo_Admin_ApiService>(
        ResourceNames.Api,
        launchProfileName: null)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint()
    .WithHttpsEndpoint()
    .WithHttpHealthCheck(EndpointPaths.Health)
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(migrationService);

var catalogApiService = builder.AddProject<ViajantesTurismo_Catalog_ApiService>(
        ResourceNames.CatalogApi,
        launchProfileName: null)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint()
    .WithHttpsEndpoint()
    .WithHttpHealthCheck(EndpointPaths.Health);

builder.AddProject<ViajantesTurismo_Management_Web>(
        ResourceNames.WebApp,
        launchProfileName: null)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint()
    .WithHttpsEndpoint()
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck(EndpointPaths.Health)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(catalogApiService)
    .WaitFor(catalogApiService);

builder.AddProject<ViajantesTurismo_Public_Web>(
        ResourceNames.PublicWebApp,
        launchProfileName: null)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint()
    .WithHttpsEndpoint()
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck(EndpointPaths.Health)
    .WithReference(catalogApiService)
    .WaitFor(catalogApiService);

builder.AddAdminPerformanceSmoke(apiService);

await builder.Build().RunAsync();
