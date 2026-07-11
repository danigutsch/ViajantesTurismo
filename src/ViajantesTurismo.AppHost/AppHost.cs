using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

var builder = DistributedApplication.CreateBuilder(args);

var databaseServer = builder.AddDatabaseServer();
var adminDatabase = databaseServer.AddDatabase(ResourceNames.AdminDatabase);
var catalogDatabase = databaseServer.AddDatabase(ResourceNames.CatalogDatabase);

var cache = builder.AddCache();

var migrationService = builder.AddMigrationService(adminDatabase, catalogDatabase);

var brandingApiService = builder.AddBrandingApi(catalogDatabase, migrationService);

var apiService = builder.AddAdminApi(adminDatabase, brandingApiService, migrationService);

var catalogApiService = builder.AddCatalogApi(adminDatabase, catalogDatabase, migrationService);

builder.AddIntegrationEventWorker(adminDatabase, catalogDatabase, migrationService);

builder.AddManagementWeb(cache, apiService, catalogApiService, brandingApiService);

builder.AddPublicWeb(catalogApiService, brandingApiService);

builder.AddAdminPerformanceSmoke(apiService);

builder.AddObservabilityStack();

await builder.Build().RunAsync();
