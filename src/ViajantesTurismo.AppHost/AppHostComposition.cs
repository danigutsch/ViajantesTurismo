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
    /// <remarks>
    /// Catalog hosts Branding and relays its outbox; the Admin worker keeps the Admin Branding projection that
    /// document generation reads, so even the Admin-only profile needs both. Infrastructure configured in
    /// <see cref="ExternalInfrastructure"/> replaces the container that would otherwise be started for it.
    /// </remarks>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="profile">The product profile to compose.</param>
    public static void AddProductResources(this IDistributedApplicationBuilder builder, HostedProfile profile)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (profile is not (HostedProfile.Full or HostedProfile.System or HostedProfile.Admin))
        {
            throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unsupported hosted profile.");
        }

        var configuration = builder.Configuration;
        ExternalInfrastructure.RequireAllWhenShared(configuration);
        var isRunMode = builder.ExecutionContext.IsRunMode;
        var includeDeveloperTooling = isRunMode && profile.IncludesDeveloperTooling();
        builder.AddObservabilityStack(includeDeveloperTooling);
        var databaseServer = ExternalInfrastructure.HasDatabases(configuration) ? null : builder.AddDatabaseServer(includePgWeb: includeDeveloperTooling);
        var adminDatabase = builder.AddApplicationDatabase(databaseServer, ResourceNames.AdminDatabase);
        var catalogDatabase = builder.AddApplicationDatabase(databaseServer, ResourceNames.CatalogDatabase);
        var securityDatabase = builder.AddApplicationDatabase(databaseServer, ResourceNames.SecurityDatabase);
        var managementWebClientSecret = isRunMode ? builder.AddGeneratedSecret(ResourceNames.ManagementWebClientSecret) : null;
        var identityProvider = managementWebClientSecret is not null && ExternalInfrastructure.IdentityProviderAuthority(configuration) is null
            ? builder.AddRunModeIdentityProvider(managementWebClientSecret)
            : null;
        var clamAv = ExternalInfrastructure.ClamAv(configuration) is null ? builder.AddMalwareScanner() : null;
        var migrationService = builder.AddMigrationService(adminDatabase, catalogDatabase, securityDatabase);
        var seaweedFs = profile.IncludesMediaInfrastructure() && ExternalInfrastructure.SeaweedFs(configuration) is null
            ? builder.AddMediaObjectStorage()
            : null;

        var catalogApi = builder.AddCatalogApi(adminDatabase, catalogDatabase, migrationService, clamAv, seaweedFs, identityProvider);
        builder.AddAdminIntegrationEventWorker(adminDatabase, catalogDatabase, migrationService);
        var adminApi = builder.AddAdminApi(adminDatabase, migrationService, clamAv, identityProvider);

        if (profile is HostedProfile.Admin)
        {
            return;
        }

        IResourceBuilder<IResourceWithConnectionString> cache = ExternalInfrastructure.HasCache(configuration)
            ? builder.AddConnectionString(ResourceNames.Cache)
            : builder.AddCache(includeRedisInsight: includeDeveloperTooling);

        if (includeDeveloperTooling && databaseServer is not null && builder.EnablesDatabaseObservability())
        {
            builder.AddDatabaseObservability(adminDatabase, catalogDatabase, migrationService);
        }

        builder.AddCatalogIntegrationEventWorker(adminDatabase, catalogDatabase, migrationService, clamAv, seaweedFs);
        builder.AddManagementWeb(cache, securityDatabase, migrationService, identityProvider, managementWebClientSecret, adminApi, catalogApi);
        builder.AddPublicWeb(catalogApi);
        if (includeDeveloperTooling && identityProvider is not null)
        {
            builder.AddAdminPerformanceSmoke(adminApi, identityProvider);
        }
    }

    private static IResourceBuilder<IResourceWithConnectionString> AddApplicationDatabase(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresServerResource>? server,
        string name)
    {
        return server is null ? builder.AddConnectionString(name) : server.AddDatabase(name);
    }
}
