using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>Reads Catalog inbox observations through the Operations API.</summary>
public interface ICatalogInboxOperationsApiClient
{
    /// <summary>Reads the retained completed-duplicate totals and latest observation time.</summary>
    Task<HttpOutcome<GetCatalogInboxHealthDto>> ReadHealth(CancellationToken ct);

    /// <summary>Reads one bounded page of retained completed duplicates.</summary>
    Task<HttpOutcome<GetCatalogInboxDuplicatePageDto>> ReadPage(CatalogInboxDuplicateFilters filters, CancellationToken ct);
}
