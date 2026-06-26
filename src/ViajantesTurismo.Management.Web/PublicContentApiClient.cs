using System.Net;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.Web;

internal sealed class PublicContentApiClient(HttpClient httpClient) : IPublicContentApiClient
{
    public async Task<PublicContentDto[]> GetContent(CancellationToken ct)
    {
        List<PublicContentDto>? content = null;

        await foreach (var entry in httpClient.GetFromJsonAsAsyncEnumerable<PublicContentDto>("/catalog/public-content", ct))
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

    public async Task<PublicContentDto?> GetContent(string key, CancellationToken ct)
    {
        var requestUri = new Uri($"/catalog/public-content/{Uri.EscapeDataString(key)}", UriKind.Relative);
        using var response = await httpClient.GetAsync(requestUri, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PublicContentDto>(ct);
    }

    public async Task<PublicContentDto> SaveContent(string key, UpsertPublicContentRequest request, CancellationToken ct)
    {
        var requestUri = new Uri($"/catalog/public-content/{Uri.EscapeDataString(key)}", UriKind.Relative);
        using var response = await httpClient.PutAsJsonAsync(requestUri, request, ct);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);

        var content = await response.Content.ReadFromJsonAsync<PublicContentDto>(ct);
        return content ?? throw new InvalidOperationException("Catalog API returned an empty content response.");
    }
}
