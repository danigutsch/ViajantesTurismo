using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using PandaBear.BuildingBlocks;
using PandaBear.HttpCaching.AspNetCore;
using PandaBear.ObjectStorage;
using PandaBear.Results;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.ServiceDefaults;
using static ViajantesTurismo.Catalog.Api.CatalogValidationProblems;
using static ViajantesTurismo.Catalog.Api.Media.CatalogMediaResponses;

namespace ViajantesTurismo.Catalog.Api.Media;

/// <summary>Maps Catalog media intake, review, preview and public delivery.</summary>
internal static class CatalogMediaEndpoints
{
    /// <summary>Gets the endpoint name of the management gallery read.</summary>
    internal const string TourImagesEndpointName = nameof(ListTourImages);

    private const long MediaUploadMaxRequestBytes = MediaUploadValidationOptions.DefaultMaxLengthBytes + 16_384;

    private static readonly Dictionary<string, string> MediaContentTypesByFormat = new(StringComparer.OrdinalIgnoreCase)
    {
        ["avif"] = "image/avif",
        ["jpg"] = "image/jpeg",
        ["png"] = "image/png",
        ["webp"] = "image/webp"
    };

    private static readonly string[] MediaImageContentTypes = [.. MediaContentTypesByFormat.Values];

    /// <summary>Registers media routes and preserves their binding and response metadata.</summary>
    internal static void MapCatalogMediaEndpoints(this IEndpointRouteBuilder versionedApi, IEndpointRouteBuilder managementCatalog)
    {
        managementCatalog.MapGet("/tours/{id:guid}/images", ListTourImages)
            .WithName(TourImagesEndpointName)
            .Produces<CatalogMediaImageDto[]>(StatusCodes.Status200OK)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapGet("/tours/{id:guid}/images/review/next", GetNextPendingReviewImage)
            .WithName(nameof(GetNextPendingReviewImage))
            .WithSummary("Gets the next linked image that requires an accessibility review.")
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapGet("/media/images/{id:guid}/preview/{width:int}/{format}", GetManagementMediaPreview)
            .WithName("GetManagementMediaPreview")
            .Produces<Stream>(StatusCodes.Status200OK, MediaImageContentTypes[0], MediaImageContentTypes[1..])
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapPost("/tours/{id:guid}/images", UploadTourImage)
            .WithName("UploadTourImage")
            .Accepts<MediaImageUploadFormDto>("multipart/form-data")
            .Produces<CatalogMediaImageDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status504GatewayTimeout)
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
            .WithMetadata(
                new RequestSizeLimitAttribute(MediaUploadMaxRequestBytes),
                new RequestFormLimitsAttribute
                {
                    MultipartBodyLengthLimit = MediaUploadValidationOptions.DefaultMaxLengthBytes,
                    ValueCountLimit = 5
                })
            .WithRequestTimeout(ApiRequestLimits.MultipartUploadTimeoutPolicy)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .DisableAntiforgery()
            .RequireAuthorization(CatalogAuthorization.CatalogWrite);
        managementCatalog.MapPost("/media/images/{id:guid}/accessibility-draft", GenerateMediaImageAccessibilityDraft)
            .WithName("GenerateMediaImageAccessibilityDraft")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .RemoveInferredResponseMetadata(StatusCodes.Status404NotFound)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(CatalogAuthorization.MediaAi);
        managementCatalog.MapPut("/media/images/{id:guid}/accessibility-review", ReviewMediaImageAccessibility)
            .WithName("ReviewMediaImageAccessibility")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .RemoveInferredResponseMetadata(StatusCodes.Status404NotFound)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(CatalogAuthorization.CatalogWrite);

        versionedApi.MapGet("/public/catalog/media/{id:guid}/{width:int}/{format}", GetPublicMedia)
            .WithName("GetPublicMedia")
            .Produces<Stream>(StatusCodes.Status200OK, MediaImageContentTypes[0], MediaImageContentTypes[1..])
            .Produces(StatusCodes.Status404NotFound)
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy)
            .AllowAnonymous();
    }

    private static async Task<IResult> GetPublicMedia(
        Guid id,
        int width,
        string format,
        ICatalogTourReadModelStore tourStore,
        IPublicMediaImageStore imageStore,
        IObjectStore objectStore,
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
        IObjectStore objectStore,
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

    private static async Task<IResult> ListTourImages(
        Guid id,
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        LinkGenerator routes,
        HttpContext context,
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
        return Results.Ok(images.Select(image => MapManagementMediaImage(image, routes, context)).ToArray());
    }

    private static async Task<Results<Ok<CatalogPendingImageReviewDto>, BadRequest, NotFound>> GetNextPendingReviewImage(
        Guid id,
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        LinkGenerator routes,
        HttpContext context,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return TypedResults.BadRequest();
        }

        if (await store.GetTour(id, ct) is null)
        {
            return TypedResults.NotFound();
        }

        var image = await imageStore.GetNextPendingReviewByTour(id, ct);
        return TypedResults.Ok(new CatalogPendingImageReviewDto(
            image is null ? null : MapManagementMediaImage(image, routes, context)));
    }

    private static async Task<Results<Created<CatalogMediaImageDto>, BadRequest, NotFound, ValidationProblem, ProblemHttpResult, ForbidHttpResult>> UploadTourImage(
        Guid id,
        [AsParameters] MediaImageUploadFormDto form,
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        MediaImageUploadIntake intake,
        CatalogGalleryMutationGuard galleryGuard,
        IAuthorizationService authorization,
        LinkGenerator routes,
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
                Copyright: form.Copyright,
                CanPublishGalleryPlacement: canPublish),
            ct).ConfigureAwait(false);

        return result.Status switch
        {
            ResultStatus.Ok => TypedResults.Created($"/catalog/media/images/{result.Value.Image.Id}", MapManagementMediaImage(result.Value.Image, routes, context)),
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
        LinkGenerator routes,
        HttpContext context,
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
        var permission = await galleryGuard.Acquire([.. image.TourLinks.Select(link => link.CatalogTourId)], canPublish, ct).ConfigureAwait(false);
        if (permission.IsFailure)
        {
            return TypedResults.Forbid();
        }

        await using var mutationLease = permission.Value;
        var result = await service.GenerateDraft(
            id,
            new MediaImageAccessibilityDraftInput
            {
                Language = PublicContentCultures.Parse(request.Language),
                Context = request.Context,
                Latitude = request.Latitude,
                Longitude = request.Longitude
            },
            ct);

        if (result.IsSuccess)
        {
            return TypedResults.Ok(MapManagementMediaImage(result.Value, routes, context));
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
        LinkGenerator routes,
        HttpContext context,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return TypedResults.BadRequest();
        }

        var language = PublicContentCultures.Parse(request.Language);
        try
        {
            var image = await imageStore.GetImage(id, ct).ConfigureAwait(false);
            if (image is null)
            {
                return TypedResults.NotFound();
            }

            var canPublish = (await authorization.AuthorizeAsync(context.User, CatalogAuthorization.CatalogPublish).ConfigureAwait(false)).Succeeded;
            var permission = await galleryGuard.Acquire([.. image.TourLinks.Select(link => link.CatalogTourId)], canPublish, ct).ConfigureAwait(false);
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
            return TypedResults.Ok(MapManagementMediaImage(image, routes, context));
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(ct))
        {
            MediaAccessibilityTelemetry.RecordReview("failed", language);
            throw;
        }
    }

    /// <summary>Typed NotFound results infer a new 404 response; removing it retains the existing published OpenAPI response set.</summary>
    private static RouteHandlerBuilder RemoveInferredResponseMetadata(this RouteHandlerBuilder routeHandlerBuilder, int statusCode)
    {
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

    private static Dictionary<string, string[]> ValidateAccessibilityDraftRequest(PublicMediaImageAccessibilityDraftRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Language))
        {
            errors[nameof(PublicMediaImageAccessibilityDraftRequest.Language)] = ["Language is required."];
        }
        else if (!PublicContentCultures.IsSupported(PublicContentCultures.Parse(request.Language)))
        {
            errors[nameof(PublicMediaImageAccessibilityDraftRequest.Language)] = ["Language must be a supported culture."];
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

    private static CatalogMediaImageDto MapManagementMediaImage(PublicMediaImage image, LinkGenerator routes, HttpContext context)
    {
        var variants = image.ResponsiveVariants.OrderBy(variant => variant.Width).ToArray();
        return new CatalogMediaImageDto
        {
            Id = image.Id,
            Links = MapManagementMediaLinks(image, variants, routes, context),
            AccessibilityTexts = [.. image.AccessibilityTexts.Select(text => new PublicMediaAccessibilityTextDto
            {
                Language = text.Language.Value,
                AltText = text.AltText,
                Caption = text.Caption,
                IsDecorative = text.IsDecorative,
                RequiresHumanReview = text.RequiresHumanReview,
                IsAiGenerated = text.IsAiGenerated
            })],
            ResponsiveVariants = [.. variants.Select(MapManagementResponsiveVariant)],
            AltText = image.AltText,
            Caption = image.Caption,
            IsDecorative = image.IsDecorative,
            RequiresHumanReview = image.RequiresHumanReview,
            IsAiGenerated = image.IsAiGenerated
        };
    }

    private static List<HypermediaLink> MapManagementMediaLinks(
        PublicMediaImage image, IReadOnlyList<MediaImageResponsiveVariant> variants, LinkGenerator routes, HttpContext context)
    {
        List<HypermediaLink> links = [.. image.TourLinks.OrderBy(link => link.CatalogTourId).Select(link =>
            CatalogEndpointLinks.Get(routes, context, "collection", TourImagesEndpointName,
                new RouteValueDictionary { ["id"] = link.CatalogTourId }))];

        foreach (var variant in variants)
        {
            var format = MediaContentTypesByFormat.FirstOrDefault(pair =>
                string.Equals(pair.Value, variant.ContentType, StringComparison.OrdinalIgnoreCase)).Key;
            if (format is not null)
            {
                links.Add(CatalogEndpointLinks.Get(routes, context, "preview", nameof(GetManagementMediaPreview), new RouteValueDictionary
                {
                    ["id"] = image.Id,
                    ["width"] = variant.Width,
                    ["format"] = format
                }));
            }
        }

        return links;
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

    private static string? GetMediaContentType(string format) => MediaContentTypesByFormat.GetValueOrDefault(format.Trim());

    private static async Task<ObjectReadResult?> TryOpenMediaObject(
        IObjectStore objectStore,
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
}
