using System.Net;
using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// HTTP client for public catalog endpoints.
/// </summary>
public sealed class PublicCatalogApiClient(HttpClient httpClient) : IPublicCatalogApiClient
{
    /// <summary>Gets the route prefix shared by public catalog requests.</summary>
    public const string RoutePrefix = "/api/v1/public/catalog";
    private static readonly PublicCatalogApiClientJsonContext Json = PublicCatalogApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<HttpOutcome<TourSummaryDto[]>> GetPublishedTours(CancellationToken ct)
    {
        var outcome = await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri($"{RoutePrefix}/tours", UriKind.Relative)),
            HttpStatusCode.OK,
            Json.TourSummaryDtoArray,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
        return outcome.Kind == HttpOutcomeKind.Succeeded
            ? outcome with
            {
                Value = outcome.Value?.OfType<TourSummaryDto>().ToArray() ?? []
            }
            : outcome;
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<TourDetailsDto>> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(slug);

        var escapedSlug = Uri.EscapeDataString(slug);
        return await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri($"{RoutePrefix}/tours/{escapedSlug}", UriKind.Relative)),
            HttpStatusCode.OK,
            Json.TourDetailsDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<PublicContentVariantDto>> GetPublicContent(
        string key,
        string? culture,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(key);

        var escapedKey = EscapePath(key);
        var escapedCulture = string.IsNullOrWhiteSpace(culture) ? "en-US" : Uri.EscapeDataString(culture);
        return await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(
                HttpMethod.Get,
                new Uri($"{RoutePrefix}/content/{escapedKey}?culture={escapedCulture}", UriKind.Relative)),
            HttpStatusCode.OK,
            Json.PublicContentVariantDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PublicMediaObjectResponse?> GetPublicMedia(Guid id, int width, string format, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        var response = await httpClient.GetAsync(
            new Uri($"{RoutePrefix}/media/{id}/{width}/{Uri.EscapeDataString(format)}", UriKind.Relative),
            HttpCompletionOption.ResponseHeadersRead,
            ct).ConfigureAwait(false);
        return await HttpResponseBody.ReadOrNotFound(response,
            static (message, content, mediaType) => new PublicMediaObjectResponse(message, content, mediaType),
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogGalleryIndexDto>> ListPublishedAlbums(string culture, string? page, CancellationToken ct) =>
        HttpOutcomeMapper.Send(httpClient, new HttpRequestMessage(HttpMethod.Get, GalleryUri("/gallery", culture, page)),
            HttpStatusCode.OK, Json.CatalogGalleryIndexDto, Json.ContractValidationProblemDto, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogGalleryAlbumDto>> GetPublishedAlbum(string slug, string culture, string? page, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return HttpOutcomeMapper.Send(httpClient, new HttpRequestMessage(HttpMethod.Get,
            GalleryUri($"/gallery/{Uri.EscapeDataString(slug)}", culture, page)),
            HttpStatusCode.OK, Json.CatalogGalleryAlbumDto, Json.ContractValidationProblemDto, ct);
    }

    private static Uri GalleryUri(string path, string culture, string? page)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);

        var pagination = page is null ? string.Empty : $"&page={Uri.EscapeDataString(page)}";
        return new Uri($"{RoutePrefix}{path}?culture={Uri.EscapeDataString(culture)}{pagination}", UriKind.Relative);
    }

    private static string EscapePath(string path)
    {
        return string.Join('/', path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.EscapeDataString));
    }
}
