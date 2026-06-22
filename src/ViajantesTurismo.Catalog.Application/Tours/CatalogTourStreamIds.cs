using SharedKernel.EventSourcing;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Creates Catalog tour event stream identifiers.
/// </summary>
public static class CatalogTourStreamIds
{
    /// <summary>
    /// Creates the stream identifier for the Catalog tour sourced from an Admin tour.
    /// </summary>
    public static StreamId FromAdminTourId(Guid adminTourId) => StreamId.From($"catalog-tour-{adminTourId:N}");
}
