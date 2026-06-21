using Projects;
using ViajantesTurismo.Resources;

var builder = DistributedApplication.CreateBuilder(args);
var repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".."));
var performanceSmokeCommand = OperatingSystem.IsWindows() ? "pwsh" : "bash";
var performanceSmokeArguments = OperatingSystem.IsWindows()
    ? new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts/run-admin-performance-smoke.ps1" }
    : ["scripts/run-admin-performance-smoke.sh"];

var databaseServer = builder.AddPostgres(ResourceNames.DatabaseServer)
    .WithPgWeb();
var database = databaseServer.AddDatabase(ResourceNames.Database);

var cache = builder.AddRedis(ResourceNames.Cache)
    .WithRedisInsight();

var migrationService = builder.AddProject<ViajantesTurismo_MigrationService>(ResourceNames.MigrationService)
    .WithReference(database)
    .WaitFor(database);

var apiService = builder.AddProject<ViajantesTurismo_Admin_ApiService>(ResourceNames.Api)
    .WithHttpHealthCheck(EndpointPaths.Health)
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(migrationService);

builder.AddProject<ViajantesTurismo_Management_Web>(ResourceNames.WebApp)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck(EndpointPaths.Health)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<ViajantesTurismo_Public_Web>(ResourceNames.PublicWebApp)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck(EndpointPaths.Health);

builder.AddExecutable(ResourceNames.AdminPerformanceSmoke, performanceSmokeCommand, repositoryRoot, performanceSmokeArguments)
    .WithEnvironment("VT_API_BASE_URL", apiService.GetEndpoint("http"))
    .WaitFor(apiService)
    .WithExplicitStart();

await builder.Build().RunAsync();
