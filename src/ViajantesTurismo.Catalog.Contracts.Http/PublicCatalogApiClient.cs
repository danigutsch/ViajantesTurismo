using System.Net;
using PandaBear.HttpClients;
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
