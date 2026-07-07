using System.Net;
using System.Net.Http.Json;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// HTTP client for catalog tour management endpoints.
/// </summary>
public sealed class CatalogToursApiClient(HttpClient httpClient) : ICatalogToursApiClient
{
    private static readonly CatalogToursApiClientJsonContext Json = CatalogToursApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<CatalogTourDto[]> GetTours(CancellationToken ct)
    {
        List<CatalogTourDto>? tours = null;

        await foreach (var tour in httpClient.GetFromJsonAsAsyncEnumerable("/catalog/tours", Json.CatalogTourDto, ct).ConfigureAwait(false))
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
    public async Task<CatalogTourDto?> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient.PutAsJsonAsync($"/catalog/tours/{id}/presentation", request, Json.UpsertCatalogTourPresentationRequest, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync(Json.CatalogTourDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The catalog tour response body was empty.");
    }

    /// <inheritdoc />
    public async Task<CatalogTourDto?> GetTour(Guid id, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(new Uri($"/catalog/tours/{id}", UriKind.Relative), ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(Json.CatalogTourDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The catalog tour response body was empty.");
    }

    /// <inheritdoc />
    public async Task<PublicMediaImageDto?> GenerateMediaImageAccessibilityDraft(Guid id, PublicMediaImageAccessibilityDraftRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient.PostAsJsonAsync($"/catalog/media/images/{id}/accessibility-draft", request, Json.PublicMediaImageAccessibilityDraftRequest, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync(Json.PublicMediaImageDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The media image response body was empty.");
    }
}
