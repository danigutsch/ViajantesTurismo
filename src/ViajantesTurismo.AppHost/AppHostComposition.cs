using ViajantesTurismo.Resources;

namespace ViajantesTurismo.AppHost;

/// <summary>
/// Composes the product resources for each supported hosted profile.
/// </summary>
internal static class AppHostComposition
{
    /// <summary>
    /// Adds the resources required by the selected product profile.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="profile">The product profile to compose.</param>
    public static void AddProductResources(this IDistributedApplicationBuilder builder, HostedProfile profile)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (profile is not (HostedProfile.Full or HostedProfile.System or HostedProfile.Admin))
        {
            throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unsupported hosted profile.");
        }

        var includeDeveloperTooling = profile.IncludesDeveloperTooling();
        var databaseServer = builder.AddDatabaseServer(includePgWeb: includeDeveloperTooling);
        var adminDatabase = databaseServer.AddDatabase(ResourceNames.AdminDatabase);
        var catalogDatabase = databaseServer.AddDatabase(ResourceNames.CatalogDatabase);
        var securityDatabase = databaseServer.AddDatabase(ResourceNames.SecurityDatabase);
        var managementWebClientSecret = builder.AddParameter(ResourceNames.ManagementWebClientSecret, secret: true);
        var identityProvider = builder.AddRunModeIdentityProvider(managementWebClientSecret);
        var migrationService = builder.AddMigrationService(adminDatabase, catalogDatabase, securityDatabase);
        var brandingApiService = builder.AddBrandingApi(catalogDatabase, migrationService, identityProvider);
        var apiService = builder.AddAdminApi(adminDatabase, brandingApiService, migrationService, identityProvider);

        if (profile is HostedProfile.Admin)
        {
            return;
        }

        var cache = builder.AddCache(includeRedisInsight: includeDeveloperTooling);
        var clamAv = profile.IncludesMediaInfrastructure() ? builder.AddClamAv(ResourceNames.ClamAv) : null;
        var seaweedFs = profile.IncludesMediaInfrastructure() ? builder.AddMediaObjectStorage() : null;

        if (builder.EnablesDatabaseObservability(profile))
        {
            builder.AddDatabaseObservability(adminDatabase, catalogDatabase, migrationService);
        }

        var catalogApiService = builder.AddCatalogApi(
            adminDatabase,
            catalogDatabase,
            migrationService,
            clamAv,
            seaweedFs,
            identityProvider);

        builder.AddIntegrationEventWorker(adminDatabase, catalogDatabase, migrationService, clamAv, seaweedFs);
        builder.AddManagementWeb(
            cache,
            securityDatabase,
            migrationService,
            identityProvider,
            managementWebClientSecret,
            apiService,
            catalogApiService,
            brandingApiService);
        builder.AddPublicWeb(catalogApiService, brandingApiService);
        if (includeDeveloperTooling)
        {
            builder.AddAdminPerformanceSmoke(apiService);
            builder.AddObservabilityStack();
        }
    }
}
