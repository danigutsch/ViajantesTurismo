using System.Net;
using PandaBear.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.Management.WebBrowserTests;

/// <summary>
/// Catalog tours API client for browser journeys: serves one tour, holds uploads on a gate that only
/// cancellation releases, and answers presentation saves with a configured validation problem.
/// </summary>
/// <param name="tour">The tour every read returns.</param>
internal sealed class FakeCatalogBrowserApiClient(CatalogTourDto tour) : ICatalogToursApiClient
{
    private readonly TaskCompletionSource uploadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<CancellationToken> uploadCanceled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the tour the fake serves.</summary>
    public CatalogTourDto Tour { get; } = tour;

    /// <summary>Gets or sets the field-keyed errors every presentation save answers with.</summary>
    public IReadOnlyDictionary<string, string[]> PresentationValidationErrors { get; set; } = new Dictionary<string, string[]>();

    /// <summary>Gets a task that completes once an upload is being held.</summary>
    public Task UploadStarted => uploadStarted.Task;

    /// <summary>Gets a task that completes with the upload's token once the held upload observed cancellation.</summary>
    public Task<CancellationToken> UploadCanceled => uploadCanceled.Task;

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogTourDto[]>> GetTours(CancellationToken ct) =>
        Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, new[] { Tour }));

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogTourDto>> GetTour(Guid id, CancellationToken ct) =>
        Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, Tour));

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogTourDto>> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct) =>
        Task.FromResult(new HttpOutcome<CatalogTourDto>
        {
            Kind = HttpOutcomeKind.ValidationProblem,
            StatusCode = HttpStatusCode.BadRequest,
            ValidationErrors = PresentationValidationErrors
        });

    /// <inheritdoc />
    public async Task<HttpOutcome<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, IProgress<long>? progress, CancellationToken ct)
    {
        uploadStarted.TrySetResult();
        try
        {
            await new TaskCompletionSource().Task.WaitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            uploadCanceled.TrySetResult(ct);
            throw;
        }

        throw new InvalidOperationException("The held upload is never released.");
    }

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, CancellationToken ct) =>
        UploadTourImage(id, request, null, ct);

    /// <inheritdoc />
    public Task<HttpOutcome> Publish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome> Unpublish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogMediaImageDto>> GenerateMediaImageAccessibilityDraft(Guid id, PublicMediaImageAccessibilityDraftRequest request, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogMediaImageDto[]>> GetTourImages(Guid id, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogMediaImageDto>> ReviewMediaImageAccessibility(Guid id, PublicMediaImageAccessibilityReviewRequest request, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<PublicMediaObjectResponse?> GetMediaPreview(Guid id, int width, string format, CancellationToken ct) => throw new NotSupportedException();
}
