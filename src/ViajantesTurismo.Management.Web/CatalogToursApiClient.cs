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

    public async Task<CatalogTourDto?> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct)
    {
        using var response = await httpClient.PutAsJsonAsync($"/catalog/tours/{id}/presentation", request, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await Helpers.ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);

        return await response.Content.ReadFromJsonAsync<CatalogTourDto>(ct);
    }

    public async Task<CatalogTourDto?> GetTour(Guid id, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(new Uri($"/catalog/tours/{id}", UriKind.Relative), ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatalogTourDto>(ct);
    }
}
