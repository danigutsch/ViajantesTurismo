using System.Net;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

internal sealed class FakeCatalogToursApiClient : ICatalogToursApiClient
{
    public CatalogTourDto[] Tours { get; set; } = [];

    public bool ThrowOnGetTours { get; set; }

    public ContractValidationException? ValidationException { get; set; }

    public bool ThrowConflictOnUpdate { get; set; }

    public bool ThrowAcceptedOnUpdate { get; set; }

    public bool ThrowAcceptedOnPublication { get; set; }

    public UpsertCatalogTourPresentationRequest? LastPresentationRequest { get; private set; }

    public CatalogTourPublicationRequest? LastPublicationRequest { get; private set; }

    public bool? LastPublicationState { get; private set; }

    public IReadOnlyList<CatalogMediaImageDto> Images { get; set; } = [];

    public bool ThrowOnGetTourImages { get; set; }

    public HttpOutcome<CatalogMediaImageDto[]>? GetTourImagesOutcome { get; set; }

    public CatalogMediaImageDto? Draft { get; set; }

    public Task<HttpOutcome<CatalogMediaImageDto>>? AccessibilityDraftTask { get; set; }

    public CatalogMediaImageDto? UploadedImage { get; set; }

    public Func<Guid, CatalogTourImageUploadRequest, IProgress<long>?, CancellationToken, Task<CatalogMediaImageDto?>>? UploadTourImageHandler { get; set; }

    public Guid? LastUploadedTourId { get; private set; }

    public string? LastUploadedFileName { get; private set; }

    public string? LastUploadedContentType { get; private set; }

    public string? LastUploadedAltText { get; private set; }

    public string? LastUploadedCaption { get; private set; }

    public CancellationToken LastUploadCancellationToken { get; private set; }

    public int UploadTourImageCallCount { get; private set; }

    public CatalogMediaImageDto? AccessibilityReviewResult { get; set; }

    public bool ReturnNullOnAccessibilityReview { get; set; }

    public bool ThrowOnSubsequentGetTourImages { get; set; }

    public PublicMediaObjectResponse? Media { get; set; }

    public bool ThrowOnMediaPreview { get; set; }

    public Guid? LastMediaId { get; private set; }

    public int? LastMediaWidth { get; private set; }

    public string? LastMediaFormat { get; private set; }

    public PublicMediaImageAccessibilityReviewRequest? LastAccessibilityReviewRequest { get; private set; }

    private int getTourImagesRequests;

    public Task<HttpOutcome<CatalogTourDto[]>> GetTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(ThrowOnGetTours
            ? TransportFailure<CatalogTourDto[]>()
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, Tours));
    }

    public Task<HttpOutcome<CatalogTourDto>> GetTour(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ThrowOnGetTours)
        {
            return Task.FromResult(TransportFailure<CatalogTourDto>());
        }

        var tour = Tours.SingleOrDefault(tour => tour.Id == id);
        return Task.FromResult(tour is null
            ? HttpOutcomeMapper.Status<CatalogTourDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, tour));
    }

    public Task<HttpOutcome<CatalogTourDto>> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ValidationException is not null)
        {
            return Task.FromResult(ValidationFailure<CatalogTourDto>(ValidationException));
        }

        LastPresentationRequest = request;
        if (ThrowAcceptedOnUpdate)
        {
            return Task.FromResult(HttpOutcomeMapper.Status<CatalogTourDto>(HttpOutcomeKind.UnexpectedStatus, HttpStatusCode.Accepted));
        }

        if (ThrowConflictOnUpdate)
        {
            return Task.FromResult(HttpOutcomeMapper.Status<CatalogTourDto>(HttpOutcomeKind.Conflict, HttpStatusCode.Conflict));
        }

        var tour = Tours.SingleOrDefault(tour => tour.Id == id);
        if (tour is null)
        {
            return Task.FromResult(HttpOutcomeMapper.Status<CatalogTourDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound));
        }

        var updated = tour with
        {
            Title = request.Title,
            Slug = request.Slug,
            Summary = request.Summary,
            Description = request.Description,
            Itinerary = request.Itinerary,
            SeoTitle = request.SeoTitle,
            SeoDescription = request.SeoDescription,
            Version = tour.Version + 1
        };

        Tours = Tours.Select(current => current.Id == id ? updated : current).ToArray();
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, updated));
    }

    public Task<HttpOutcome> Publish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct)
    {
        return ChangePublication(id, request, isPublished: true, ct);
    }

    public Task<HttpOutcome> Unpublish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct)
    {
        return ChangePublication(id, request, isPublished: false, ct);
    }

    public Task<HttpOutcome<CatalogMediaImageDto>> GenerateMediaImageAccessibilityDraft(Guid id, PublicMediaImageAccessibilityDraftRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return AccessibilityDraftTask ?? Task.FromResult(Draft is null
            ? HttpOutcomeMapper.Status<CatalogMediaImageDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, Draft));
    }

    public async Task<HttpOutcome<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, IProgress<long>? progress, CancellationToken ct)
    {
        LastUploadCancellationToken = ct;
        UploadTourImageCallCount++;
        ct.ThrowIfCancellationRequested();

        LastUploadedTourId = id;
        LastUploadedFileName = request.FileName;
        LastUploadedContentType = request.ContentType;
        LastUploadedAltText = request.AltText;
        LastUploadedCaption = request.Caption;
        var uploaded = UploadTourImageHandler is null
            ? UploadedImage
            : await UploadTourImageHandler(id, request, progress, ct);
        progress?.Report(request.ContentLength);
        return uploaded is null
            ? HttpOutcomeMapper.Status<CatalogMediaImageDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.Created, uploaded);
    }

    public Task<HttpOutcome<CatalogMediaImageDto[]>> GetTourImages(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ThrowOnGetTourImages)
        {
            return Task.FromResult(TransportFailure<CatalogMediaImageDto[]>());
        }

        if (GetTourImagesOutcome is not null)
        {
            return Task.FromResult(GetTourImagesOutcome);
        }

        if (ThrowOnSubsequentGetTourImages && Interlocked.Increment(ref getTourImagesRequests) > 1)
        {
            return Task.FromResult(TransportFailure<CatalogMediaImageDto[]>());
        }

        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, Images.ToArray()));
    }

    public Task<HttpOutcome<CatalogMediaImageDto>> ReviewMediaImageAccessibility(Guid id, PublicMediaImageAccessibilityReviewRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ValidationException is not null)
        {
            return Task.FromResult(ValidationFailure<CatalogMediaImageDto>(ValidationException));
        }

        LastAccessibilityReviewRequest = request;
        if (ReturnNullOnAccessibilityReview)
        {
            return Task.FromResult(HttpOutcomeMapper.Status<CatalogMediaImageDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound));
        }

        var reviewed = AccessibilityReviewResult ?? Images.FirstOrDefault(image => image.Id == id);
        if (reviewed is not null)
        {
            Images = Images.Select(image => image.Id == id ? reviewed : image).ToArray();
        }

        return Task.FromResult(reviewed is null
            ? HttpOutcomeMapper.Status<CatalogMediaImageDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, reviewed));
    }

    public Task<PublicMediaObjectResponse?> GetMediaPreview(Guid id, int width, string format, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        LastMediaId = id;
        LastMediaWidth = width;
        LastMediaFormat = format;

        if (ThrowOnMediaPreview)
        {
            throw new HttpRequestException("Catalog unavailable.");
        }

        return Task.FromResult(Media);
    }

    private Task<HttpOutcome> ChangePublication(
        Guid id,
        CatalogTourPublicationRequest request,
        bool isPublished,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        LastPublicationRequest = request;
        LastPublicationState = isPublished;

        if (ThrowAcceptedOnPublication)
        {
            return Task.FromResult(HttpOutcomeMapper.Status(HttpOutcomeKind.UnexpectedStatus, HttpStatusCode.Accepted));
        }

        var tour = Tours.SingleOrDefault(tour => tour.Id == id);
        if (tour is not null)
        {
            var updated = tour with { IsPublished = isPublished, Version = tour.Version + 1 };
            Tours = Tours.Select(current => current.Id == id ? updated : current).ToArray();
        }

        return Task.FromResult(tour is null
            ? HttpOutcomeMapper.Status(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.NoContent, location: null));
    }

    private static HttpOutcome<TValue> TransportFailure<TValue>() => new()
    {
        Kind = HttpOutcomeKind.TransportFailure,
        Message = "HTTP transport failed."
    };

    private static HttpOutcome<TValue> ValidationFailure<TValue>(ContractValidationException exception) => new()
    {
        Kind = HttpOutcomeKind.ValidationProblem,
        StatusCode = HttpStatusCode.BadRequest,
        ValidationErrors = exception.ValidationErrors
    };

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, CancellationToken ct)
    {
        return UploadTourImage(id, request, null, ct);
    }
}
