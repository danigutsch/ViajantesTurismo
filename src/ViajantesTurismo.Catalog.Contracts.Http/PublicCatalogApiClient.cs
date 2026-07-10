using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// HTTP client for public catalog endpoints.
/// </summary>
public sealed class PublicCatalogApiClient(HttpClient httpClient) : IPublicCatalogApiClient
{
    private static readonly PublicCatalogApiClientJsonContext Json = PublicCatalogApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<CatalogTourDto[]> GetPublishedTours(CancellationToken ct)
    {
        List<CatalogTourDto>? tours = null;

        await foreach (var tour in httpClient.GetFromJsonAsAsyncEnumerable("/public/catalog/tours", Json.CatalogTourDto, ct).ConfigureAwait(false))
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

    /// <inheritdoc />
    public async Task<CatalogTourDto?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(slug);

        var escapedSlug = Uri.EscapeDataString(slug);
        using var response = await httpClient.GetAsync(new Uri($"/public/catalog/tours/{escapedSlug}", UriKind.Relative), ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(Json.CatalogTourDto, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PublicContentVariantDto?> GetPublicContent(string key, string? culture, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(key);

        var escapedKey = EscapePath(key);
        var escapedCulture = string.IsNullOrWhiteSpace(culture) ? "en-US" : Uri.EscapeDataString(culture);
        using var response = await httpClient.GetAsync(
            new Uri($"/public/catalog/content/{escapedKey}?culture={escapedCulture}", UriKind.Relative),
            ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(Json.PublicContentVariantDto, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PublicThemeSettingsDto> GetThemeSettings(CancellationToken ct)
    {
        var theme = await httpClient.GetFromJsonAsync("/public/catalog/theme", Json.PublicThemeSettingsDto, ct).ConfigureAwait(false);
        return theme ?? throw new InvalidOperationException("Catalog returned an empty public theme response.");
    }

    private static string EscapePath(string path)
    {
        return string.Join('/', path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.EscapeDataString));
    }
}
