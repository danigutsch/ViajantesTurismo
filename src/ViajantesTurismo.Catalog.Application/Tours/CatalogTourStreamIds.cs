using SharedKernel.EventSourcing;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Creates Catalog tour event stream identifiers.
/// </summary>
public static class CatalogTourStreamIds
{
    /// <summary>The prefix shared by the point lookup and the Catalog event-stream queries.</summary>
    public const string Prefix = "catalog-tour-";

    /// <summary>
    /// Creates the stream identifier for the Catalog tour sourced from an Admin tour.
    /// </summary>
    public static StreamId FromAdminTourId(Guid adminTourId) => StreamId.From($"{Prefix}{adminTourId:N}");
}
