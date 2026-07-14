namespace ViajantesTurismo.DatabaseObservability;

internal sealed class PostgreSqlIndexHealthHostOptions
{
    public const string SectionName = "DatabaseObservability:PostgreSqlIndexHealth";
    public const string AdminConnectionStringName = "admin-index-health";
    public const string CatalogConnectionStringName = "catalog-index-health";

    public bool Enabled { get; set; }

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromHours(1);

    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
