using Projects;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.AppHost;

/// <summary>
/// Adds named resources to the local Aspire model.
/// </summary>
internal static class AppHostResourceExtensions
{
    /// <summary>Tag for <c>docker.io/library/postgres:18.4</c>.</summary>
    private const string PostgresImageTag = "18.4";

    /// <summary>Digest for <c>docker.io/library/postgres:18.4</c>.</summary>
    private const string PostgresImageDigest = "4aabea78cf39b90e834caf3af7d602a18565f6fe2508705c8d01aa63245c2e20";

    /// <summary>Tag for <c>docker.io/sosedoff/pgweb:0.17.0</c>.</summary>
    private const string PgWebImageTag = "0.17.0";

    /// <summary>Digest for <c>docker.io/sosedoff/pgweb:0.17.0</c>.</summary>
    private const string PgWebImageDigest = "a5256d416e2e8b92d69a4459058e3eca33a9f075d8325491644411d0bc3bd70b";

    /// <summary>Tag for <c>docker.io/library/redis:8.8</c>.</summary>
    private const string RedisImageTag = "8.8";

    /// <summary>Digest for <c>docker.io/library/redis:8.8</c>.</summary>
    private const string RedisImageDigest = "2838d5524559494f6f1cd66e97e76b200d64a633a8614200620755ed395daf32";

    /// <summary>Tag for <c>docker.io/redis/redisinsight:3.6</c>.</summary>
    private const string RedisInsightImageTag = "3.6";

    /// <summary>Digest for <c>docker.io/redis/redisinsight:3.6</c>.</summary>
    private const string RedisInsightImageDigest = "aa21bbd198455b4ad964f76782db951155aa0d712321f599972d1525f031f0e6";

    /// <summary>Configuration key for Public Web canonical sitemap URLs.</summary>
    private const string PublicWebSitemapCanonicalOriginEnvironmentVariable = "PublicWeb__Sitemap__CanonicalOrigin";
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
    private const string KeycloakRealmImportDirectory = "/opt/keycloak/data/import";
    private const string KeycloakBootstrapAdminUsername = "admin";
    private const string LocalHttpAuthorityAllowed = "true";
    private const string DatabaseObservabilityEnabledEnvironmentVariable = "DatabaseObservability__PostgreSqlIndexHealth__Enabled";
    private const string AdminIndexHealthConnectionStringParameterName = "admin-index-health-connection-string";
    private const string CatalogIndexHealthConnectionStringParameterName = "catalog-index-health-connection-string";
    private const string AdminIndexHealthConnectionStringEnvironmentVariable = "ConnectionStrings__admin-index-health";
    private const string CatalogIndexHealthConnectionStringEnvironmentVariable = "ConnectionStrings__catalog-index-health";

    /// <summary>Default bucket for Viajantes media object storage.</summary>
    private const string SeaweedFsBucketDefault = "viajantes-media";

    /// <summary>
    /// Adds the PostgreSQL server and, when selected, its PgWeb companion resource.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="includePgWeb">Whether to include the PgWeb companion resource.</param>
    /// <returns>The configured PostgreSQL server resource.</returns>
    public static IResourceBuilder<PostgresServerResource> AddDatabaseServer(this IDistributedApplicationBuilder builder, bool includePgWeb)
    {
        var databaseServer = builder.AddPostgres(ResourceNames.DatabaseServer)
            .WithImageTag(PostgresImageTag)
            .WithImageSHA256(PostgresImageDigest)
            .WithArgs("-c", "max_connections=200");

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
            .WithImageTag(RedisImageTag)
            .WithImageSHA256(RedisImageDigest);

        if (includeRedisInsight)
        {
            cache.WithRedisInsight(redisInsight => redisInsight
                .WithImageTag(RedisInsightImageTag)
                .WithImageSHA256(RedisInsightImageDigest));
        }

        return cache;
    }

    /// <summary>
    /// Adds the SeaweedFS resource that stores Viajantes media objects.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns>The configured SeaweedFS resource.</returns>
    public static IResourceBuilder<SeaweedFsResource> AddMediaObjectStorage(this IDistributedApplicationBuilder builder)
    {
        return builder.AddSeaweedFs(ResourceNames.SeaweedFs, SeaweedFsBucketDefault);
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

        var conformanceUserPassword = builder.AddParameter(ResourceNames.IdentityProviderConformanceUserPassword, secret: true);
        return AddIdentityProvider(builder, managementWebClientSecret, conformanceUserPassword);
    }

    private static IResourceBuilder<ContainerResource> AddIdentityProvider(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ParameterResource> managementWebClientSecret,
        IResourceBuilder<ParameterResource> conformanceUserPassword)
    {
        ArgumentNullException.ThrowIfNull(conformanceUserPassword);

        var bootstrapAdminPassword = builder.AddParameter(ResourceNames.IdentityProviderAdminPassword, secret: true);
        var realmImportPath = Path.Combine(AppContext.BaseDirectory, "Keycloak");

        return builder.AddContainer(ResourceNames.IdentityProvider, KeycloakImageName, KeycloakImageRegistry)
            .WithImageTag(KeycloakImageTag)
            .WithImageSHA256(OidcProviderImageDigest)
            .WithExternalHttpEndpoints()
            .WithHttpEndpoint(targetPort: 8080, name: "http")
            .WithBindMount(realmImportPath, KeycloakRealmImportDirectory, isReadOnly: true)
            .WithEnvironment(KeycloakBootstrapAdminUsernameEnvironmentVariable, KeycloakBootstrapAdminUsername)
            .WithEnvironment(KeycloakBootstrapAdminPasswordEnvironmentVariable, bootstrapAdminPassword)
            .WithEnvironment(KeycloakManagementClientSecretEnvironmentVariable, managementWebClientSecret)
            .WithEnvironment(KeycloakConformanceUserPasswordEnvironmentVariable, conformanceUserPassword)
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
        IResourceBuilder<PostgresDatabaseResource> adminDatabase,
        IResourceBuilder<PostgresDatabaseResource> catalogDatabase,
        IResourceBuilder<PostgresDatabaseResource> securityDatabase)
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
        IResourceBuilder<PostgresDatabaseResource> adminDatabase,
        IResourceBuilder<PostgresDatabaseResource> catalogDatabase,
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
    /// <param name="brandingApiService">The Branding API resource.</param>
    /// <param name="migrationService">The migration service resource.</param>
    /// <param name="clamAv">The private ClamAV malware scanner resource.</param>
    /// <param name="identityProvider">The local identity-provider resource.</param>
    /// <returns>The configured Admin API resource.</returns>
    public static IResourceBuilder<ProjectResource> AddAdminApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> adminDatabase,
        IResourceBuilder<ProjectResource> brandingApiService,
        IResourceBuilder<ProjectResource> migrationService,
        IResourceBuilder<ClamAvResource> clamAv,
        IResourceBuilder<ContainerResource>? identityProvider)
    {
        return builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Admin_ApiService>(ResourceNames.Api)
            .WithHttpHealthCheck(EndpointPaths.Health)
            .WithReference(adminDatabase)
            .WithReference(brandingApiService)
            .WaitFor(adminDatabase)
            .WaitFor(brandingApiService)
            .WaitForCompletion(migrationService)
            .WithClamAvReference(clamAv)
            .WaitFor(clamAv)
            .WithLocalIdentityProvider(identityProvider);
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
        IResourceBuilder<PostgresDatabaseResource> adminDatabase,
        IResourceBuilder<PostgresDatabaseResource> catalogDatabase,
        IResourceBuilder<ProjectResource> migrationService,
        IResourceBuilder<ClamAvResource>? clamAv,
        IResourceBuilder<SeaweedFsResource>? seaweedFs,
        IResourceBuilder<ContainerResource>? identityProvider)
    {
        var catalogApi = builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Catalog_ApiService>(ResourceNames.CatalogApi)
            .WithHttpHealthCheck(EndpointPaths.Health)
            .WithReference(adminDatabase)
            .WithReference(catalogDatabase)
            .WaitFor(adminDatabase)
            .WaitFor(catalogDatabase)
            .WaitForCompletion(migrationService)
            .WithLocalIdentityProvider(identityProvider);

        if (clamAv is not null)
        {
            catalogApi.WithClamAvReference(clamAv).WaitFor(clamAv);
        }

        if (seaweedFs is not null)
        {
            catalogApi.WithSeaweedFsReference(seaweedFs).WaitFor(seaweedFs);
        }

        return catalogApi;
    }

    /// <summary>
    /// Adds the Branding API service.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="catalogDatabase">The Catalog database resource that also stores Branding settings.</param>
    /// <param name="migrationService">The migration service resource.</param>
    /// <param name="identityProvider">The local identity-provider resource.</param>
    /// <returns>The configured Branding API resource.</returns>
    public static IResourceBuilder<ProjectResource> AddBrandingApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> catalogDatabase,
        IResourceBuilder<ProjectResource> migrationService,
        IResourceBuilder<ContainerResource>? identityProvider)
    {
        return builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Branding_ApiService>(ResourceNames.BrandingApi)
            .WithHttpHealthCheck(EndpointPaths.Health)
            .WithReference(catalogDatabase)
            .WaitFor(catalogDatabase)
            .WaitForCompletion(migrationService)
            .WithLocalIdentityProvider(identityProvider);
    }

    /// <summary>
    /// Adds the standalone integration-event worker.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="adminDatabase">The Admin database resource that owns the transport table.</param>
    /// <param name="catalogDatabase">The Catalog database resource.</param>
    /// <param name="migrationService">The migration service resource.</param>
    /// <param name="clamAv">The optional private ClamAV scanner resource.</param>
    /// <param name="seaweedFs">The optional private SeaweedFS object-storage resource.</param>
    /// <returns>The configured integration-event worker resource.</returns>
    public static IResourceBuilder<ProjectResource> AddIntegrationEventWorker(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> adminDatabase,
        IResourceBuilder<PostgresDatabaseResource> catalogDatabase,
        IResourceBuilder<ProjectResource> migrationService,
        IResourceBuilder<ClamAvResource>? clamAv,
        IResourceBuilder<SeaweedFsResource>? seaweedFs)
    {
        var worker = builder.AddDevelopmentDotNetProject<ViajantesTurismo_IntegrationEventWorker>(ResourceNames.IntegrationEventWorker)
            .WithReference(adminDatabase)
            .WithReference(catalogDatabase)
            .WaitFor(adminDatabase)
            .WaitFor(catalogDatabase)
            .WaitForCompletion(migrationService);

        if (clamAv is not null)
        {
            worker.WithClamAvReference(clamAv).WaitFor(clamAv);
        }

        if (seaweedFs is not null)
        {
            worker.WithSeaweedFsReference(seaweedFs).WaitFor(seaweedFs);
        }

        return worker;
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
    /// <param name="managementWebClientSecret">The confidential Management Web client secret.</param>
    /// <param name="apiService">The Admin API resource.</param>
    /// <param name="catalogApiService">The Catalog API resource.</param>
    /// <param name="brandingApiService">The Branding API resource.</param>
    /// <returns>The configured management web resource.</returns>
    public static IResourceBuilder<ProjectResource> AddManagementWeb(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<RedisResource> cache,
        IResourceBuilder<PostgresDatabaseResource> securityDatabase,
        IResourceBuilder<ProjectResource> migrationService,
        IResourceBuilder<ContainerResource>? identityProvider,
        IResourceBuilder<ParameterResource> managementWebClientSecret,
        IResourceBuilder<ProjectResource> apiService,
        IResourceBuilder<ProjectResource> catalogApiService,
        IResourceBuilder<ProjectResource> brandingApiService)
    {
        return builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Management_Web>(ResourceNames.WebApp)
            .WithExternalHttpEndpoints()
            .WithHttpHealthCheck(EndpointPaths.Health)
            .WithReference(cache)
            .WaitFor(cache)
            .WithReference(securityDatabase)
            .WaitFor(securityDatabase)
            .WaitForCompletion(migrationService)
            .WithLocalIdentityProvider(identityProvider)
            .WithEnvironment(AuthenticationClientIdEnvironmentVariable, ResourceNames.WebApp)
            .WithEnvironment(AuthenticationClientSecretEnvironmentVariable, managementWebClientSecret)
            .WithEnvironment(AuthenticationTokenExchangeEnabledEnvironmentVariable, "true")
            .WithEnvironment(AuthenticationTokenExchangeProviderEnvironmentVariable, KeycloakTokenExchangeProvider)
            .WithReference(apiService)
            .WaitFor(apiService)
            .WithReference(catalogApiService)
            .WaitFor(catalogApiService)
            .WithReference(brandingApiService)
            .WaitFor(brandingApiService);
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

    private static IResourceBuilder<TResource> WithLocalIdentityProvider<TResource>(
        this IResourceBuilder<TResource> resource,
        IResourceBuilder<ContainerResource>? identityProvider)
        where TResource : IResourceWithEnvironment, IResourceWithWaitSupport
    {
        return identityProvider is null
            ? resource
            : resource.WaitFor(identityProvider).WithDevelopmentOidcAuthority(identityProvider);
    }

    /// <summary>
    /// Adds the public web frontend.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="catalogApiService">The Catalog API resource.</param>
    /// <param name="brandingApiService">The Branding API resource.</param>
    /// <returns>The configured public web resource.</returns>
    public static IResourceBuilder<ProjectResource> AddPublicWeb(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> catalogApiService,
        IResourceBuilder<ProjectResource> brandingApiService)
    {
        var publicWeb = builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Public_Web>(ResourceNames.PublicWebApp);

        return publicWeb
            .WithExternalHttpEndpoints()
            .WithEnvironment(PublicWebSitemapCanonicalOriginEnvironmentVariable, publicWeb.GetEndpoint("https"))
            .WithHttpHealthCheck(EndpointPaths.Health)
            .WithReference(catalogApiService)
            .WaitFor(catalogApiService)
            .WithReference(brandingApiService)
            .WaitFor(brandingApiService);
    }
}
