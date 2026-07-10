using System.Net;
using System.Net.Http.Json;
using SharedKernel.HttpClients;
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
    public async Task<PublicContentDto[]> GetContent(CancellationToken ct)
    {
        List<PublicContentDto>? content = null;

        await foreach (var entry in httpClient.GetFromJsonAsAsyncEnumerable(RoutePrefix, Json.PublicContentDto, ct).ConfigureAwait(false))
        {
            if (entry is null)
            {
                continue;
            }

            content ??= [];
            content.Add(entry);
        }

        return content?.ToArray() ?? [];
    }

    /// <inheritdoc />
    public async Task<PublicContentDto?> GetContent(string key, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(key);

        var requestUri = new Uri($"{RoutePrefix}/{EscapePath(key)}", UriKind.Relative);
        using var response = await httpClient.GetAsync(requestUri, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(Json.PublicContentDto, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PublicContentDto> SaveContent(string key, UpsertPublicContentRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(request);

        var requestUri = new Uri($"{RoutePrefix}/{EscapePath(key)}", UriKind.Relative);
        using var response = await httpClient.PutAsJsonAsync(requestUri, request, Json.UpsertPublicContentRequest, ct).ConfigureAwait(false);
        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);

        var content = await response.Content.ReadFromJsonAsync(Json.PublicContentDto, ct).ConfigureAwait(false);
        return content ?? throw new InvalidOperationException("Catalog API returned an empty content response.");
    }

    private static string EscapePath(string path)
    {
        return string.Join('/', path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.EscapeDataString));
    }
}
