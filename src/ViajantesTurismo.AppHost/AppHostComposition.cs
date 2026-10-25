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

        if (string.Equals(builder.Configuration["HostedTests:SharedInfrastructure"], bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            builder.AddSharedProductResources(profile);
            return;
        }

        builder.AddObservabilityStack();
        var includeDeveloperTooling = profile.IncludesDeveloperTooling();
        var databaseServer = builder.AddDatabaseServer(includePgWeb: includeDeveloperTooling);
        var adminDatabase = databaseServer.AddDatabase(ResourceNames.AdminDatabase);
        var catalogDatabase = databaseServer.AddDatabase(ResourceNames.CatalogDatabase);
        var securityDatabase = databaseServer.AddDatabase(ResourceNames.SecurityDatabase);
        var managementWebClientSecret = builder.AddParameter(ResourceNames.ManagementWebClientSecret, secret: true);
        var identityProvider = builder.AddRunModeIdentityProvider(managementWebClientSecret);
        var clamAv = builder.AddClamAv(ResourceNames.ClamAv);
        var migrationService = builder.AddMigrationService(adminDatabase, catalogDatabase, securityDatabase);
        var seaweedFs = profile.IncludesMediaInfrastructure() ? builder.AddMediaObjectStorage() : null;

        // Catalog hosts Branding and relays its outbox; the Admin worker keeps the Admin Branding projection that
        // document generation reads, so even the Admin-only profile needs both.
        var catalogApiService = builder.AddCatalogApi(adminDatabase, catalogDatabase, migrationService, clamAv, seaweedFs, identityProvider);
        builder.AddAdminIntegrationEventWorker(adminDatabase, catalogDatabase, migrationService);
        var apiService = builder.AddAdminApi(adminDatabase, migrationService, clamAv, identityProvider);

        if (profile is HostedProfile.Admin)
        {
            return;
        }

        var cache = builder.AddCache(includeRedisInsight: includeDeveloperTooling);

        if (builder.EnablesDatabaseObservability(profile))
        {
            builder.AddDatabaseObservability(adminDatabase, catalogDatabase, migrationService);
        }

        builder.AddCatalogIntegrationEventWorker(adminDatabase, catalogDatabase, migrationService, clamAv, seaweedFs);
        builder.AddManagementWeb(cache, securityDatabase, migrationService, identityProvider, managementWebClientSecret, apiService, catalogApiService);
        builder.AddPublicWeb(catalogApiService);
        if (includeDeveloperTooling)
        {
            builder.AddAdminPerformanceSmoke(apiService);
        }
    }
}
