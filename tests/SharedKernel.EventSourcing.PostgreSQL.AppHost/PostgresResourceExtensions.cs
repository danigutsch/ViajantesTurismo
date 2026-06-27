namespace SharedKernel.EventSourcing.PostgreSQL.AppHost;

/// <summary>
/// Adds PostgreSQL resources for event sourcing tests.
/// </summary>
internal static class PostgresResourceExtensions
{
    private const string PostgresImageDigest = "00bc86618629af00d2937fdc5a5d63db3ff8450acf52f0636ec813c7f4902929";

    /// <summary>
    /// Adds the PostgreSQL server with the repository-pinned image digest.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The Aspire resource name.</param>
    /// <returns>The configured PostgreSQL server resource.</returns>
    public static IResourceBuilder<PostgresServerResource> AddDatabaseServer(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        return builder.AddPostgres(name)
            .WithImageSHA256(PostgresImageDigest);
    }
}
