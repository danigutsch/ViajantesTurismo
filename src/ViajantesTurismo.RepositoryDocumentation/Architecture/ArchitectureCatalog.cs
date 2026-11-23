namespace ViajantesTurismo.RepositoryDocumentation.Architecture;

/// <summary>Repository-owned architecture metadata for the data ownership view.</summary>
/// <param name="SchemaVersion">The version of the metadata format.</param>
/// <param name="Boundaries">The trust boundary labels keyed by their identifiers.</param>
/// <param name="Lifecycle">The lifecycle labels keyed by their identifiers.</param>
/// <param name="DataClasses">The handling classes keyed by their identifiers.</param>
/// <param name="TableRoles">The persistence roles keyed by their identifiers.</param>
/// <param name="Services">The consuming applications.</param>
/// <param name="Stores">The data stores used by those applications.</param>
internal sealed record ArchitectureCatalog(
    int SchemaVersion,
    Dictionary<string, string> Boundaries,
    Dictionary<string, string> Lifecycle,
    Dictionary<string, string> DataClasses,
    Dictionary<string, string> TableRoles,
    ArchitectureService[] Services,
    ArchitectureStore[] Stores);
