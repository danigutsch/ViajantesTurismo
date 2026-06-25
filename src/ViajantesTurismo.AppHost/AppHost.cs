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

if (IsEnabled(Environment.GetEnvironmentVariable("VT_ASPIRE_ENABLE_PERFORMANCE_TESTS")))
{
    var repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".."));
    var command = OperatingSystem.IsWindows() ? "pwsh" : "bash";
    var commandArguments = OperatingSystem.IsWindows()
        ? new[] { "-NoProfile", "-File", "scripts/run-admin-performance-smoke.ps1" }
        : new[] { "scripts/run-admin-performance-smoke.sh" };

    var performanceSmoke = builder.AddExecutable(ResourceNames.AdminPerformanceSmoke, command, repositoryRoot, commandArguments)
        .WithEnvironment("VT_API_BASE_URL", apiService.GetEndpoint("http"))
        .WithEnvironment("VT_K6_PROFILE", GetEnvironmentOrDefault("VT_K6_PROFILE", "smoke"))
        .WithEnvironment("VT_K6_RESULTS_DIR", GetEnvironmentOrDefault("VT_K6_RESULTS_DIR", "tests/performance/results"))
        .WaitFor(apiService);

    AddOptionalEnvironmentVariable("VT_K6_VUS");
    AddOptionalEnvironmentVariable("VT_K6_DURATION");
    AddOptionalEnvironmentVariable("VT_K6_USE_DOCKER");
    AddOptionalEnvironmentVariable("VT_K6_DOCKER_IMAGE");

    void AddOptionalEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (!string.IsNullOrWhiteSpace(value))
        {
            performanceSmoke.WithEnvironment(name, value);
        }
    }
}

await builder.Build().RunAsync();

static bool IsEnabled(string? value)
{
    return string.Equals(value, "1", StringComparison.Ordinal)
        || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}

static string GetEnvironmentOrDefault(string name, string defaultValue)
{
    var value = Environment.GetEnvironmentVariable(name);

    return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
}
