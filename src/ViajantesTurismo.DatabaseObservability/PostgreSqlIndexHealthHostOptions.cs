namespace ViajantesTurismo.DatabaseObservability;

/// <summary>Host options for PostgreSQL index-health monitoring.</summary>
internal sealed class PostgreSqlIndexHealthHostOptions
{
    /// <summary>Configuration section name for these options.</summary>
    public const string SectionName = "DatabaseObservability:PostgreSqlIndexHealth";
    /// <summary>Connection string name of the Admin database used for index-health checks.</summary>
    public const string AdminConnectionStringName = "admin-index-health";
    /// <summary>Connection string name of the Catalog database used for index-health checks.</summary>
    public const string CatalogConnectionStringName = "catalog-index-health";

    /// <summary>Whether index-health monitoring is enabled.</summary>
    public bool Enabled
    {
        get; set;
    }

    /// <summary>How often index health is polled.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Timeout for each index-health query.</summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
