using Projects;
using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

var builder = DistributedApplication.CreateBuilder(args);

const string PostgresImageDigest = "00bc86618629af00d2937fdc5a5d63db3ff8450acf52f0636ec813c7f4902929";
const string PgWebImageDigest = "a5256d416e2e8b92d69a4459058e3eca33a9f075d8325491644411d0bc3bd70b";
const string RedisImageDigest = "4483474d5e78c444ce180037def4430ec0d02553663ded2a6d7a1c922da00ecf";
const string RedisInsightImageDigest = "4455c3304eafe1311d0a367022bad41520e307138b7272e1c0c308ce781f7162";

var databaseServer = builder.AddPostgres(ResourceNames.DatabaseServer)
    .WithImageSHA256(PostgresImageDigest)
    .WithPgWeb(pgweb => pgweb.WithImageSHA256(PgWebImageDigest));
var database = databaseServer.AddDatabase(ResourceNames.Database);

var cache = builder.AddRedis(ResourceNames.Cache)
    .WithImageSHA256(RedisImageDigest)
    .WithRedisInsight(redisInsight => redisInsight.WithImageSHA256(RedisInsightImageDigest));

var migrationService = builder.AddDevelopmentDotNetProject<ViajantesTurismo_MigrationService>(ResourceNames.MigrationService)
    .WithReference(database)
    .WaitFor(database);

var apiService = builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Admin_ApiService>(ResourceNames.Api)
    .WithHttpHealthCheck(EndpointPaths.Health)
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(migrationService);

var catalogApiService = builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Catalog_ApiService>(ResourceNames.CatalogApi)
    .WithHttpHealthCheck(EndpointPaths.Health)
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(migrationService);

builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Management_Web>(ResourceNames.WebApp)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck(EndpointPaths.Health)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(catalogApiService)
    .WaitFor(catalogApiService);

builder.AddDevelopmentAspNetCoreProject<ViajantesTurismo_Public_Web>(ResourceNames.PublicWebApp)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck(EndpointPaths.Health)
    .WithReference(catalogApiService)
    .WaitFor(catalogApiService);

builder.AddAdminPerformanceSmoke(apiService);

await builder.Build().RunAsync();
