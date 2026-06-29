using System.Net;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Public.Web;

internal sealed class PublicCatalogApiClient(HttpClient httpClient) : IPublicCatalogApiClient
{
    public async Task<CatalogTourDto[]> GetPublishedTours(CancellationToken ct)
    {
        List<CatalogTourDto>? tours = null;

        await foreach (var tour in httpClient.GetFromJsonAsAsyncEnumerable<CatalogTourDto>("/public/catalog/tours", ct))
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

    public async Task<CatalogTourDto?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        var escapedSlug = Uri.EscapeDataString(slug);
        using var response = await httpClient.GetAsync(new Uri($"/public/catalog/tours/{escapedSlug}", UriKind.Relative), ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CatalogTourDto>(ct);
    }

    public async Task<PublicContentVariantDto?> GetPublicContent(string key, string? culture, CancellationToken ct)
    {
        var escapedKey = Uri.EscapeDataString(key);
        var escapedCulture = string.IsNullOrWhiteSpace(culture) ? "en-US" : Uri.EscapeDataString(culture);
        using var response = await httpClient.GetAsync(
            new Uri($"/public/catalog/content/{escapedKey}?culture={escapedCulture}", UriKind.Relative),
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PublicContentVariantDto>(ct);
    }
}
