using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// HTTP client for public catalog endpoints.
/// </summary>
public sealed class PublicCatalogApiClient(HttpClient httpClient) : IPublicCatalogApiClient
{
    private const string RoutePrefix = "/api/v1/public/catalog";
    private static readonly PublicCatalogApiClientJsonContext Json = PublicCatalogApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<TourSummaryDto[]> GetPublishedTours(CancellationToken ct)
    {
        List<TourSummaryDto>? tours = null;

        await foreach (var tour in httpClient.GetFromJsonAsAsyncEnumerable($"{RoutePrefix}/tours", Json.TourSummaryDto, ct).ConfigureAwait(false))
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
    public async Task<TourDetailsDto?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(slug);

        var escapedSlug = Uri.EscapeDataString(slug);
        using var response = await httpClient.GetAsync(new Uri($"{RoutePrefix}/tours/{escapedSlug}", UriKind.Relative), ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(Json.TourDetailsDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The published tour response body was empty.");
    }

    /// <inheritdoc />
    public async Task<PublicContentVariantDto?> GetPublicContent(string key, string? culture, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(key);

        var escapedKey = EscapePath(key);
        var escapedCulture = string.IsNullOrWhiteSpace(culture) ? "en-US" : Uri.EscapeDataString(culture);
        using var response = await httpClient.GetAsync(
            new Uri($"{RoutePrefix}/content/{escapedKey}?culture={escapedCulture}", UriKind.Relative),
            ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(Json.PublicContentVariantDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The public content response body was empty.");
    }

    /// <inheritdoc />
    public async Task<PublicMediaObjectResponse?> GetPublicMedia(Guid id, int width, string format, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        var response = await httpClient.GetAsync(
            new Uri($"{RoutePrefix}/media/{id}/{width}/{Uri.EscapeDataString(format)}", UriKind.Relative),
            HttpCompletionOption.ResponseHeadersRead,
            ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            response.Dispose();
            throw;
        }

        try
        {
            var content = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            return new PublicMediaObjectResponse(response, content, response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private static string EscapePath(string path)
    {
        return string.Join('/', path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.EscapeDataString));
    }

}
