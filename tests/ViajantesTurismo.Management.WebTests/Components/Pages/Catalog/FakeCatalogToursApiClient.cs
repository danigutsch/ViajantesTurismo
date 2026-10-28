using System.Net;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

/// <summary>Configurable in-memory fake of the catalog tours API client that records requests for page tests.</summary>
internal sealed class FakeCatalogToursApiClient : ICatalogToursApiClient
{
    /// <summary>Tours returned by reads and mutated by presentation and publication updates.</summary>
    public CatalogTourDto[] Tours { get; set; } = [];

    /// <summary>When true, tour reads return a transport failure.</summary>
    public bool ThrowOnGetTours
    {
        get; set;
    }

    /// <summary>When set, presentation updates and accessibility reviews return a validation problem built from it.</summary>
    public ContractValidationException? ValidationException
    {
        get; set;
    }

    /// <summary>When set, publish and unpublish return a validation problem built from it.</summary>
    public ContractValidationException? PublicationValidationException
    {
        get; set;
    }

    /// <summary>When set, image uploads return a validation problem carrying these server-supplied errors.</summary>
    public IReadOnlyDictionary<string, string[]>? UploadValidationErrors
    {
        get; set;
    }

    /// <summary>When true, presentation updates return a conflict outcome.</summary>
    public bool ThrowConflictOnUpdate
    {
        get; set;
    }

    /// <summary>When true, presentation updates return a transport failure outcome.</summary>
    public bool FailTransportOnUpdate
    {
        get; set;
    }

    /// <summary>When true, publish and unpublish return a transport failure outcome.</summary>
    public bool FailTransportOnPublication
    {
        get; set;
    }

    /// <summary>When true, presentation updates return an unexpected 202 Accepted outcome.</summary>
    public bool ThrowAcceptedOnUpdate
    {
        get; set;
    }

    /// <summary>When true, publish and unpublish return an unexpected 202 Accepted outcome.</summary>
    public bool ThrowAcceptedOnPublication
    {
        get; set;
    }

    /// <summary>Request received by the latest presentation update.</summary>
    public UpsertCatalogTourPresentationRequest? LastPresentationRequest
    {
        get; private set;
    }

    /// <summary>Request received by the latest publish or unpublish call.</summary>
    public CatalogTourPublicationRequest? LastPublicationRequest
    {
        get; private set;
    }

    /// <summary>Publication state requested by the latest publish (true) or unpublish (false) call.</summary>
    public bool? LastPublicationState
    {
        get; private set;
    }

    /// <summary>Images returned by tour image reads and replaced by accessibility reviews.</summary>
    public IReadOnlyList<CatalogMediaImageDto> Images { get; set; } = [];

    /// <summary>When true, tour image reads return a transport failure.</summary>
    public bool ThrowOnGetTourImages
    {
        get; set;
    }

    /// <summary>When set, tour image reads return this outcome instead of <see cref="Images"/>.</summary>
    public HttpOutcome<CatalogMediaImageDto[]>? GetTourImagesOutcome
    {
        get; set;
    }

    /// <summary>Image returned by accessibility draft generation, or not found when null.</summary>
    public CatalogMediaImageDto? Draft
    {
        get; set;
    }

    /// <summary>When set, accessibility draft generation returns this task instead of a computed outcome.</summary>
    public Task<HttpOutcome<CatalogMediaImageDto>>? AccessibilityDraftTask
    {
        get; set;
    }

    /// <summary>Image returned by an upload when no upload handler is set; null yields not found.</summary>
    public CatalogMediaImageDto? UploadedImage
    {
        get; set;
    }

    /// <summary>Custom upload behavior that overrides <see cref="UploadedImage"/> when set.</summary>
    public Func<Guid, CatalogTourImageUploadRequest, IProgress<long>?, CancellationToken, Task<CatalogMediaImageDto?>>? UploadTourImageHandler
    {
        get; set;
    }

    /// <summary>Tour id of the latest upload.</summary>
    public Guid? LastUploadedTourId
    {
        get; private set;
    }

    /// <summary>File name of the latest upload.</summary>
    public string? LastUploadedFileName
    {
        get; private set;
    }

    /// <summary>Content type of the latest upload.</summary>
    public string? LastUploadedContentType
    {
        get; private set;
    }

    /// <summary>Alt text of the latest upload.</summary>
    public string? LastUploadedAltText
    {
        get; private set;
    }

    /// <summary>Caption of the latest upload.</summary>
    public string? LastUploadedCaption
    {
        get; private set;
    }

    /// <summary>Cancellation token passed to the latest upload.</summary>
    public CancellationToken LastUploadCancellationToken
    {
        get; private set;
    }

    /// <summary>Number of upload calls received.</summary>
    public int UploadTourImageCallCount
    {
        get; private set;
    }

    /// <summary>Image returned by an accessibility review; falls back to the matching entry in <see cref="Images"/> when null.</summary>
    public CatalogMediaImageDto? AccessibilityReviewResult
    {
        get; set;
    }

    /// <summary>When true, accessibility reviews return not found.</summary>
    public bool ReturnNullOnAccessibilityReview
    {
        get; set;
    }

    /// <summary>When true, tour image reads after the first return a transport failure.</summary>
    public bool ThrowOnSubsequentGetTourImages
    {
        get; set;
    }

    /// <summary>Media object returned by preview requests.</summary>
    public PublicMediaObjectResponse? Media
    {
        get; set;
    }

    /// <summary>When true, media preview requests throw <see cref="HttpRequestException"/>.</summary>
    public bool ThrowOnMediaPreview
    {
        get; set;
    }

    /// <summary>Media id of the latest preview request.</summary>
    public Guid? LastMediaId
    {
        get; private set;
    }

    /// <summary>Width of the latest preview request.</summary>
    public int? LastMediaWidth
    {
        get; private set;
    }

    /// <summary>Format of the latest preview request.</summary>
    public string? LastMediaFormat
    {
        get; private set;
    }

    /// <summary>Request received by the latest accessibility review.</summary>
    public PublicMediaImageAccessibilityReviewRequest? LastAccessibilityReviewRequest
    {
        get; private set;
    }

    private int getTourImagesRequests;

    /// <summary>Returns <see cref="Tours"/>, or a transport failure when configured.</summary>
    public Task<HttpOutcome<CatalogTourDto[]>> GetTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(ThrowOnGetTours
            ? TransportFailure<CatalogTourDto[]>()
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, Tours));
    }

    /// <summary>Returns the tour with the given id, not found if absent, or a transport failure when configured.</summary>
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

    /// <summary>Records the request, applies the configured failure or updates the stored tour's presentation fields and version.</summary>
    public Task<HttpOutcome<CatalogTourDto>> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ValidationException is not null)
        {
            return Task.FromResult(ValidationFailure<CatalogTourDto>(ValidationException));
        }

        LastPresentationRequest = request;
        if (FailTransportOnUpdate)
        {
            return Task.FromResult(TransportFailure<CatalogTourDto>());
        }

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

        Tours = [.. Tours.Select(current => current.Id == id ? updated : current)];
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, updated));
    }

    /// <summary>Marks the tour published, recording the request.</summary>
    public Task<HttpOutcome> Publish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct)
    {
        return ChangePublication(id, request, isPublished: true, ct);
    }

    /// <summary>Marks the tour unpublished, recording the request.</summary>
    public Task<HttpOutcome> Unpublish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct)
    {
        return ChangePublication(id, request, isPublished: false, ct);
    }

    /// <summary>Returns the configured accessibility draft task or image, or not found when none is set.</summary>
    public Task<HttpOutcome<CatalogMediaImageDto>> GenerateMediaImageAccessibilityDraft(Guid id, PublicMediaImageAccessibilityDraftRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return AccessibilityDraftTask ?? Task.FromResult(Draft is null
            ? HttpOutcomeMapper.Status<CatalogMediaImageDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, Draft));
    }

    /// <summary>Records the upload, runs the configured handler, reports full progress and returns the uploaded image or not found.</summary>
    public async Task<HttpOutcome<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, IProgress<long>? progress, CancellationToken ct)
    {
        LastUploadCancellationToken = ct;
        UploadTourImageCallCount++;
        ct.ThrowIfCancellationRequested();

        if (UploadValidationErrors is not null)
        {
            return new HttpOutcome<CatalogMediaImageDto>
            {
                Kind = HttpOutcomeKind.ValidationProblem,
                StatusCode = HttpStatusCode.BadRequest,
                ValidationErrors = UploadValidationErrors
            };
        }

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

    /// <summary>Returns the configured tour images outcome, a transport failure, or <see cref="Images"/>.</summary>
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

    /// <summary>Records the review request and returns the reviewed image, replacing it in <see cref="Images"/>, or not found.</summary>
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
            Images = [.. Images.Select(image => image.Id == id ? reviewed : image)];
        }

        return Task.FromResult(reviewed is null
            ? HttpOutcomeMapper.Status<CatalogMediaImageDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, reviewed));
    }

    /// <summary>Records the preview parameters, throws when configured, otherwise returns <see cref="Media"/>.</summary>
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

        if (PublicationValidationException is not null)
        {
            return Task.FromResult(new HttpOutcome
            {
                Kind = HttpOutcomeKind.ValidationProblem,
                StatusCode = HttpStatusCode.BadRequest,
                ValidationErrors = PublicationValidationException.ValidationErrors
            });
        }

        if (FailTransportOnPublication)
        {
            return Task.FromResult(new HttpOutcome
            {
                Kind = HttpOutcomeKind.TransportFailure,
                Message = "HTTP transport failed."
            });
        }

        if (ThrowAcceptedOnPublication)
        {
            return Task.FromResult(HttpOutcomeMapper.Status(HttpOutcomeKind.UnexpectedStatus, HttpStatusCode.Accepted));
        }

        var tour = Tours.SingleOrDefault(tour => tour.Id == id);
        if (tour is not null)
        {
            var updated = tour with
            {
                IsPublished = isPublished,
                Version = tour.Version + 1
            };
            Tours = [.. Tours.Select(current => current.Id == id ? updated : current)];
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
