using ViajantesTurismo.Admin.Application.Operations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

/// <summary>Returns a fixture page and captures the last validated request.</summary>
/// <param name="page">The response metadata to return.</param>
internal sealed class StubCatalogDeliveryQuery(GetCatalogDeliveryPageDto page) : ICatalogDeliveryQuery
{
    /// <summary>Gets the captured request, if the endpoint called the query.</summary>
    internal CatalogDeliveryRequest? Request
    {
        get; private set;
    }

    /// <inheritdoc />
    public Task<GetCatalogDeliveryPageDto> ReadCatalogDelivery(CatalogDeliveryRequest request, CancellationToken ct)
    {
        Request = request;
        return Task.FromResult(page);
    }
}
