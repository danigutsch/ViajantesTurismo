using System.Globalization;
using System.Net;
using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>Sends versioned requests to the Catalog gallery management endpoints.</summary>
public sealed class CatalogGalleryEditorApiClient(HttpClient httpClient) : ICatalogGalleryEditorApiClient
{
    private static readonly CatalogToursApiClientJsonContext Json = CatalogToursApiClientJsonContext.Default;

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogGalleryEditorStateDto>> GetState(Guid tourId, long? placementSequence, CancellationToken ct)
    {
        var query = placementSequence is { } sequence
            ? $"?placementSequence={sequence.ToString(CultureInfo.InvariantCulture)}"
            : string.Empty;
        return HttpOutcomeMapper.Send(httpClient, new HttpRequestMessage(HttpMethod.Get, EditorUri(tourId, query)),
            HttpStatusCode.OK, Json.CatalogGalleryEditorStateDto, Json.ContractValidationProblemDto, ct);
    }

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogGalleryPhotoOptionsDto>> SearchPhotos(
        Guid tourId, CatalogGalleryPhotoOptionsRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var message = new HttpRequestMessage(HttpMethod.Post, EditorUri(tourId, "/options"))
        {
            Content = JsonContent.Create(request, Json.CatalogGalleryPhotoOptionsRequest)
        };
        return HttpOutcomeMapper.Send(httpClient, message, HttpStatusCode.OK, Json.CatalogGalleryPhotoOptionsDto, Json.ContractValidationProblemDto, ct);
    }

    /// <inheritdoc />
    public Task<HttpOutcome> Initialize(Guid tourId, CancellationToken ct)
    {
        return HttpOutcomeMapper.Send(httpClient, new HttpRequestMessage(HttpMethod.Post, EditorUri(tourId, "/initialize")),
            HttpStatusCode.NoContent, Json.ContractValidationProblemDto, ct);
    }

    /// <inheritdoc />
    public Task<HttpOutcome> QueuePlacement(Guid tourId, QueueCatalogGalleryPlacementRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var message = new HttpRequestMessage(HttpMethod.Put, EditorUri(tourId))
        {
            Content = JsonContent.Create(request, Json.QueueCatalogGalleryPlacementRequest)
        };
        return HttpOutcomeMapper.Send(httpClient, message, HttpStatusCode.Accepted, Json.ContractValidationProblemDto, ct);
    }

    private static Uri EditorUri(Guid tourId, string suffix = "") => new($"/api/v1/catalog/tours/{tourId:D}/gallery-editor{suffix}", UriKind.Relative);
}
