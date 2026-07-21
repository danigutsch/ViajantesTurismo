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
            tours.Add(EnsureValidTour(tour));
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

        ThrowIfProjectionPending(response);
        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);
        var tour = await response.Content.ReadFromJsonAsync(Json.CatalogTourDto, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The catalog tour response body was empty.");
        return EnsureValidTour(tour);
    }

    /// <inheritdoc />
    public async Task Publish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient.PostAsJsonAsync(
            $"{RoutePrefix}/tours/{id}/publish",
            request,
            Json.CatalogTourPublicationRequest,
            ct).ConfigureAwait(false);
        ThrowIfProjectionPending(response);
        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task Unpublish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient.PostAsJsonAsync(
            $"{RoutePrefix}/tours/{id}/unpublish",
            request,
            Json.CatalogTourPublicationRequest,
            ct).ConfigureAwait(false);
        ThrowIfProjectionPending(response);
        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);
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
        var tour = await response.Content.ReadFromJsonAsync(Json.CatalogTourDto, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The catalog tour response body was empty.");
        return EnsureValidTour(tour);
    }

    /// <inheritdoc />
    public async Task<CatalogMediaImageDto?> GenerateMediaImageAccessibilityDraft(Guid id, PublicMediaImageAccessibilityDraftRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient.PostAsJsonAsync($"{RoutePrefix}/media/images/{id}/accessibility-draft", request, Json.PublicMediaImageAccessibilityDraftRequest, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync(Json.CatalogMediaImageDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The media image response body was empty.");
    }

    /// <inheritdoc />
    public async Task<CatalogMediaImageDto?> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, CancellationToken ct)
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
        return await response.Content.ReadFromJsonAsync(Json.CatalogMediaImageDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The media image response body was empty.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CatalogMediaImageDto>> GetTourImages(Guid id, CancellationToken ct)
    {
        var images = await httpClient.GetFromJsonAsync(new Uri($"{RoutePrefix}/tours/{id}/images", UriKind.Relative), Json.CatalogMediaImageDtoArray, ct).ConfigureAwait(false);
        return images ?? [];
    }

    /// <inheritdoc />
    public async Task<CatalogMediaImageDto?> ReviewMediaImageAccessibility(Guid id, PublicMediaImageAccessibilityReviewRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient.PutAsJsonAsync(new Uri($"{RoutePrefix}/media/images/{id}/accessibility-review", UriKind.Relative), request, Json.PublicMediaImageAccessibilityReviewRequest, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync(Json.CatalogMediaImageDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The media image response body was empty.");
    }

    /// <inheritdoc />
    public async Task<PublicMediaObjectResponse?> GetMediaPreview(Guid id, int width, string format, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri($"{RoutePrefix}/media/images/{id}/preview/{width}/{Uri.EscapeDataString(format)}", UriKind.Relative));
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }

        try
        {
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            return new PublicMediaObjectResponse(response, content, response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private static void AddOptionalFormValue(MultipartFormDataContent content, string name, string? value)
    {
        if (value is not null)
        {
            content.Add(new StringContent(value), name);
        }
    }

    private static CatalogTourDto EnsureValidTour(CatalogTourDto tour)
    {
        if (tour.Version < 1)
        {
            throw new InvalidOperationException("The catalog tour response contained an invalid stream version.");
        }

        return tour;
    }

    private static void ThrowIfProjectionPending(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Accepted)
        {
            throw new HttpRequestException(
                "The Catalog tour change was accepted and is waiting for projection.",
                inner: null,
                HttpStatusCode.Accepted);
        }
    }
}
