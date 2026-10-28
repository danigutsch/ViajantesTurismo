using ViajantesTurismo.Resources;

namespace ViajantesTurismo.AppHost;

/// <summary>Composes application instances against infrastructure owned by the hosted-test assembly.</summary>
internal static class SharedProductResourceExtensions
{
    /// <summary>Registers the shared product resources and wires OTLP environment for every project resource.</summary>
    /// <remarks>Catalog hosts Branding and relays its outbox; the Admin worker fills the projection documents read.</remarks>
    public static void AddSharedProductResources(this IDistributedApplicationBuilder builder, HostedProfile profile)
    {
        var admin = builder.AddConnectionString(ResourceNames.AdminDatabase);
        var catalog = builder.AddConnectionString(ResourceNames.CatalogDatabase);
        var security = builder.AddConnectionString(ResourceNames.SecurityDatabase);
        var secret = builder.AddParameter(ResourceNames.ManagementWebClientSecret, secret: true);
        var migrations = builder.AddMigrationService(admin, catalog, security);
        var catalogApi = builder.AddCatalogApi(admin, catalog, migrations, clamAv: null, seaweedFs: null, identityProvider: null);
        builder.AddAdminIntegrationEventWorker(admin, catalog, migrations);
        var api = builder.AddAdminApi(admin, migrations, clamAv: null, identityProvider: null);
        if (profile is not HostedProfile.Admin)
        {
            var cache = builder.AddConnectionString(ResourceNames.Cache);
            builder.AddCatalogIntegrationEventWorker(admin, catalog, migrations, clamAv: null, seaweedFs: null);
            builder.AddManagementWeb(cache, security, migrations, identityProvider: null, secret, api, catalogApi);
            builder.AddPublicWeb(catalogApi);
        }

        foreach (var project in builder.Resources.OfType<ProjectResource>())
        {
            var resource = builder.CreateResourceBuilder(project);
            resource.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", Required(builder, "OtlpEndpoint"))
                .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");
            if (project.Name is ResourceNames.Api or ResourceNames.CatalogApi or ResourceNames.WebApp)
            {
                resource.WithEnvironment("Authentication__Authority", Required(builder, "Authority"))
                    .WithEnvironment("Authentication__Issuer", Required(builder, "Authority"))
                    .WithEnvironment("Authentication__AllowHttpDevelopmentAuthority", "true");
            }

            if (project.Name is ResourceNames.Api or ResourceNames.CatalogApi or ResourceNames.CatalogIntegrationEventWorker)
            {
                resource.WithEnvironment("MalwareScanning__ClamAv__Host", Required(builder, "ClamAv:Host"))
                    .WithEnvironment("MalwareScanning__ClamAv__Port", Required(builder, "ClamAv:Port"));
            }

            if (project.Name is ResourceNames.CatalogApi or ResourceNames.CatalogIntegrationEventWorker)
            {
                resource.WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__Endpoint", Required(builder, "SeaweedFs:Endpoint"))
                    .WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__AccessKey", Required(builder, "SeaweedFs:AccessKey"))
                    .WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__SecretKey", Required(builder, "SeaweedFs:SecretKey"))
                    .WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__Bucket", Required(builder, "SeaweedFs:Bucket"))
                    .WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__AutoProvisionBucket", "true");
            }
        }
    }

    private static string Required(IDistributedApplicationBuilder builder, string key)
    {
        return builder.Configuration[$"HostedTests:{key}"]
            ?? throw new InvalidOperationException($"The shared hosted-test setting '{key}' is missing.");
    }
}
