using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

var builder = DistributedApplication.CreateBuilder(args);

var databaseServer = builder.AddDatabaseServer();
var database = databaseServer.AddDatabase(ResourceNames.Database);

var cache = builder.AddCache();

var migrationService = builder.AddMigrationService(database);

var apiService = builder.AddAdminApi(database, migrationService);

var catalogApiService = builder.AddCatalogApi(database, migrationService);

builder.AddManagementWeb(cache, apiService, catalogApiService);

builder.AddPublicWeb(catalogApiService);

builder.AddAdminPerformanceSmoke(apiService);

builder.AddObservabilityStack();

await builder.Build().RunAsync();
