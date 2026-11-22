using Microsoft.Extensions.Configuration;
using PandaBear.Aspire.Hosting;
using Projects;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.AppHost;

/// <summary>
/// Adds named resources to the local Aspire model.
/// </summary>
internal static class AppHostResourceExtensions
{
    /// <summary>Tag for <c>docker.io/sosedoff/pgweb:0.17.0</c>.</summary>
    private const string PgWebImageTag = "0.17.0";

    /// <summary>Digest for <c>docker.io/sosedoff/pgweb:0.17.0</c>.</summary>
    private const string PgWebImageDigest = "a5256d416e2e8b92d69a4459058e3eca33a9f075d8325491644411d0bc3bd70b";

    /// <summary>Tag for <c>docker.io/redis/redisinsight:3.6</c>.</summary>
    private const string RedisInsightImageTag = "3.6";

    /// <summary>Digest for <c>docker.io/redis/redisinsight:3.6</c>.</summary>
    private const string RedisInsightImageDigest = "aa21bbd198455b4ad964f76782db951155aa0d712321f599972d1525f031f0e6";

    /// <summary>Configuration key for Public Web canonical sitemap URLs.</summary>
    private const string PublicWebSitemapCanonicalOriginEnvironmentVariable = "PublicWeb__Sitemap__CanonicalOrigin";
    private const string PublicWebReplicasConfigurationKey = "Aspire:Replicas:PublicWeb";
    private const string KeycloakImageRegistry = "quay.io";
    private const string KeycloakImageName = "keycloak/keycloak";
    private const string KeycloakImageTag = "26.7.0";
    private const string OidcProviderImageDigest = "2eb3cd316835c990e69e26ade292ffa78f6fb0db7d5fc6377463c162e1979ac0";
    private const string AuthenticationAuthorityEnvironmentVariable = "Authentication__Authority";
    private const string AuthenticationIssuerEnvironmentVariable = "Authentication__Issuer";
    private const string AuthenticationAllowHttpDevelopmentAuthorityEnvironmentVariable = "Authentication__AllowHttpDevelopmentAuthority";
    private const string AuthenticationClientIdEnvironmentVariable = "Authentication__ClientId";
    private const string AuthenticationClientSecretEnvironmentVariable = "Authentication__ClientSecret";
    private const string AuthenticationTokenExchangeEnabledEnvironmentVariable = "Authentication__TokenExchange__Enabled";
    private const string AuthenticationTokenExchangeProviderEnvironmentVariable = "Authentication__TokenExchange__Provider";
    private const string KeycloakTokenExchangeProvider = "Keycloak";
    private const string KeycloakBootstrapAdminUsernameEnvironmentVariable = "KC_BOOTSTRAP_ADMIN_USERNAME";
    private const string KeycloakBootstrapAdminPasswordEnvironmentVariable = "KC_BOOTSTRAP_ADMIN_PASSWORD";
    private const string KeycloakManagementClientSecretEnvironmentVariable = "MANAGEMENT_WEB_CLIENT_SECRET";
    private const string KeycloakConformanceUserPasswordEnvironmentVariable = "LOCAL_CONFORMANCE_PASSWORD";
    private const string KeycloakOperatorConformanceUserPasswordEnvironmentVariable = "LOCAL_OPERATOR_CONFORMANCE_PASSWORD";
    private const string KeycloakK6ClientSecretEnvironmentVariable = "LOCAL_K6_CLIENT_SECRET";
    private const string KeycloakRealmImportDirectory = "/opt/keycloak/data/import";

    /// <summary>
    /// Configuration key for the host directory that holds the Keycloak realm import files; by default the
    /// <c>Keycloak</c> folder copied next to the AppHost.
    /// </summary>
    public const string RealmImportDirectoryConfigurationKey = "AppHost:IdentityProvider:RealmImportDirectory";
    private const string KeycloakBootstrapAdminUsername = "admin";
    private const string LocalHttpAuthorityAllowed = "true";
    private const string DatabaseObservabilityEnabledEnvironmentVariable = "DatabaseObservability__PostgreSqlIndexHealth__Enabled";
    private const string AdminIndexHealthConnectionStringParameterName = "admin-index-health-connection-string";
    private const string CatalogIndexHealthConnectionStringParameterName = "catalog-index-health-connection-string";
    private const string AdminIndexHealthConnectionStringEnvironmentVariable = "ConnectionStrings__admin-index-health";
    private const string CatalogIndexHealthConnectionStringEnvironmentVariable = "ConnectionStrings__catalog-index-health";

    /// <summary>Default bucket for Viajantes media object storage.</summary>
    private const string SeaweedFsBucketDefault = "viajantes-media";

    /// <summary>The named volume that keeps the ClamAV virus definitions between AppHost runs.</summary>
    private const string ClamAvDefinitionsVolumeName = "clamav-definitions";

    /// <summary>
    /// Configuration key that opts into keeping application data between AppHost runs. Data containers are ephemeral by
    /// default; with <c>true</c>, PostgreSQL and SeaweedFS get a persistent lifetime and a data volume, and Redis a
    /// persistent lifetime only.
    /// </summary>
    public const string PersistentDataConfigurationKey = "AppHost:PersistentData";

    /// <summary>Suffix of the parameter that holds the PostgreSQL password when data is persisted.</summary>
    private const string PasswordParameterSuffix = "-password";

    /// <summary>
    /// Gets whether <see cref="PersistentDataConfigurationKey"/> opts into keeping application data between runs.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns><see langword="true"/> when persistent data is configured.</returns>
    public static bool PersistsData(this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Configuration.GetValue<bool>(PersistentDataConfigurationKey);
    }

    /// <summary>
    /// Adds the PostgreSQL server and, when selected, its PgWeb companion resource.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="includePgWeb">Whether to include the PgWeb companion resource.</param>
    /// <returns>The configured PostgreSQL server resource.</returns>
    public static IResourceBuilder<PostgresServerResource> AddDatabaseServer(this IDistributedApplicationBuilder builder, bool includePgWeb)
    {
        var persistsData = builder.PersistsData();

        var password = persistsData
            ? builder.AddParameter(
                ResourceNames.DatabaseServer + PasswordParameterSuffix,
                new GenerateParameterDefault { MinLength = 32, Special = false },
                secret: true,
                persist: true)
            : null;
        var databaseServer = builder.AddPostgres(ResourceNames.DatabaseServer, password: password)
            .WithImageTag(PostgresContainerImageTags.Tag)
            .WithImageSHA256(PostgresContainerImageTags.Digest)
            .WithArgs("-c", "max_connections=200");

        if (persistsData)
        {
            databaseServer.WithLifetime(ContainerLifetime.Persistent).WithDataVolume();
        }

        if (includePgWeb)
        {
            databaseServer.WithPgWeb(pgweb => pgweb
                .WithImageTag(PgWebImageTag)
                .WithImageSHA256(PgWebImageDigest));
        }

        return databaseServer;
    }

    /// <summary>
    /// Adds the Redis cache and, when selected, its RedisInsight companion resource.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="includeRedisInsight">Whether to include the RedisInsight companion resource.</param>
    /// <returns>The configured Redis resource.</returns>
    public static IResourceBuilder<RedisResource> AddCache(this IDistributedApplicationBuilder builder, bool includeRedisInsight)
    {
        var cache = builder.AddRedis(ResourceNames.Cache)
            .WithImageTag(RedisContainerImageTags.Tag)
            .WithImageSHA256(RedisContainerImageTags.Digest);

        if (builder.PersistsData())
        {
            cache.WithLifetime(ContainerLifetime.Persistent);
        }

        if (includeRedisInsight)
        {
            cache.WithRedisInsight(redisInsight => redisInsight
                .WithImageTag(RedisInsightImageTag)
                .WithImageSHA256(RedisInsightImageDigest));
        }

        return cache;
    }

    /// <summary>
    /// Adds the ClamAV resource that scans Viajantes uploads for malware.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns>The configured ClamAV resource.</returns>
    public static IResourceBuilder<ClamAvResource> AddMalwareScanner(this IDistributedApplicationBuilder builder)
    {
        return builder.AddClamAv(ResourceNames.ClamAv).WithDataVolume(ClamAvDefinitionsVolumeName);
    }

    /// <summary>
    /// Adds the SeaweedFS resource that stores Viajantes media objects.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns>The configured SeaweedFS resource.</returns>
    public static IResourceBuilder<SeaweedFsResource> AddMediaObjectStorage(this IDistributedApplicationBuilder builder)
    {
        var storage = builder.AddSeaweedFs(ResourceNames.SeaweedFs, SeaweedFsBucketDefault);
        return builder.PersistsData() ? storage.WithLifetime(ContainerLifetime.Persistent).WithDataVolume() : storage;
    }

    /// <summary>
    /// Adds a digest-pinned Keycloak identity provider when the AppHost runs locally.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="managementWebClientSecret">The confidential Management Web client secret.</param>
    /// <returns>The configured local identity-provider resource, or <see langword="null"/> during publish.</returns>
    public static IResourceBuilder<ContainerResource>? AddRunModeIdentityProvider(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ParameterResource> managementWebClientSecret)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(managementWebClientSecret);

        if (!builder.ExecutionContext.IsRunMode)
        {
            return null;
        }

        var conformanceUserPassword = builder.AddGeneratedSecret(ResourceNames.IdentityProviderConformanceUserPassword);
        var operatorConformanceUserPassword = builder.AddGeneratedSecret(ResourceNames.IdentityProviderOperatorConformanceUserPassword);
        var k6ClientSecret = builder.AddGeneratedSecret(ResourceNames.IdentityProviderK6ClientSecret);
        return AddIdentityProvider(builder, managementWebClientSecret, conformanceUserPassword, operatorConformanceUserPassword, k6ClientSecret);
    }

    /// <summary>
    /// Gets the secret parameter that holds the client secret of the k6 service-account client.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns>The parameter that <see cref="AddRunModeIdentityProvider"/> created.</returns>
    public static IResourceBuilder<ParameterResource> GetK6ClientSecret(this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.CreateResourceBuilder(builder.Resources.OfType<ParameterResource>()
            .Single(static parameter => string.Equals(parameter.Name, ResourceNames.IdentityProviderK6ClientSecret, StringComparison.Ordinal)));
    }

    private static IResourceBuilder<ContainerResource> AddIdentityProvider(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ParameterResource> managementWebClientSecret,
        IResourceBuilder<ParameterResource> conformanceUserPassword,
        IResourceBuilder<ParameterResource> operatorConformanceUserPassword,
        IResourceBuilder<ParameterResource> k6ClientSecret)
    {
        ArgumentNullException.ThrowIfNull(conformanceUserPassword);
        ArgumentNullException.ThrowIfNull(operatorConformanceUserPassword);
        ArgumentNullException.ThrowIfNull(k6ClientSecret);

        var bootstrapAdminPassword = builder.AddGeneratedSecret(ResourceNames.IdentityProviderAdminPassword);
        var realmImportPath = builder.Configuration[RealmImportDirectoryConfigurationKey]
            ?? Path.Combine(AppContext.BaseDirectory, "Keycloak");

        return builder.AddContainer(ResourceNames.IdentityProvider, KeycloakImageName)
            .WithImageRegistry(KeycloakImageRegistry)
            .WithImageTag(KeycloakImageTag)
            .WithImageSHA256(OidcProviderImageDigest)
            .WithExternalHttpEndpoints()
            .WithHttpEndpoint(targetPort: 8080, name: "http")
            .WithBindMount(realmImportPath, KeycloakRealmImportDirectory, isReadOnly: true)
            .WithEnvironment(KeycloakBootstrapAdminUsernameEnvironmentVariable, KeycloakBootstrapAdminUsername)
            .WithEnvironment(KeycloakBootstrapAdminPasswordEnvironmentVariable, bootstrapAdminPassword)
            .WithEnvironment(KeycloakManagementClientSecretEnvironmentVariable, managementWebClientSecret)
            .WithEnvironment(KeycloakConformanceUserPasswordEnvironmentVariable, conformanceUserPassword)
            .WithEnvironment(KeycloakOperatorConformanceUserPasswordEnvironmentVariable, operatorConformanceUserPassword)
            .WithEnvironment(KeycloakK6ClientSecretEnvironmentVariable, k6ClientSecret)
            .WithHttpHealthCheck("/realms/viajantes/.well-known/openid-configuration")
            .WithArgs("start-dev", "--import-realm");
    }

    /// <summary>
    /// Adds the database migration service.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="adminDatabase">The Admin database resource.</param>
    /// <param name="catalogDatabase">The Catalog database resource.</param>
    /// <param name="securityDatabase">The Management security database resource.</param>
    /// <returns>The configured migration service resource.</returns>
    public static IResourceBuilder<ProjectResource> AddMigrationService(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> adminDatabase,
        IResourceBuilder<IResourceWithConnectionString> catalogDatabase,
        IResourceBuilder<IResourceWithConnectionString> securityDatabase)
    {
        return builder.AddDevelopmentDotNetProject<ViajantesTurismo_MigrationService>(ResourceNames.MigrationService)
            .WithReference(adminDatabase)
            .WithReference(catalogDatabase)
            .WithReference(securityDatabase)
            .WaitFor(adminDatabase)
            .WaitFor(catalogDatabase)
            .WaitFor(securityDatabase);
    }

    /// <summary>
    /// Adds the opt-in PostgreSQL database observability service.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="adminDatabase">The Admin database resource.</param>
    /// <param name="catalogDatabase">The Catalog database resource.</param>
    /// <param name="migrationService">The migration service resource.</param>
    /// <returns>The configured database observability resource.</returns>
    public static IResourceBuilder<ProjectResource> AddDatabaseObservability(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> adminDatabase,
        IResourceBuilder<IResourceWithConnectionString> catalogDatabase,
        IResourceBuilder<ProjectResource> migrationService)
    {
        var adminConnectionString = builder.AddParameter(AdminIndexHealthConnectionStringParameterName, secret: true);
        var catalogConnectionString = builder.AddParameter(CatalogIndexHealthConnectionStringParameterName, secret: true);

        var resource = builder.AddDevelopmentDotNetProject<ViajantesTurismo_DatabaseObservability>(ResourceNames.DatabaseObservability)
            .WaitFor(adminDatabase)
            .WaitFor(catalogDatabase)
            .WaitForCompletion(migrationService);

        return resource
            .WithEnvironment(DatabaseObservabilityEnabledEnvironmentVariable, "true")
            .WithEnvironment(AdminIndexHealthConnectionStringEnvironmentVariable, adminConnectionString)
            .WithEnvironment(CatalogIndexHealthConnectionStringEnvironmentVariable, catalogConnectionString);
    }

    /// <summary>
    /// Adds the Admin API service.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="adminDatabase">The Admin database resource.</param>
    /// <param name="migrationService">The migration service resource.</param>
    /// <param name="clamAv">The private ClamAV malware scanner resource.</param>
    /// <param name="identityProvider">The local identity-provider resource.</param>
    /// <returns>The configured Admin API resource.</returns>
    public static IResourceBuilder<ProjectResource> AddAdminApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> adminDatabase,
        IResourceBuilder<ProjectResource> migrationService,
        IResourceBuilder<ClamAvResource>? clamAv,
        IResourceBuilder<ContainerResource>? identityProvider)
    {
        return builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Admin_Api>(ResourceNames.Api)
            .WithReference(adminDatabase)
            .WaitFor(adminDatabase)
            .WaitForCompletion(migrationService)
            .WithLocalIdentityProvider(identityProvider)
            .WithPayments()
            .WithMalwareScanner(clamAv);
    }

    /// <summary>
    /// Adds the Catalog API service.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="adminDatabase">The Admin database resource that owns integration-event transport.</param>
    /// <param name="catalogDatabase">The Catalog database resource.</param>
    /// <param name="migrationService">The migration service resource.</param>
    /// <param name="clamAv">The optional private ClamAV scanner resource.</param>
    /// <param name="seaweedFs">The optional private SeaweedFS object-storage resource.</param>
    /// <param name="identityProvider">The local identity-provider resource.</param>
    /// <returns>The configured Catalog API resource.</returns>
    public static IResourceBuilder<ProjectResource> AddCatalogApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> adminDatabase,
        IResourceBuilder<IResourceWithConnectionString> catalogDatabase,
        IResourceBuilder<ProjectResource> migrationService,
        IResourceBuilder<ClamAvResource>? clamAv,
        IResourceBuilder<SeaweedFsResource>? seaweedFs,
        IResourceBuilder<ContainerResource>? identityProvider)
    {
        return builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Catalog_Api>(ResourceNames.CatalogApi)
            .WithReference(adminDatabase)
            .WithReference(catalogDatabase)
            .WaitFor(adminDatabase)
            .WaitFor(catalogDatabase)
            .WaitForCompletion(migrationService)
            .WithLocalIdentityProvider(identityProvider)
            .WithMalwareScanner(clamAv)
            .WithMediaObjectStorage(seaweedFs);
    }

    /// <summary>
    /// Adds the Catalog-owned integration-event worker, which runs only Catalog consumers and background services.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="adminDatabase">The Admin database resource that owns the transport table.</param>
    /// <param name="catalogDatabase">The Catalog database resource.</param>
    /// <param name="migrationService">The migration service resource.</param>
    /// <param name="clamAv">The optional private ClamAV scanner resource.</param>
    /// <param name="seaweedFs">The optional private SeaweedFS object-storage resource.</param>
    /// <returns>The configured Catalog integration-event worker resource.</returns>
    public static IResourceBuilder<ProjectResource> AddCatalogIntegrationEventWorker(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> adminDatabase,
        IResourceBuilder<IResourceWithConnectionString> catalogDatabase,
        IResourceBuilder<ProjectResource> migrationService,
        IResourceBuilder<ClamAvResource>? clamAv,
        IResourceBuilder<SeaweedFsResource>? seaweedFs)
    {
        return builder.AddDevelopmentDotNetProject<ViajantesTurismo_Catalog_IntegrationEventWorker>(ResourceNames.CatalogIntegrationEventWorker)
            .WithReference(adminDatabase)
            .WithReference(catalogDatabase)
            .WaitFor(adminDatabase)
            .WaitFor(catalogDatabase)
            .WaitForCompletion(migrationService)
            .WithMalwareScanner(clamAv)
            .WithMediaObjectStorage(seaweedFs);
    }

    /// <summary>
    /// Adds the Admin-owned integration-event worker, which applies Branding revisions to the Admin projection.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="adminDatabase">The Admin database resource that holds the Branding projection.</param>
    /// <param name="catalogDatabase">The Catalog database resource that holds the transport table.</param>
    /// <param name="migrationService">The migration service resource.</param>
    /// <returns>The configured Admin integration-event worker resource.</returns>
    public static IResourceBuilder<ProjectResource> AddAdminIntegrationEventWorker(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> adminDatabase,
        IResourceBuilder<IResourceWithConnectionString> catalogDatabase,
        IResourceBuilder<ProjectResource> migrationService)
    {
        return builder.AddDevelopmentDotNetProject<ViajantesTurismo_Admin_IntegrationEventWorker>(ResourceNames.AdminIntegrationEventWorker)
            .WithReference(adminDatabase)
            .WithReference(catalogDatabase)
            .WaitFor(adminDatabase)
            .WaitFor(catalogDatabase)
            .WaitForCompletion(migrationService);
    }

    private static IResourceBuilder<TDestination> WithSeaweedFsReference<TDestination>(
        this IResourceBuilder<TDestination> destination,
        IResourceBuilder<SeaweedFsResource> seaweedFs)
        where TDestination : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(seaweedFs);
        var builder = destination.ApplicationBuilder;

        return destination
            .WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__Endpoint", seaweedFs.GetEndpoint(SeaweedFsResource.S3EndpointName))
            .WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__AccessKey", builder.CreateResourceBuilder(seaweedFs.Resource.AccessKeyParameter))
            .WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__SecretKey", builder.CreateResourceBuilder(seaweedFs.Resource.SecretKeyParameter))
            .WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__Bucket", builder.CreateResourceBuilder(seaweedFs.Resource.BucketParameter))
            .WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__AutoProvisionBucket", "true");
    }

    /// <summary>
    /// Adds the management web frontend.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="cache">The cache resource.</param>
    /// <param name="securityDatabase">The Management Web security-state database resource.</param>
    /// <param name="migrationService">The migration service resource.</param>
    /// <param name="identityProvider">The local identity-provider resource.</param>
    /// <param name="managementWebClientSecret">The local client secret, supplied only for run mode.</param>
    /// <param name="adminApi">The Admin API resource.</param>
    /// <param name="catalogApi">The Catalog API resource, which also hosts the Branding routes.</param>
    /// <returns>The configured management web resource.</returns>
    public static IResourceBuilder<ProjectResource> AddManagementWeb(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> cache,
        IResourceBuilder<IResourceWithConnectionString> securityDatabase,
        IResourceBuilder<ProjectResource> migrationService,
        IResourceBuilder<ContainerResource>? identityProvider,
        IResourceBuilder<ParameterResource>? managementWebClientSecret,
        IResourceBuilder<ProjectResource> adminApi,
        IResourceBuilder<ProjectResource> catalogApi)
    {
        var managementWeb = builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Management_Web>(ResourceNames.WebApp)
            .WithExternalHttpEndpoints()
            .WithReference(cache)
            .WaitFor(cache)
            .WithReference(securityDatabase)
            .WaitFor(securityDatabase)
            .WaitForCompletion(migrationService)
            .WithLocalIdentityProvider(identityProvider)
            .WithEnvironment(AuthenticationTokenExchangeEnabledEnvironmentVariable, "true")
            .WithEnvironment(AuthenticationTokenExchangeProviderEnvironmentVariable, KeycloakTokenExchangeProvider)
            .WithReference(adminApi)
            .WaitFor(adminApi)
            .WithReference(catalogApi)
            .WaitFor(catalogApi);

        if (managementWebClientSecret is not null)
        {
            managementWeb
                .WithEnvironment(AuthenticationClientIdEnvironmentVariable, ResourceNames.WebApp)
                .WithEnvironment(AuthenticationClientSecretEnvironmentVariable, managementWebClientSecret);
        }

        return managementWeb;
    }

    private static IResourceBuilder<TResource> WithDevelopmentOidcAuthority<TResource>(
        this IResourceBuilder<TResource> resource,
        IResourceBuilder<ContainerResource> identityProvider)
        where TResource : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(identityProvider);

        return resource
            .WithEnvironment(
                AuthenticationAuthorityEnvironmentVariable,
                $"{identityProvider.GetEndpoint("http")}/realms/viajantes")
            .WithEnvironment(
                AuthenticationIssuerEnvironmentVariable,
                $"{identityProvider.GetEndpoint("http")}/realms/viajantes")
            .WithEnvironment(AuthenticationAllowHttpDevelopmentAuthorityEnvironmentVariable, LocalHttpAuthorityAllowed);
    }

    /// <summary>
    /// Points the resource at the local identity provider, or at the external authority configured under
    /// <see cref="ExternalInfrastructure.IdentityProviderAuthorityKey"/> when there is no local one in run mode.
    /// </summary>
    private static IResourceBuilder<TResource> WithLocalIdentityProvider<TResource>(
        this IResourceBuilder<TResource> resource,
        IResourceBuilder<ContainerResource>? identityProvider)
        where TResource : IResourceWithEnvironment, IResourceWithWaitSupport
    {
        if (identityProvider is not null)
        {
            return resource.WaitFor(identityProvider).WithDevelopmentOidcAuthority(identityProvider);
        }

        if (!resource.ApplicationBuilder.ExecutionContext.IsRunMode
            || ExternalInfrastructure.IdentityProviderAuthority(resource.ApplicationBuilder.Configuration) is not { } authority)
        {
            return resource;
        }

        resource.WithEnvironment(AuthenticationAuthorityEnvironmentVariable, authority.AbsoluteUri.TrimEnd('/'))
            .WithEnvironment(AuthenticationIssuerEnvironmentVariable, authority.AbsoluteUri.TrimEnd('/'));
        return authority.Scheme == Uri.UriSchemeHttp
            ? resource.WithEnvironment(AuthenticationAllowHttpDevelopmentAuthorityEnvironmentVariable, LocalHttpAuthorityAllowed)
            : resource;
    }

    /// <summary>Gives the resource the local malware scanner, or the external one configured under <c>AppHost:External:ClamAv</c>.</summary>
    private static IResourceBuilder<TResource> WithMalwareScanner<TResource>(
        this IResourceBuilder<TResource> resource,
        IResourceBuilder<ClamAvResource>? clamAv)
        where TResource : IResourceWithEnvironment, IResourceWithWaitSupport
    {
        if (clamAv is not null)
        {
            return resource.WithClamAvReference(clamAv).WaitFor(clamAv);
        }

        return ExternalInfrastructure.ClamAv(resource.ApplicationBuilder.Configuration) is { } scanner
            ? resource.WithEnvironment("MalwareScanning__ClamAv__Host", scanner.Host)
                .WithEnvironment("MalwareScanning__ClamAv__Port", scanner.Port.ToString(System.Globalization.CultureInfo.InvariantCulture))
            : resource;
    }

    /// <summary>Gives the resource the local media object storage, or the external one configured under <c>AppHost:External:SeaweedFs</c>.</summary>
    private static IResourceBuilder<TResource> WithMediaObjectStorage<TResource>(
        this IResourceBuilder<TResource> resource,
        IResourceBuilder<SeaweedFsResource>? seaweedFs)
        where TResource : IResourceWithEnvironment, IResourceWithWaitSupport
    {
        if (seaweedFs is not null)
        {
            return resource.WithSeaweedFsReference(seaweedFs).WaitFor(seaweedFs);
        }

        return ExternalInfrastructure.SeaweedFs(resource.ApplicationBuilder.Configuration) is { } storage
            ? resource.WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__Endpoint", storage.Endpoint)
                .WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__AccessKey", storage.AccessKey)
                .WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__SecretKey", storage.SecretKey)
                .WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__Bucket", storage.Bucket)
                .WithEnvironment("Catalog__MediaObjectStorage__SeaweedFs__AutoProvisionBucket", "true")
            : resource;
    }

    /// <summary>
    /// Adds the public web frontend.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="catalogApi">The Catalog API resource, which also hosts the Branding routes.</param>
    /// <returns>The configured public web resource.</returns>
    public static IResourceBuilder<ProjectResource> AddPublicWeb(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> catalogApi)
    {
        var replicas = builder.Configuration.GetValue(PublicWebReplicasConfigurationKey, 1);
        if (replicas < 1)
        {
            throw new InvalidOperationException($"{PublicWebReplicasConfigurationKey} must be a positive integer.");
        }

        return builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Public_Web>(ResourceNames.PublicWebApp)
            .WithReplicas(replicas)
            .WithExternalHttpEndpoints()
            .WithSelfOrigin(PublicWebSitemapCanonicalOriginEnvironmentVariable)
            .WithReference(catalogApi)
            .WaitFor(catalogApi);
    }
}
