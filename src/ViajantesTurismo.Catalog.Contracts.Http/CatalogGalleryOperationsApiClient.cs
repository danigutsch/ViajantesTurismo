using System.Globalization;
using System.Net;
using System.Text.Json;
using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>Sends bounded Gallery operations reads to the Catalog management API.</summary>
public sealed class CatalogGalleryOperationsApiClient(HttpClient httpClient) : ICatalogGalleryOperationsApiClient
{
    private const string GalleryRoute = "/api/v1/catalog/operations/gallery";

    private static readonly CatalogToursApiClientJsonContext PlacementJson = new(
        new JsonSerializerOptions(JsonSerializerDefaults.Web) { RespectRequiredConstructorParameters = true });

    /// <inheritdoc />
    public Task<HttpOutcome<GetCatalogGalleryPlacementStatusDto>> Read(Guid catalogTourId, long sequence, CancellationToken ct) =>
        Read($"{GalleryRoute}/{catalogTourId:D}/placements/{sequence.ToString(CultureInfo.InvariantCulture)}",
            placement => placement.CatalogTourId == catalogTourId && placement.Sequence == sequence, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetCatalogGalleryPlacementStatusDto>> ReadByOriginalWake(Guid eventId, CancellationToken ct) =>
        Read($"{GalleryRoute}/original-wakes/{eventId:D}/placement", placement => placement.IntentId == eventId, ct);

    /// <inheritdoc />
    public async Task<HttpOutcome<GetCatalogGalleryRetryHealthDto>> GetRetryHealth(CancellationToken ct)
    {
        var uri = new Uri($"{GalleryRoute}/retry-health", UriKind.Relative);
        var outcome = await HttpOutcomeMapper.Send(httpClient, new HttpRequestMessage(HttpMethod.Get, uri),
            HttpStatusCode.OK, PlacementJson.GetCatalogGalleryRetryHealthDto,
            CatalogToursApiClientJsonContext.Default.ContractValidationProblemDto, ct).ConfigureAwait(false);
        if (outcome.Kind != HttpOutcomeKind.Succeeded)
        {
            return outcome;
        }

        return outcome.Value is { DuePlacementCount: >= 0 } health
            && (health.DuePlacementCount > 0) == health.OldestDueAt.HasValue
            ? outcome
            : HttpOutcomeMapper.Status<GetCatalogGalleryRetryHealthDto>(
                HttpOutcomeKind.MalformedBody, HttpStatusCode.OK, "Catalog Gallery response contained invalid retry health metadata.");
    }

    private async Task<HttpOutcome<GetCatalogGalleryPlacementStatusDto>> Read(
        string path, Func<GetCatalogGalleryPlacementStatusDto, bool> matchesRequest, CancellationToken ct)
    {
        var uri = new Uri(path, UriKind.Relative);
        var outcome = await HttpOutcomeMapper.Send(httpClient, new HttpRequestMessage(HttpMethod.Get, uri),
            HttpStatusCode.OK, PlacementJson.GetCatalogGalleryPlacementStatusDto,
            CatalogToursApiClientJsonContext.Default.ContractValidationProblemDto, ct).ConfigureAwait(false);
        if (outcome.Kind != HttpOutcomeKind.Succeeded)
        {
            return outcome;
        }

        return outcome.Value is { } placement
            && placement.CatalogTourId != Guid.Empty
            && placement.Sequence > 0
            && placement.IntentId != Guid.Empty
            && Enum.IsDefined(placement.Status)
            && matchesRequest(placement)
            ? outcome
            : HttpOutcomeMapper.Status<GetCatalogGalleryPlacementStatusDto>(
                HttpOutcomeKind.MalformedBody, HttpStatusCode.OK, "Catalog Gallery response contained invalid placement metadata.");
    }
}
