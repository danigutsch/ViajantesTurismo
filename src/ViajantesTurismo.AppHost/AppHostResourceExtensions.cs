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

    /// <summary>
    /// Adds the PostgreSQL server and PgWeb companion resource.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns>The configured PostgreSQL server resource.</returns>
    public static IResourceBuilder<PostgresServerResource> AddDatabaseServer(this IDistributedApplicationBuilder builder)
    {
        return builder.AddPostgres(ResourceNames.DatabaseServer)
            .WithImageTag(PostgresImageTag)
            .WithImageSHA256(PostgresImageDigest)
            .WithPgWeb(pgweb => pgweb
                .WithImageTag(PgWebImageTag)
                .WithImageSHA256(PgWebImageDigest));
    }

    /// <summary>
    /// Adds the Redis cache and RedisInsight companion resource.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns>The configured Redis resource.</returns>
    public static IResourceBuilder<RedisResource> AddCache(this IDistributedApplicationBuilder builder)
    {
        return builder.AddRedis(ResourceNames.Cache)
            .WithImageTag(RedisImageTag)
            .WithImageSHA256(RedisImageDigest)
            .WithRedisInsight(redisInsight => redisInsight
                .WithImageTag(RedisInsightImageTag)
                .WithImageSHA256(RedisInsightImageDigest));
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
