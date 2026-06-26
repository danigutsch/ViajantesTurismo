using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

internal sealed class FakeCatalogToursApiClient : ICatalogToursApiClient
{
    public CatalogTourDto[] Tours { get; set; } = [];

    public bool ThrowOnGetTours { get; set; }

    public Task<CatalogTourDto[]> GetTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ThrowOnGetTours
            ? throw new HttpRequestException("Catalog unavailable.")
            : Task.FromResult(Tours);
    }
}
