using System.Net;
using System.Net.Http.Json;
using PandaBear.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// HTTP client for public content management endpoints.
/// </summary>
public sealed class PublicContentApiClient(HttpClient httpClient) : IPublicContentApiClient
{
    private const string RoutePrefix = "/api/v1/catalog/public-content";
    private static readonly PublicContentApiClientJsonContext Json = PublicContentApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<HttpOutcome<PublicContentDto[]>> GetContent(CancellationToken ct)
    {
        var outcome = await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri(RoutePrefix, UriKind.Relative)),
            HttpStatusCode.OK,
            Json.PublicContentDtoArray,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
        return outcome.Kind == HttpOutcomeKind.Succeeded
            ? outcome with
            {
                Value = outcome.Value?.OfType<PublicContentDto>().ToArray() ?? []
            }
            : outcome;
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<PublicContentDto>> GetContent(string key, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(key);

        var requestUri = new Uri($"{RoutePrefix}/{EscapePath(key)}", UriKind.Relative);
        return await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, requestUri),
            HttpStatusCode.OK,
            Json.PublicContentDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<PublicContentDto>> SaveContent(
        string key,
        UpsertPublicContentRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(request);

        var requestUri = new Uri($"{RoutePrefix}/{EscapePath(key)}", UriKind.Relative);
        var httpRequest = new HttpRequestMessage(HttpMethod.Put, requestUri)
        {
            Content = JsonContent.Create(request, Json.UpsertPublicContentRequest)
        };
        return await HttpOutcomeMapper.Send(
            httpClient,
            httpRequest,
            HttpStatusCode.OK,
            Json.PublicContentDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    private static string EscapePath(string path)
    {
        return string.Join('/', path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.EscapeDataString));
    }
}
