using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using PandaBear.BuildingBlocks;
using PandaBear.EventSourcing;
using PandaBear.ApiVersioning.AspNetCore;
using PandaBear.HttpCaching.AspNetCore;
using PandaBear.Results;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.ApiService;

internal static class CatalogEndpoints
{

    internal static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var versionedApi = app.MapApiVersionGroup(CatalogOpenApiDocuments.CurrentApiVersion);
        var managementCatalog = versionedApi.MapGroup("/catalog").WithNoStoreResponses();

        managementCatalog.MapGet("/tours", GetTours)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapGet("/tours/{id:guid}", GetTour)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapPut("/tours/{id:guid}/presentation", UpsertTourPresentation)
            .Produces<CatalogTourDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                AddAcceptedLocationHeader(operation);
                return Task.CompletedTask;
            })
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(CatalogAuthorization.CatalogWrite);
        managementCatalog.MapPost("/tours/{id:guid}/publish", PublishTour)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                AddAcceptedLocationHeader(operation);
                return Task.CompletedTask;
            })
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(CatalogAuthorization.CatalogPublish);
        managementCatalog.MapPost("/tours/{id:guid}/unpublish", UnpublishTour)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                AddAcceptedLocationHeader(operation);
                return Task.CompletedTask;
            })
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(CatalogAuthorization.CatalogPublish);
        managementCatalog.MapGet("/tours/{id:guid}/images", ListTourImages)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapGet("/media/images/{id:guid}/preview/{width:int}/{format}", GetManagementMediaPreview)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapPost("/tours/{id:guid}/images", UploadTourImage)
            .Accepts<MediaImageUploadFormDto>("multipart/form-data")
            .Produces<CatalogMediaImageDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .RemoveInferredResponseMetadata(StatusCodes.Status404NotFound)
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                var content = operation.RequestBody?.Content;
                if (content is not null && content.TryGetValue("multipart/form-data", out var mediaType)
                    && mediaType.Schema is OpenApiSchema schema)
                {
                    schema.Required ??= new HashSet<string>(StringComparer.Ordinal);
                    schema.Required.Add("file");
                    schema.Required.Add("altText");
                }

                return Task.CompletedTask;
            })
            .WithMetadata(new RequestSizeLimitAttribute(MediaUploadValidationOptions.DefaultMaxLengthBytes + 16_384))
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .DisableAntiforgery()
            .RequireAuthorization(CatalogAuthorization.CatalogWrite);
        managementCatalog.MapPost("/media/images/{id:guid}/accessibility-draft", GenerateMediaImageAccessibilityDraft)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .RemoveInferredResponseMetadata(StatusCodes.Status404NotFound)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(CatalogAuthorization.MediaAi);
        managementCatalog.MapPut("/media/images/{id:guid}/accessibility-review", ReviewMediaImageAccessibility)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .RemoveInferredResponseMetadata(StatusCodes.Status404NotFound)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(CatalogAuthorization.CatalogWrite);

        versionedApi.MapGet("/public/catalog/tours", GetPublishedTours)
            .CacheOutput(policy => policy.Expire(PublicCatalogHttpCache.Freshness).Tag(PublicCatalogHttpCache.Tag))
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy)
            .AllowAnonymous();
        versionedApi.MapGet("/public/catalog/tours/{slug}", GetPublishedTour)
            .Produces<TourDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .CacheOutput(policy => policy.Expire(PublicCatalogHttpCache.Freshness).Tag(PublicCatalogHttpCache.Tag))
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy)
            .AllowAnonymous();
        versionedApi.MapGet("/public/catalog/media/{id:guid}/{width:int}/{format}", GetPublicMedia)
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy)
            .AllowAnonymous();
        versionedApi.MapGet("/public/catalog/content/{**key}", GetPublicContent)
            .CacheOutput(policy => policy.Expire(PublicContentHttpCache.Freshness).SetVaryByQuery(PublicContentHttpCache.CultureQueryKey).Tag(PublicContentHttpCache.Tag))
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy)
            .AllowAnonymous();
        managementCatalog.MapGet("/public-content", async (IPublicContentStore store, CancellationToken ct) =>
        {
            var content = await store.ListContent(ct);
            return content.Select(MapPublicContent);
        }).RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapGet("/public-content/{**key}", GetPublicContentForManagement)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapPut("/public-content/{**key}", UpsertPublicContent)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(CatalogAuthorization.CatalogWrite);

        return app;
    }

    private static async Task<IResult> GetTour(Guid id, ICatalogTourReadModelStore store, IPublicMediaImageStore imageStore, CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return Results.BadRequest();
        }

        var tour = await store.GetTour(id, ct);
        if (tour is null)
        {
            return Results.NotFound();
        }

        var images = await imageStore.ListByTour(id, ct);
        return Results.Ok(MapManagementTour(tour, images));
    }

    private static async Task<IResult> GetPublishedTour(string slug, ICatalogTourReadModelStore store, IPublicMediaImageStore imageStore, HttpContext httpContext, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Results.BadRequest();
        }

        var tour = await store.GetPublishedTourBySlug(slug, ct);
        if (tour is null)
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.NotFound();
        }

        var images = await imageStore.ListByTour(tour.CatalogTourId, ct);
        var dto = MapPublicTourDetail(tour, GetReadyImages(images));
        PublicCatalogHttpCache.SetPublicHeaders(httpContext);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetPublicContent(
        string key,
        string? language,
        string? culture,
        IPublicContentStore store,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.BadRequest();
        }

        if (!TryGetPublicContentLanguage(language, culture, out var requestedLanguage))
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.BadRequest();
        }

        var content = await store.GetContent(key, ct);
        if (content is null || !content.IsPubliclyVisible)
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.NotFound();
        }

        var variant = content.FindPublicVariant(requestedLanguage);
        if (variant is null)
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.NotFound();
        }

        var dto = MapVariant(variant);
        PublicContentHttpCache.SetPublicHeaders(httpContext);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetPublicMedia(
        Guid id,
        int width,
        string format,
        ICatalogTourReadModelStore tourStore,
        IPublicMediaImageStore imageStore,
        IMediaObjectStore objectStore,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.NotFound();
        }

        var contentType = GetMediaContentType(format);
        if (width <= 0 || contentType is null)
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.NotFound();
        }

        var image = await imageStore.GetImage(id, ct).ConfigureAwait(false);
        if (image is null || !CanDeliverPublicImage(image) || !await IsLinkedToPublishedTour(image, tourStore, ct).ConfigureAwait(false))
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.NotFound();
        }

        var variant = image.FindResponsiveVariant(width, contentType);
        if (variant is null)
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.NotFound();
        }

        if (!await objectStore.Exists(variant.ObjectKey, ct).ConfigureAwait(false))
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.NotFound();
        }

        var media = await TryOpenMediaObject(objectStore, variant.ObjectKey, ct).ConfigureAwait(false);
        if (media is null)
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.NotFound();
        }

        if (!string.Equals(media.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
        {
            media.Dispose();
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.NotFound();
        }

        HttpCacheHeaders.SetNoStore(httpContext);
        httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
        httpContext.Response.RegisterForDispose(media);
        return Results.Stream(media.Content, contentType, enableRangeProcessing: false);
    }

    private static async Task<IResult> GetManagementMediaPreview(
        Guid id,
        int width,
        string format,
        IPublicMediaImageStore imageStore,
        IMediaObjectStore objectStore,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return Results.BadRequest();
        }

        var contentType = GetMediaContentType(format);
        if (width <= 0 || contentType is null)
        {
            return Results.NotFound();
        }

        var image = await imageStore.GetImage(id, ct).ConfigureAwait(false);
        if (image is null)
        {
            return Results.NotFound();
        }

        var variant = image.FindResponsiveVariant(width, contentType);
        if (variant is null || !await objectStore.Exists(variant.ObjectKey, ct).ConfigureAwait(false))
        {
            return Results.NotFound();
        }

        var media = await TryOpenMediaObject(objectStore, variant.ObjectKey, ct).ConfigureAwait(false);
        if (media is null)
        {
            return Results.NotFound();
        }

        if (!string.Equals(media.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
        {
            media.Dispose();
            return Results.NotFound();
        }

        httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
        httpContext.Response.RegisterForDispose(media);
        return Results.Stream(media.Content, contentType, enableRangeProcessing: false);
    }

    private static async Task<IResult> GetPublicContentForManagement(string key, IPublicContentStore store, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Results.BadRequest();
        }

        var content = await store.GetContent(key, ct);
        return content is null ? Results.NotFound() : Results.Ok(MapPublicContent(content));
    }

    private static async Task<IReadOnlyList<CatalogTourDto>> GetTours(
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        CancellationToken ct)
    {
        var tours = await store.ListTours(ct);
        var imagesByTour = await imageStore.ListByTours([.. tours.Select(tour => tour.CatalogTourId)], ct);

        return
        [
            .. tours.Select(tour => MapManagementTour(tour, GetImages(imagesByTour, tour.CatalogTourId)))
        ];
    }

    private static async Task<IReadOnlyList<TourSummaryDto>> GetPublishedTours(
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tours = await store.ListTours(ct);
        var publishedTours = tours.Where(tour => tour.IsPubliclyVisible).ToArray();
        var imagesByTour = await imageStore.ListByTours([.. publishedTours.Select(tour => tour.CatalogTourId)], ct);

        TourSummaryDto[] result =
        [
            .. publishedTours.Select(tour => MapPublicTourSummary(tour, GetReadyImages(GetImages(imagesByTour, tour.CatalogTourId))))
        ];
        PublicCatalogHttpCache.SetPublicHeaders(httpContext);
        return result;
    }

    private static async Task<Results<Ok<PublicContentDto>, BadRequest, ValidationProblem>> UpsertPublicContent(
        string key,
        UpsertPublicContentRequest request,
        PublicContentUpsertService service,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return TypedResults.BadRequest();
        }

        var content = await service.Upsert(key, request, ct);

        if (content.IsFailure)
        {
            return ToValidationProblem(content.ErrorDetails ?? throw new InvalidOperationException("Public content validation errors must include validation details."));
        }

        await InvalidatePublicContentCache(outputCacheStore, logger);
        return TypedResults.Ok(MapPublicContent(content.Value));
    }

    private static async Task<Results<Ok<CatalogTourDto>, Accepted, BadRequest, ValidationProblem, NotFound, ProblemHttpResult>> UpsertTourPresentation(
        Guid id,
        UpsertCatalogTourPresentationRequest request,
        CatalogTourPresentationService presentationService,
        IPublicMediaImageStore imageStore,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return TypedResults.BadRequest();
        }

        if (ValidateExpectedVersion(request.ExpectedVersion) is { } versionValidationProblem)
        {
            return versionValidationProblem;
        }

        var presentation = CatalogTourPresentationUpdate.Create(
            request.Title,
            request.Slug,
            request.Summary,
            request.Description,
            request.Itinerary,
            request.SeoTitle,
            request.SeoDescription);
        if (presentation.IsFailure)
        {
            return ToValidationProblem(presentation.ErrorDetails);
        }

        var images = await imageStore.ListByTour(id, ct);
        CatalogTourDraftReadModel? updated;
        try
        {
            updated = await presentationService.UpdatePresentation(
                id,
                presentation.Value,
                request.ExpectedVersion,
                ct);
        }
        catch (CatalogTourSlugConflictException)
        {
            return TypedResults.Problem("The public tour slug is already in use.", statusCode: StatusCodes.Status409Conflict);
        }
        catch (CatalogTourPublishedPresentationChangeException)
        {
            return TypedResults.Problem(
                "Published tours must be unpublished before their presentation can change.",
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (ExpectedStreamRevisionConflictException)
        {
            return TypedResults.Problem("The tour changed while this edit was in progress. Reload it and try again.", statusCode: StatusCodes.Status409Conflict);
        }
        catch (CatalogTourProjectionPendingException exception)
        {
            logger.TourProjectionPending(exception.GetType().Name);
            return await AcceptPendingProjection(id, outputCacheStore, logger);
        }

        if (updated is null)
        {
            return TypedResults.NotFound();
        }

        await InvalidatePublicCatalogCache(outputCacheStore, logger);
        return TypedResults.Ok(MapManagementTour(updated, (IReadOnlyList<PublicMediaImage>?)images));
    }

    private static async Task<Results<NoContent, Accepted, BadRequest, ValidationProblem, NotFound, ProblemHttpResult>> PublishTour(
        Guid id,
        CatalogTourPublicationRequest request,
        CatalogTourPresentationService presentationService,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return TypedResults.BadRequest();
        }

        if (ValidateExpectedVersion(request.ExpectedVersion) is { } versionValidationProblem)
        {
            return versionValidationProblem;
        }

        try
        {
            if (await presentationService.Publish(id, request.ExpectedVersion, ct).ConfigureAwait(false) is null)
            {
                return TypedResults.NotFound();
            }
        }
        catch (CatalogTourPublicationNotReadyException)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(CatalogTourDto.Summary)] = ["Catalog tours require a title, summary, and slug before publication."]
            });
        }
        catch (ExpectedStreamRevisionConflictException)
        {
            return TypedResults.Problem("The tour changed while publication was in progress. Reload it and try again.", statusCode: StatusCodes.Status409Conflict);
        }
        catch (CatalogTourProjectionPendingException exception)
        {
            logger.TourProjectionPending(exception.GetType().Name);
            return await AcceptPendingProjection(id, outputCacheStore, logger);
        }

        await InvalidatePublicCatalogCache(outputCacheStore, logger);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, Accepted, BadRequest, ValidationProblem, NotFound, ProblemHttpResult>> UnpublishTour(
        Guid id,
        CatalogTourPublicationRequest request,
        CatalogTourPresentationService presentationService,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return TypedResults.BadRequest();
        }

        if (ValidateExpectedVersion(request.ExpectedVersion) is { } versionValidationProblem)
        {
            return versionValidationProblem;
        }

        try
        {
            if (await presentationService.Unpublish(id, request.ExpectedVersion, ct).ConfigureAwait(false) is null)
            {
                return TypedResults.NotFound();
            }
        }
        catch (ExpectedStreamRevisionConflictException)
        {
            return TypedResults.Problem("The tour changed while unpublication was in progress. Reload it and try again.", statusCode: StatusCodes.Status409Conflict);
        }
        catch (CatalogTourProjectionPendingException exception)
        {
            logger.TourProjectionPending(exception.GetType().Name);
            return await AcceptPendingProjection(id, outputCacheStore, logger);
        }

        await InvalidatePublicCatalogCache(outputCacheStore, logger);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListTourImages(
        Guid id,
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return Results.BadRequest();
        }

        var tour = await store.GetTour(id, ct);
        if (tour is null)
        {
            return Results.NotFound();
        }

        var images = await imageStore.ListByTour(id, ct);
        return Results.Ok(images.Select(MapManagementMediaImage).ToArray());
    }

    private static async Task<Results<Created<CatalogMediaImageDto>, BadRequest, NotFound, ValidationProblem, ProblemHttpResult, ForbidHttpResult>> UploadTourImage(
        Guid id,
        [AsParameters] MediaImageUploadFormDto form,
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        MediaImageUploadIntake intake,
        CatalogGalleryMutationGuard galleryGuard,
        IAuthorizationService authorization,
        HttpContext context,
        CancellationToken ct)
    {
        var altText = form.AltText;
        if (id == Guid.Empty || form.File is null || string.IsNullOrWhiteSpace(altText))
        {
            return TypedResults.BadRequest();
        }

        if (await store.GetTour(id, ct).ConfigureAwait(false) is null)
        {
            return TypedResults.NotFound();
        }

        var canPublish = (await authorization.AuthorizeAsync(context.User, CatalogAuthorization.CatalogPublish).ConfigureAwait(false)).Succeeded;
        var permission = await galleryGuard.Acquire([id], canPublish, ct).ConfigureAwait(false);
        if (permission.IsFailure)
        {
            return TypedResults.Forbid();
        }

        await using var mutationLease = permission.Value;
        var existingImages = await imageStore.ListByTour(id, ct).ConfigureAwait(false);

        await using var content = form.File.OpenReadStream();
        var result = await intake.Accept(
            new MediaImageUploadIntakeRequest(
                Guid.CreateVersion7(),
                content,
                form.File.FileName,
                form.File.ContentType,
                form.File.Length,
                altText,
                [new MediaImageTourLink(id, existingImages.Count, existingImages.Count == 0)],
                Caption: form.Caption,
                Attribution: form.Attribution,
                Copyright: form.Copyright),
            ct).ConfigureAwait(false);

        return result.Status switch
        {
            ResultStatus.Ok => TypedResults.Created($"/catalog/media/images/{result.Value.Image.Id}", MapManagementMediaImage(result.Value.Image)),
            ResultStatus.Invalid => ToValidationProblem(result.ErrorDetails ?? throw new InvalidOperationException("Invalid upload results must include validation details.")),
            ResultStatus.Conflict => TypedResults.Problem(result.ErrorDetails?.Detail ?? "The tour gallery changed while this upload was processing. Retry the upload.", statusCode: StatusCodes.Status409Conflict),
            ResultStatus.Unavailable => TypedResults.Problem("Media upload scanner is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => TypedResults.Problem("Media upload could not be completed.")
        };
    }

    private static async Task<Results<Ok<CatalogMediaImageDto>, BadRequest, ValidationProblem, NotFound, ProblemHttpResult, ForbidHttpResult>> GenerateMediaImageAccessibilityDraft(
        Guid id,
        PublicMediaImageAccessibilityDraftRequest request,
        MediaImageAccessibilityDraftService service,
        IPublicMediaImageStore imageStore,
        CatalogGalleryMutationGuard galleryGuard,
        IAuthorizationService authorization,
        HttpContext context,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return TypedResults.BadRequest();
        }

        var errors = ValidateAccessibilityDraftRequest(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var image = await imageStore.GetImage(id, ct).ConfigureAwait(false);
        if (image is null)
        {
            return TypedResults.NotFound();
        }

        var canPublish = (await authorization.AuthorizeAsync(context.User, CatalogAuthorization.CatalogPublish).ConfigureAwait(false)).Succeeded;
        var permission = await galleryGuard.Acquire(image.TourLinks.Select(link => link.CatalogTourId).ToArray(), canPublish, ct).ConfigureAwait(false);
        if (permission.IsFailure)
        {
            return TypedResults.Forbid();
        }

        await using var mutationLease = permission.Value;
        var result = await service.GenerateDraft(
            id,
            new MediaImageAccessibilityDraftInput
            {
                Language = ToDomainLanguage(request.Language),
                Context = request.Context,
                Latitude = request.Latitude,
                Longitude = request.Longitude
            },
            ct);

        if (result.IsSuccess)
        {
            await InvalidatePublicCatalogCache(outputCacheStore, logger);
            return TypedResults.Ok(MapManagementMediaImage(result.Value));
        }

        return result.Status switch
        {
            ResultStatus.NotFound => TypedResults.NotFound(),
            ResultStatus.Invalid => ToValidationProblem(result.ErrorDetails),
            ResultStatus.Unavailable => TypedResults.Problem(result.ErrorDetails.Detail, statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => TypedResults.Problem(result.ErrorDetails.Detail)
        };
    }

    private static async Task<Results<Ok<CatalogMediaImageDto>, BadRequest, NotFound, ValidationProblem, ForbidHttpResult>> ReviewMediaImageAccessibility(
        Guid id,
        PublicMediaImageAccessibilityReviewRequest request,
        IPublicMediaImageStore imageStore,
        CatalogGalleryMutationGuard galleryGuard,
        IAuthorizationService authorization,
        HttpContext context,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return TypedResults.BadRequest();
        }

        var language = ToDomainLanguage(request.Language);
        try
        {
            var image = await imageStore.GetImage(id, ct).ConfigureAwait(false);
            if (image is null)
            {
                return TypedResults.NotFound();
            }

            var canPublish = (await authorization.AuthorizeAsync(context.User, CatalogAuthorization.CatalogPublish).ConfigureAwait(false)).Succeeded;
            var permission = await galleryGuard.Acquire(image.TourLinks.Select(link => link.CatalogTourId).ToArray(), canPublish, ct).ConfigureAwait(false);
            if (permission.IsFailure)
            {
                return TypedResults.Forbid();
            }

            await using var mutationLease = permission.Value;
            image = await imageStore.GetImage(id, ct).ConfigureAwait(false);
            if (image is null)
            {
                return TypedResults.NotFound();
            }

            var previous = image.AccessibilityTexts.FirstOrDefault(text => text.Language == language);
            var queueAge = previous?.PendingDraftCreatedAt is { } pendingAt ? Math.Max(0, (timeProvider.GetUtcNow() - pendingAt).TotalSeconds) : (double?)null;
            var edited = previous is not null && (previous.AltText != request.AltText || previous.Caption != request.Caption || previous.IsDecorative != request.IsDecorative);
            var result = image.SetReviewedAccessibilityText(language, request.AltText, request.Caption, request.IsDecorative);
            if (result.Status == ResultStatus.Invalid)
            {
                MediaAccessibilityTelemetry.RecordReview(!request.IsDecorative && string.IsNullOrWhiteSpace(request.AltText) ? "missing_alt" : "rejected", language);
                return ToValidationProblem(result.ErrorDetails ?? throw new InvalidOperationException("Invalid accessibility review results must include validation details."));
            }

            await imageStore.Upsert(image, ct).ConfigureAwait(false);
            MediaAccessibilityTelemetry.RecordReview(edited ? "edited" : "approved", language, queueAge);
            await InvalidatePublicCatalogCache(outputCacheStore, logger);
            return TypedResults.Ok(MapManagementMediaImage(image));
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(ct))
        {
            MediaAccessibilityTelemetry.RecordReview("failed", language);
            throw;
        }
    }

    private static async Task InvalidatePublicCatalogCache(IOutputCacheStore outputCacheStore, ILogger logger)
    {
        try
        {
            await outputCacheStore.EvictByTagAsync(PublicCatalogHttpCache.Tag, CancellationToken.None);
            logger.PublicCacheAreaInvalidated(PublicCatalogHttpCache.Area);
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(CancellationToken.None))
        {
            logger.PublicCacheAreaInvalidationFailed(PublicCatalogHttpCache.Area, exception.GetType().Name);
        }
    }

    private static async Task<Accepted> AcceptPendingProjection(
        Guid id,
        IOutputCacheStore outputCacheStore,
        ILogger logger)
    {
        await InvalidatePublicCatalogCache(outputCacheStore, logger);
        return TypedResults.Accepted(new Uri($"/api/v1/catalog/tours/{id}", UriKind.Relative));
    }

    private static void AddAcceptedLocationHeader(OpenApiOperation operation)
    {
        if (operation.Responses is null
            || !operation.Responses.TryGetValue("202", out var acceptedResponse)
            || acceptedResponse is not OpenApiResponse response)
        {
            return;
        }

        response.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.Ordinal);
        response.Headers["Location"] = new OpenApiHeader
        {
            Description = "Management detail resource to poll until projection completes.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        };
    }

    private static RouteHandlerBuilder RemoveInferredResponseMetadata(this RouteHandlerBuilder routeHandlerBuilder, int statusCode)
    {
        // Typed NotFound results infer a new 404 response; retain the existing published OpenAPI response set.
        routeHandlerBuilder.Finally(endpointBuilder =>
        {
            for (var index = endpointBuilder.Metadata.Count - 1; index >= 0; index--)
            {
                if (endpointBuilder.Metadata[index] is IProducesResponseTypeMetadata metadata && metadata.StatusCode == statusCode)
                {
                    endpointBuilder.Metadata.RemoveAt(index);
                }
            }
        });

        return routeHandlerBuilder;
    }

    private static async Task InvalidatePublicContentCache(IOutputCacheStore outputCacheStore, ILogger logger)
    {
        try
        {
            await outputCacheStore.EvictByTagAsync(PublicContentHttpCache.Tag, CancellationToken.None);
            logger.PublicCacheAreaInvalidated(PublicContentHttpCache.Area);
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(CancellationToken.None))
        {
            logger.PublicCacheAreaInvalidationFailed(PublicContentHttpCache.Area, exception.GetType().Name);
        }
    }

    private static CatalogTourDto MapManagementTour(CatalogTourDraftReadModel tour, IReadOnlyList<PublicMediaImage>? images)
    {
        return new CatalogTourDto
        {
            Id = tour.CatalogTourId,
            AdminTourId = tour.AdminTourId,
            Identifier = tour.Identifier,
            Title = tour.Title,
            Slug = tour.Slug,
            Summary = tour.Summary,
            Description = tour.Description,
            Itinerary = tour.Itinerary,
            SeoTitle = tour.SeoTitle,
            SeoDescription = tour.SeoDescription,
            IsPublished = tour.IsPublished,
            Version = tour.StreamVersion,
            Images = MapImages(images ?? []),
            UpdatedAt = tour.UpdatedAt
        };
    }

    private static TourSummaryDto MapPublicTourSummary(CatalogTourDraftReadModel tour, IReadOnlyList<PublicMediaImage> images)
    {
        return new TourSummaryDto
        {
            Title = tour.Title,
            Slug = tour.Slug,
            Summary = tour.Summary,
            Images = MapImages(images),
            UpdatedAt = tour.UpdatedAt
        };
    }

    private static TourDetailsDto MapPublicTourDetail(CatalogTourDraftReadModel tour, IReadOnlyList<PublicMediaImage> images)
    {
        return new TourDetailsDto
        {
            Title = tour.Title,
            Slug = tour.Slug,
            Summary = tour.Summary,
            Description = tour.Description,
            Itinerary = tour.Itinerary,
            SeoTitle = tour.SeoTitle,
            SeoDescription = tour.SeoDescription,
            Images = MapImages(images),
            UpdatedAt = tour.UpdatedAt
        };
    }

    private static CatalogTourImageDto[] MapImages(IReadOnlyList<PublicMediaImage> images)
    {
        return PublicMediaImage
            .OrderForGallery(images.Where(image => image.HasPublicVariants).ToArray())
            .Select(MapImage)
            .ToArray();
    }

    private static IReadOnlyList<PublicMediaImage> GetReadyImages(IReadOnlyList<PublicMediaImage> images)
    {
        return [.. images.Where(CanDeliverPublicImage)];
    }

    private static bool CanDeliverPublicImage(PublicMediaImage image)
    {
        if (!image.HasReviewedAccessibilityText)
        {
            MediaAccessibilityTelemetry.RecordPublicationBlocker(!image.IsDecorative && string.IsNullOrWhiteSpace(image.AltText));
        }

        return image.HasPublicVariants;
    }

    private static CatalogTourImageDto MapImage(PublicMediaImage image)
    {
        return new CatalogTourImageDto
        {
            Id = image.Id,
            SortOrder = image.DisplayOrder,
            IsCover = image.IsCover,
            AltText = image.AltText,
            IsDecorative = image.IsDecorative,
            Caption = image.Caption,
            ResponsiveVariants = image.ResponsiveVariants
                .OrderBy(variant => variant.Width)
                .Select(MapPublicResponsiveVariant)
                .ToArray()
        };
    }

    private static Dictionary<string, string[]> ValidateAccessibilityDraftRequest(PublicMediaImageAccessibilityDraftRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Language == PublicContentLanguageDto.None || !Enum.IsDefined(request.Language))
        {
            errors[nameof(PublicMediaImageAccessibilityDraftRequest.Language)] = ["Language is required."];
        }

        if (request.Context?.Length > 1_000)
        {
            errors[nameof(PublicMediaImageAccessibilityDraftRequest.Context)] = ["Context cannot exceed 1000 characters."];
        }

        if (request.Latitude is < -90 or > 90)
        {
            errors[nameof(PublicMediaImageAccessibilityDraftRequest.Latitude)] = ["Latitude must be between -90 and 90."];
        }

        if (request.Longitude is < -180 or > 180)
        {
            errors[nameof(PublicMediaImageAccessibilityDraftRequest.Longitude)] = ["Longitude must be between -180 and 180."];
        }

        if ((request.Latitude is null) != (request.Longitude is null))
        {
            errors[nameof(PublicMediaImageAccessibilityDraftRequest.Latitude)] = ["Latitude and longitude must be supplied together."];
        }

        return errors;
    }

    private static IReadOnlyList<PublicMediaImage> GetImages(
        IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>> imagesByTour,
        Guid tourId)
    {
        return imagesByTour.TryGetValue(tourId, out var images) ? images : [];
    }

    private static CatalogMediaImageDto MapManagementMediaImage(PublicMediaImage image)
    {
        return new CatalogMediaImageDto
        {
            Id = image.Id,
            AccessibilityTexts = image.AccessibilityTexts.Select(text => new PublicMediaAccessibilityTextDto
            {
                Language = ToContractLanguage(text.Language),
                AltText = text.AltText,
                Caption = text.Caption,
                IsDecorative = text.IsDecorative,
                RequiresHumanReview = text.RequiresHumanReview,
                IsAiGenerated = text.IsAiGenerated
            }).ToArray(),
            ResponsiveVariants = image.ResponsiveVariants
                .OrderBy(variant => variant.Width)
                .Select(MapManagementResponsiveVariant)
                .ToArray(),
            AltText = image.AltText,
            Caption = image.Caption,
            IsDecorative = image.IsDecorative,
            RequiresHumanReview = image.RequiresHumanReview,
            IsAiGenerated = image.IsAiGenerated
        };
    }

    private static CatalogMediaImageVariantDto MapManagementResponsiveVariant(MediaImageResponsiveVariant variant)
    {
        return new CatalogMediaImageVariantDto
        {
            Width = variant.Width,
            Height = variant.Height,
            ContentType = variant.ContentType,
            FileSizeBytes = variant.FileSizeBytes
        };
    }

    private static CatalogMediaImageVariantDto MapPublicResponsiveVariant(MediaImageResponsiveVariant variant)
    {
        return new CatalogMediaImageVariantDto
        {
            Width = variant.Width,
            Height = variant.Height,
            ContentType = variant.ContentType,
            FileSizeBytes = variant.FileSizeBytes
        };
    }

    private static string? GetMediaContentType(string format) => format.Trim().ToUpperInvariant() switch
    {
        "AVIF" => "image/avif",
        "JPG" => "image/jpeg",
        "PNG" => "image/png",
        "WEBP" => "image/webp",
        _ => null
    };

    private static async Task<MediaObjectReadResult?> TryOpenMediaObject(
        IMediaObjectStore objectStore,
        string objectKey,
        CancellationToken ct)
    {
        try
        {
            return await objectStore.OpenRead(objectKey, ct).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    private static async Task<bool> IsLinkedToPublishedTour(PublicMediaImage image, ICatalogTourReadModelStore tourStore, CancellationToken ct)
    {
        foreach (var link in image.TourLinks)
        {
            var tour = await tourStore.GetTour(link.CatalogTourId, ct).ConfigureAwait(false);
            if (tour?.IsPubliclyVisible == true)
            {
                return true;
            }
        }

        return false;
    }

    private static PublicContentDto MapPublicContent(EditablePublicContent content)
    {
        var dto = new PublicContentDto
        {
            Key = content.Key,
            SourceLanguage = ToContractLanguage(content.SourceLanguage),
            PublicationState = content.PublicationState.ToString()
        };

        foreach (var variant in content.Variants.OrderBy(variant => variant.Language))
        {
            dto.Variants.Add(MapVariant(variant));
        }

        return dto;
    }

    private static PublicContentVariantDto MapVariant(PublicContentVariant variant)
    {
        return new PublicContentVariantDto
        {
            Language = ToContractLanguage(variant.Language),
            Title = variant.Title,
            Body = variant.Body,
            SeoTitle = variant.SeoTitle,
            MetaDescription = variant.MetaDescription,
            ShareSummary = variant.ShareSummary,
            RequiresHumanReview = variant.RequiresHumanReview
        };
    }

    private static ValidationProblem ToValidationProblem(ResultError error)
    {
        return TypedResults.ValidationProblem(ToValidationProblemDictionary(error.ValidationErrors), detail: error.Detail);
    }

    private static ValidationProblem? ValidateExpectedVersion(long expectedVersion)
    {
        return expectedVersion < 1
            ? TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(CatalogTourPublicationRequest.ExpectedVersion)] = ["ExpectedVersion must be at least 1."]
            })
            : null;
    }

    private static Dictionary<string, string[]> ToValidationProblemDictionary(IReadOnlyDictionary<string, IReadOnlyList<string>>? validationErrors)
    {
        if (validationErrors is null)
        {
            return [];
        }

        var result = new Dictionary<string, string[]>(validationErrors.Count, StringComparer.Ordinal);
        foreach (var (field, messages) in validationErrors)
        {
            result[field] = [.. messages];
        }

        return result;
    }

    private static PublicContentLanguage ToDomainLanguage(PublicContentLanguageDto language)
    {
        return language == PublicContentLanguageDto.None || !Enum.IsDefined(language)
            ? PublicContentLanguage.None
            : (PublicContentLanguage)(int)language;
    }

    private static PublicContentLanguageDto ToContractLanguage(PublicContentLanguage language)
    {
        return language == PublicContentLanguage.None || !Enum.IsDefined(language)
            ? PublicContentLanguageDto.None
            : (PublicContentLanguageDto)(int)language;
    }

    private static bool TryGetPublicContentLanguage(string? language, string? culture, out PublicContentLanguage publicContentLanguage)
    {
        var requestedLanguage = string.IsNullOrWhiteSpace(language) ? culture : language;
        publicContentLanguage = requestedLanguage?.Trim().ToUpperInvariant() switch
        {
            null or "" or "EN-US" or "EN" => PublicContentLanguage.EnUs,
            "PT-BR" or "PT" => PublicContentLanguage.PtBr,
            _ => PublicContentLanguage.None
        };

        return publicContentLanguage != PublicContentLanguage.None;
    }

}
