using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Operations;

/// <summary>Reads unfinished delivery metadata for the Catalog consumer owned by this application.</summary>
public interface ICatalogDeliveryQuery
{
    /// <summary>Reads one page using the supplied filters and continuation.</summary>
    Task<GetCatalogDeliveryPageDto> ReadCatalogDelivery(CatalogDeliveryRequest request, CancellationToken ct);
}
