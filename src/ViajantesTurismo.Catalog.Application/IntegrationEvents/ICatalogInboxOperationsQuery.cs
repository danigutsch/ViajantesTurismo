using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Application.IntegrationEvents;

/// <summary>Reads retained duplicate history of the Catalog integration-event consumer.</summary>
public interface ICatalogInboxOperationsQuery
{
    /// <summary>Reads the retained duplicate counts and latest observation.</summary>
    Task<GetCatalogInboxHealthDto> ReadHealth(CancellationToken ct);

    /// <summary>Reads one bounded page of completed duplicate observations.</summary>
    Task<GetCatalogInboxDuplicatePageDto> ReadPage(CatalogInboxDuplicateFilters filters, CancellationToken ct);
}
