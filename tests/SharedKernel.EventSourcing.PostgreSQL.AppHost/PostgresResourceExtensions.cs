namespace SharedKernel.EventSourcing.PostgreSQL.AppHost;

/// <summary>
/// Adds PostgreSQL resources for event sourcing tests.
/// </summary>
internal static class PostgresResourceExtensions
{
    /// <summary>
    /// Adds the PostgreSQL server for event sourcing tests.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The Aspire resource name.</param>
    /// <returns>The configured PostgreSQL server resource.</returns>
    public static IResourceBuilder<PostgresServerResource> AddDatabaseServer(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        return builder.AddPostgres(name);
    }
}
