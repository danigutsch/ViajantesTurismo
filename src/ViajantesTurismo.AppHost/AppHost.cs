using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

var builder = DistributedApplication.CreateBuilder(args);

var databaseServer = builder.AddDatabaseServer();
var adminDatabase = databaseServer.AddDatabase(ResourceNames.AdminDatabase);
var catalogDatabase = databaseServer.AddDatabase(ResourceNames.CatalogDatabase);
var securityDatabase = databaseServer.AddDatabase(ResourceNames.SecurityDatabase);

var cache = builder.AddCache();
var clamAv = builder.AddClamAv(ResourceNames.ClamAv);
var seaweedFs = builder.AddMediaObjectStorage();
var managementWebClientSecret = builder.AddParameter(ResourceNames.ManagementWebClientSecret, secret: true);
var identityProvider = builder.AddRunModeIdentityProvider(managementWebClientSecret);

var migrationService = builder.AddMigrationService(adminDatabase, catalogDatabase, securityDatabase);

var brandingApiService = builder.AddBrandingApi(catalogDatabase, migrationService, identityProvider);

var apiService = builder.AddAdminApi(adminDatabase, brandingApiService, migrationService, identityProvider);

var catalogApiService = builder.AddCatalogApi(adminDatabase, catalogDatabase, migrationService, clamAv, seaweedFs, identityProvider);

builder.AddIntegrationEventWorker(adminDatabase, catalogDatabase, migrationService, clamAv, seaweedFs);

builder.AddManagementWeb(cache, securityDatabase, migrationService, identityProvider, managementWebClientSecret, apiService, catalogApiService, brandingApiService);

builder.AddPublicWeb(catalogApiService, brandingApiService);

builder.AddAdminPerformanceSmoke(apiService);

builder.AddObservabilityStack();

await builder.Build().RunAsync();
