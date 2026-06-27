using Projects;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.AppHost;

/// <summary>
/// Adds named resources to the local Aspire model.
/// </summary>
internal static class AppHostResourceExtensions
{
    private const string PostgresImageDigest = "00bc86618629af00d2937fdc5a5d63db3ff8450acf52f0636ec813c7f4902929";
    private const string PgWebImageDigest = "a5256d416e2e8b92d69a4459058e3eca33a9f075d8325491644411d0bc3bd70b";
    private const string RedisImageDigest = "4483474d5e78c444ce180037def4430ec0d02553663ded2a6d7a1c922da00ecf";
    private const string RedisInsightImageDigest = "4455c3304eafe1311d0a367022bad41520e307138b7272e1c0c308ce781f7162";

    /// <summary>
    /// Adds the PostgreSQL server and PgWeb companion resource.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns>The configured PostgreSQL server resource.</returns>
    public static IResourceBuilder<PostgresServerResource> AddDatabaseServer(this IDistributedApplicationBuilder builder)
    {
        return builder.AddPostgres(ResourceNames.DatabaseServer)
            .WithImageSHA256(PostgresImageDigest)
            .WithPgWeb(pgweb => pgweb.WithImageSHA256(PgWebImageDigest));
    }

    /// <summary>
    /// Adds the Redis cache and RedisInsight companion resource.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns>The configured Redis resource.</returns>
    public static IResourceBuilder<RedisResource> AddCache(this IDistributedApplicationBuilder builder)
    {
        return builder.AddRedis(ResourceNames.Cache)
            .WithImageSHA256(RedisImageDigest)
            .WithRedisInsight(redisInsight => redisInsight.WithImageSHA256(RedisInsightImageDigest));
    }

    /// <summary>
    /// Adds the database migration service.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="database">The database resource.</param>
    /// <returns>The configured migration service resource.</returns>
    public static IResourceBuilder<ProjectResource> AddMigrationService(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> database)
    {
        return builder.AddDevelopmentDotNetProject<ViajantesTurismo_MigrationService>(ResourceNames.MigrationService)
            .WithReference(database)
            .WaitFor(database);
    }

    /// <summary>
    /// Adds the Admin API service.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="database">The database resource.</param>
    /// <param name="migrationService">The migration service resource.</param>
    /// <returns>The configured Admin API resource.</returns>
    public static IResourceBuilder<ProjectResource> AddAdminApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> database,
        IResourceBuilder<ProjectResource> migrationService)
    {
        return builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Admin_ApiService>(ResourceNames.Api)
            .WithHttpHealthCheck(EndpointPaths.Health)
            .WithReference(database)
            .WaitFor(database)
            .WaitForCompletion(migrationService);
    }

    /// <summary>
    /// Adds the Catalog API service.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="database">The database resource.</param>
    /// <param name="migrationService">The migration service resource.</param>
    /// <returns>The configured Catalog API resource.</returns>
    public static IResourceBuilder<ProjectResource> AddCatalogApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> database,
        IResourceBuilder<ProjectResource> migrationService)
    {
        return builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Catalog_ApiService>(ResourceNames.CatalogApi)
            .WithHttpHealthCheck(EndpointPaths.Health)
            .WithReference(database)
            .WaitFor(database)
            .WaitForCompletion(migrationService);
    }

    /// <summary>
    /// Adds the management web frontend.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="cache">The cache resource.</param>
    /// <param name="apiService">The Admin API resource.</param>
    /// <param name="catalogApiService">The Catalog API resource.</param>
    /// <returns>The configured management web resource.</returns>
    public static IResourceBuilder<ProjectResource> AddManagementWeb(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<RedisResource> cache,
        IResourceBuilder<ProjectResource> apiService,
        IResourceBuilder<ProjectResource> catalogApiService)
    {
        return builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Management_Web>(ResourceNames.WebApp)
            .WithExternalHttpEndpoints()
            .WithHttpHealthCheck(EndpointPaths.Health)
            .WithReference(cache)
            .WaitFor(cache)
            .WithReference(apiService)
            .WaitFor(apiService)
            .WithReference(catalogApiService)
            .WaitFor(catalogApiService);
    }

    /// <summary>
    /// Adds the public web frontend.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="catalogApiService">The Catalog API resource.</param>
    /// <returns>The configured public web resource.</returns>
    public static IResourceBuilder<ProjectResource> AddPublicWeb(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> catalogApiService)
    {
        return builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Public_Web>(ResourceNames.PublicWebApp)
            .WithExternalHttpEndpoints()
            .WithHttpHealthCheck(EndpointPaths.Health)
            .WithReference(catalogApiService)
            .WaitFor(catalogApiService);
    }
}
