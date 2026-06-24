using System.Net;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Public.Web;

internal sealed class PublicCatalogApiClient(HttpClient httpClient) : IPublicCatalogApiClient
{
    public async Task<CatalogTourDto[]> GetPublishedTours(CancellationToken cancellationToken)
    {
        List<CatalogTourDto>? tours = null;

        await foreach (var tour in httpClient.GetFromJsonAsAsyncEnumerable<CatalogTourDto>("/public/catalog/tours", cancellationToken))
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

    public async Task<CatalogTourDto?> GetPublishedTourBySlug(string slug, CancellationToken cancellationToken)
    {
        var escapedSlug = Uri.EscapeDataString(slug);
        using var response = await httpClient.GetAsync(new Uri($"/public/catalog/tours/{escapedSlug}", UriKind.Relative), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CatalogTourDto>(cancellationToken);
    }
}
