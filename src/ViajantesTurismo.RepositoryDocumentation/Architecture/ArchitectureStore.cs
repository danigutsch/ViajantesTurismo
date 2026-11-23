namespace ViajantesTurismo.RepositoryDocumentation.Architecture;

/// <summary>A database access surface and selected represented table roles.</summary>
/// <param name="Id">The stable diagram node and store identifier.</param>
/// <param name="DbContext">The EF Core context name, when the store is managed with EF Core.</param>
/// <param name="Database">The application resource name for the physical database.</param>
/// <param name="Owner">The bounded context responsible for the access surface.</param>
/// <param name="Access">The access performed by its consuming applications.</param>
/// <param name="Boundary">The trust boundary of the store.</param>
/// <param name="State">The lifecycle of the store.</param>
/// <param name="Consumers">The application identifiers that use the store.</param>
/// <param name="Tables">The selected table roles and handling classes.</param>
internal sealed record ArchitectureStore(
    string Id,
    string? DbContext,
    string Database,
    string Owner,
    string Access,
    string Boundary,
    string State,
    string[] Consumers,
    ArchitectureTable[] Tables);
