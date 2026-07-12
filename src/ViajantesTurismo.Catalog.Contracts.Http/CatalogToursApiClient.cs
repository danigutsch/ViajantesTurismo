using System.Net;
using System.Net.Http.Json;
using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// HTTP client for catalog tour management endpoints.
/// </summary>
public sealed class CatalogToursApiClient(HttpClient httpClient) : ICatalogToursApiClient
{
    private const string RoutePrefix = "/api/v1/catalog";
    private static readonly CatalogToursApiClientJsonContext Json = CatalogToursApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<CatalogTourDto[]> GetTours(CancellationToken ct)
    {
        List<CatalogTourDto>? tours = null;

        await foreach (var tour in httpClient.GetFromJsonAsAsyncEnumerable($"{RoutePrefix}/tours", Json.CatalogTourDto, ct).ConfigureAwait(false))
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

        using var response = await httpClient.PutAsJsonAsync($"{RoutePrefix}/tours/{id}/presentation", request, Json.UpsertCatalogTourPresentationRequest, ct).ConfigureAwait(false);

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
        using var response = await httpClient.GetAsync(new Uri($"{RoutePrefix}/tours/{id}", UriKind.Relative), ct).ConfigureAwait(false);

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

        using var response = await httpClient.PostAsJsonAsync($"{RoutePrefix}/media/images/{id}/accessibility-draft", request, Json.PublicMediaImageAccessibilityDraftRequest, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync(Json.PublicMediaImageDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The media image response body was empty.");
    }

    /// <inheritdoc />
    public async Task<PublicMediaImageDto?> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var content = new MultipartFormDataContent();
        using var file = new StreamContent(request.Content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType);
        content.Add(file, "file", request.FileName);
        content.Add(new StringContent(request.AltText), "altText");
        AddOptionalFormValue(content, "caption", request.Caption);
        AddOptionalFormValue(content, "attribution", request.Attribution);
        AddOptionalFormValue(content, "copyright", request.Copyright);

        using var response = await httpClient.PostAsync(new Uri($"{RoutePrefix}/tours/{id}/images", UriKind.Relative), content, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync(Json.PublicMediaImageDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The media image response body was empty.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublicMediaImageDto>> GetTourImages(Guid id, CancellationToken ct)
    {
        var images = await httpClient.GetFromJsonAsync(new Uri($"{RoutePrefix}/tours/{id}/images", UriKind.Relative), Json.PublicMediaImageDtoArray, ct).ConfigureAwait(false);
        return images ?? [];
    }

    /// <inheritdoc />
    public async Task<PublicMediaImageDto?> ReviewMediaImageAccessibility(Guid id, PublicMediaImageAccessibilityReviewRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient.PutAsJsonAsync(new Uri($"{RoutePrefix}/media/images/{id}/accessibility-review", UriKind.Relative), request, Json.PublicMediaImageAccessibilityReviewRequest, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync(Json.PublicMediaImageDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The media image response body was empty.");
    }

    private static void AddOptionalFormValue(MultipartFormDataContent content, string name, string? value)
    {
        if (value is not null)
        {
            content.Add(new StringContent(value), name);
        }
    }
}
