using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Management.Web;

internal sealed class CatalogToursApiClient(HttpClient httpClient) : ICatalogToursApiClient
{
    public async Task<CatalogTourDto[]> GetTours(CancellationToken ct)
    {
        List<CatalogTourDto>? tours = null;

        await foreach (var tour in httpClient.GetFromJsonAsAsyncEnumerable<CatalogTourDto>("/catalog/tours", ct))
        {
            if (tour is null)
            {
                continue;
            }

            tours ??= [];
            tours.Add(tour);
        }

        return tours?.ToArray() ?? [];
    }
}
