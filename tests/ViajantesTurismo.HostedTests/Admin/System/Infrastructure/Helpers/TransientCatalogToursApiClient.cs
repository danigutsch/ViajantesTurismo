using System.Net;
using PandaBear.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Helpers;

/// <summary>Catalog tours client fake whose first tour list call fails and later calls return the tour; other operations report not found or succeed.</summary>
/// <param name="tour">The tour returned once the initial failure has passed.</param>
/// <param name="initialFailureStatusCode">The status code returned by the first tour list call.</param>
internal sealed class TransientCatalogToursApiClient(
    CatalogTourDto tour,
    HttpStatusCode initialFailureStatusCode = HttpStatusCode.InternalServerError) : ICatalogToursApiClient
{
    /// <summary>Gets the number of tour list calls made so far.</summary>
    public int GetToursCallCount
    {
        get; private set;
    }

    /// <summary>Fails on the first call with the configured status code, then returns the single tour.</summary>
    public Task<HttpOutcome<CatalogTourDto[]>> GetTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        GetToursCallCount++;
        return GetToursCallCount == 1
            ? Task.FromResult(HttpOutcomeMapper.Status<CatalogTourDto[]>(
                MapKind(initialFailureStatusCode),
                initialFailureStatusCode))
            : Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, new[] { tour }));
    }

    /// <summary>Always reports not found.</summary>
    public Task<HttpOutcome<CatalogTourDto>> GetTour(Guid id, CancellationToken ct)
    {
        return Task.FromResult(NotFound<CatalogTourDto>());
    }

    /// <summary>Always reports not found.</summary>
    public Task<HttpOutcome<CatalogTourDto>> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct)
    {
        return Task.FromResult(NotFound<CatalogTourDto>());
    }

    /// <summary>Succeeds with no content without changing any state.</summary>
    public Task<HttpOutcome> Publish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct)
    {
        return CompletePublication(request, isPublished: true, ct);
    }

    /// <summary>Succeeds with no content without changing any state.</summary>
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
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.NoContent, location: null));
    }

    /// <summary>Always reports not found.</summary>
    public Task<HttpOutcome<CatalogMediaImageDto>> GenerateMediaImageAccessibilityDraft(
        Guid id,
        PublicMediaImageAccessibilityDraftRequest request,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(NotFound<CatalogMediaImageDto>());
    }

    /// <summary>Always reports not found.</summary>
    public Task<HttpOutcome<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, IProgress<long>? progress, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(NotFound<CatalogMediaImageDto>());
    }

    /// <summary>Returns an empty image list.</summary>
    public Task<HttpOutcome<CatalogMediaImageDto[]>> GetTourImages(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, Array.Empty<CatalogMediaImageDto>()));
    }

    /// <summary>Always reports not found.</summary>
    public Task<HttpOutcome<CatalogMediaImageDto>> ReviewMediaImageAccessibility(
        Guid id,
        PublicMediaImageAccessibilityReviewRequest request,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(NotFound<CatalogMediaImageDto>());
    }

    /// <summary>Returns no preview.</summary>
    public Task<PublicMediaObjectResponse?> GetMediaPreview(Guid id, int width, string format, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<PublicMediaObjectResponse?>(null);
    }

    private static HttpOutcome<TValue> NotFound<TValue>() =>
        HttpOutcomeMapper.Status<TValue>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound);

    private static HttpOutcomeKind MapKind(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => HttpOutcomeKind.NotFound,
            HttpStatusCode.Unauthorized => HttpOutcomeKind.Unauthorized,
            HttpStatusCode.Forbidden => HttpOutcomeKind.Forbidden,
            HttpStatusCode.Conflict => HttpOutcomeKind.Conflict,
            _ => HttpOutcomeKind.UnexpectedStatus
        };
    }

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, CancellationToken ct)
    {
        return UploadTourImage(id, request, null, ct);
    }
}
