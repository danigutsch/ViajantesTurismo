using System.Net;
using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Helpers;

internal sealed class TransientCatalogToursApiClient(
    CatalogTourDto tour,
    HttpStatusCode initialFailureStatusCode = HttpStatusCode.InternalServerError) : ICatalogToursApiClient
{
    public int GetToursCallCount { get; private set; }

    public Task<HttpOutcome<CatalogTourDto[]>> GetTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        GetToursCallCount++;
        return GetToursCallCount == 1
            ? Task.FromResult(ContractCommandOutcome.Status<CatalogTourDto[]>(
                MapKind(initialFailureStatusCode),
                initialFailureStatusCode))
            : Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.OK, new[] { tour }));
    }

    public Task<HttpOutcome<CatalogTourDto>> GetTour(Guid id, CancellationToken ct)
    {
        return Task.FromResult(NotFound<CatalogTourDto>());
    }

    public Task<HttpOutcome<CatalogTourDto>> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct)
    {
        return Task.FromResult(NotFound<CatalogTourDto>());
    }

    public Task<HttpOutcome> Publish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct)
    {
        return CompletePublication(request, isPublished: true, ct);
    }

    public Task<HttpOutcome> Unpublish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct)
    {
        return CompletePublication(request, isPublished: false, ct);
    }

    private static Task<HttpOutcome> CompletePublication(
        CatalogTourPublicationRequest request,
        bool isPublished,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();
        _ = isPublished;
        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.NoContent, location: null));
    }

    public Task<HttpOutcome<CatalogMediaImageDto>> GenerateMediaImageAccessibilityDraft(
        Guid id,
        PublicMediaImageAccessibilityDraftRequest request,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(NotFound<CatalogMediaImageDto>());
    }

    public Task<HttpOutcome<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, IProgress<long>? progress, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(NotFound<CatalogMediaImageDto>());
    }

    public Task<HttpOutcome<CatalogMediaImageDto[]>> GetTourImages(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.OK, Array.Empty<CatalogMediaImageDto>()));
    }

    public Task<HttpOutcome<CatalogMediaImageDto>> ReviewMediaImageAccessibility(
        Guid id,
        PublicMediaImageAccessibilityReviewRequest request,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(NotFound<CatalogMediaImageDto>());
    }

    public Task<PublicMediaObjectResponse?> GetMediaPreview(Guid id, int width, string format, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<PublicMediaObjectResponse?>(null);
    }

    private static HttpOutcome<TValue> NotFound<TValue>() =>
        ContractCommandOutcome.Status<TValue>(ContractCommandOutcomeKind.NotFound, HttpStatusCode.NotFound);

    private static ContractCommandOutcomeKind MapKind(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => ContractCommandOutcomeKind.NotFound,
            HttpStatusCode.Unauthorized => ContractCommandOutcomeKind.Unauthorized,
            HttpStatusCode.Forbidden => ContractCommandOutcomeKind.Forbidden,
            HttpStatusCode.Conflict => ContractCommandOutcomeKind.Conflict,
            _ => ContractCommandOutcomeKind.UnexpectedStatus
        };
    }

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, CancellationToken ct)
    {
        return UploadTourImage(id, request, null, ct);
    }
}
