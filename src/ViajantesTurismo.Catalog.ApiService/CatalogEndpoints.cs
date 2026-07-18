using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using SharedKernel.ApiVersioning.AspNetCore;
using SharedKernel.HttpCaching.AspNetCore;
using SharedKernel.Results;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.ApiService;

internal static class CatalogEndpoints
{

    internal static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var versionedApi = app.MapApiVersionGroup(CatalogOpenApiDocuments.CurrentApiVersion);

        versionedApi.MapGet("/catalog/tours", GetTours)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        versionedApi.MapGet("/catalog/tours/{id:guid}", GetTour)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        versionedApi.MapPut("/catalog/tours/{id:guid}/presentation", UpsertTourPresentation)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(CatalogAuthorization.CatalogWrite);
        versionedApi.MapGet("/catalog/tours/{id:guid}/images", ListTourImages)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        versionedApi.MapGet("/catalog/media/images/{id:guid}/preview/{width:int}/{format}", GetManagementMediaPreview)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        versionedApi.MapPost("/catalog/tours/{id:guid}/images", UploadTourImage)
            .Accepts<MediaImageUploadFormDto>("multipart/form-data")
            .Produces<CatalogMediaImageDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
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
        versionedApi.MapPost("/catalog/media/images/{id:guid}/accessibility-draft", GenerateMediaImageAccessibilityDraft)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(CatalogAuthorization.MediaAi);
        versionedApi.MapPut("/catalog/media/images/{id:guid}/accessibility-review", ReviewMediaImageAccessibility)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(CatalogAuthorization.CatalogWrite);

        versionedApi.MapGet("/public/catalog/tours", GetPublishedTours)
            .CacheOutput(policy => policy.Expire(PublicCatalogHttpCache.Freshness).Tag(PublicCatalogHttpCache.Tag))
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy)
            .AllowAnonymous();
        versionedApi.MapGet("/public/catalog/tours/{slug}", GetPublishedTour)
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
        versionedApi.MapGet("/catalog/public-content", async (IPublicContentStore store, HttpContext httpContext, CancellationToken ct) =>
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            var content = await store.ListContent(ct);
            return content.Select(MapPublicContent);
        }).RequireAuthorization(CatalogAuthorization.CatalogRead);
        versionedApi.MapGet("/catalog/public-content/{**key}", GetPublicContentForManagement)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        versionedApi.MapPut("/catalog/public-content/{**key}", UpsertPublicContent)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(CatalogAuthorization.CatalogWrite);

        return app;
    }

    private static async Task<IResult> GetTour(Guid id, ICatalogTourReadModelStore store, IPublicMediaImageStore imageStore, HttpContext httpContext, CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(httpContext);

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
        return Results.Ok(MapTour(tour, images));
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
        var dto = MapTour(tour, GetReadyImages(images));
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
        if (image is null || !image.HasPublicVariants || !await IsLinkedToPublishedTour(image, tourStore, ct).ConfigureAwait(false))
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
        HttpCacheHeaders.SetNoStore(httpContext);

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

    private static async Task<IResult> GetPublicContentForManagement(string key, IPublicContentStore store, HttpContext httpContext, CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(httpContext);

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
        HttpContext httpContext,
        CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(httpContext);
        var tours = await store.ListTours(ct);
        var imagesByTour = await imageStore.ListByTours([.. tours.Select(tour => tour.CatalogTourId)], ct);

        return
        [
            .. tours.Select(tour => MapTour(tour, GetImages(imagesByTour, tour.CatalogTourId)))
        ];
    }

    private static async Task<IReadOnlyList<CatalogTourDto>> GetPublishedTours(
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tours = await store.ListTours(ct);
        var publishedTours = tours.Where(tour => tour.IsPubliclyVisible).ToArray();
        var imagesByTour = await imageStore.ListByTours([.. publishedTours.Select(tour => tour.CatalogTourId)], ct);

        CatalogTourDto[] result =
        [
            .. publishedTours.Select(tour => MapTour(tour, GetReadyImages(GetImages(imagesByTour, tour.CatalogTourId))))
        ];
        PublicCatalogHttpCache.SetPublicHeaders(httpContext);
        return result;
    }

    private static async Task<IResult> UpsertPublicContent(
        string key,
        UpsertPublicContentRequest request,
        PublicContentUpsertService service,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(httpContext);

        if (string.IsNullOrWhiteSpace(key))
        {
            return Results.BadRequest();
        }

        var content = await service.Upsert(key, request, ct);

        if (content.IsFailure)
        {
            return ToValidationProblem(content.ErrorDetails ?? throw new InvalidOperationException("Public content validation errors must include validation details."));
        }

        await InvalidatePublicContentCache(outputCacheStore, logger, ct);
        return Results.Ok(MapPublicContent(content.Value));
    }

    private static async Task<IResult> UpsertTourPresentation(
        Guid id,
        UpsertCatalogTourPresentationRequest request,
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(httpContext);

        if (id == Guid.Empty)
        {
            return Results.BadRequest();
        }

        var presentation = CatalogTourPresentationUpdate.Create(request.Title, request.Slug, request.IsPublished);
        if (presentation.IsFailure)
        {
            return ToValidationProblem(presentation.ErrorDetails);
        }

        var updated = await store.UpdatePresentation(
            id,
            presentation.Value,
            ct);

        if (updated is null)
        {
            return Results.NotFound();
        }

        var images = await imageStore.ListByTour(id, ct);
        await InvalidatePublicCatalogCache(outputCacheStore, logger, ct);
        return Results.Ok(MapTour(updated, (IReadOnlyList<PublicMediaImage>?)images));
    }

    private static async Task<IResult> ListTourImages(
        Guid id,
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        HttpContext httpContext,
        CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(httpContext);

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

    private static async Task<IResult> UploadTourImage(
        Guid id,
        [AsParameters] MediaImageUploadFormDto form,
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        MediaImageUploadIntake intake,
        HttpContext httpContext,
        CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(httpContext);

        var altText = form.AltText;
        if (id == Guid.Empty || form.File is null || string.IsNullOrWhiteSpace(altText))
        {
            return Results.BadRequest();
        }

        if (await store.GetTour(id, ct).ConfigureAwait(false) is null)
        {
            return Results.NotFound();
        }

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
            ResultStatus.Ok => Results.Created($"/catalog/media/images/{result.Value.Image.Id}", MapManagementMediaImage(result.Value.Image)),
            ResultStatus.Invalid => ToValidationProblem(result.ErrorDetails ?? throw new InvalidOperationException("Invalid upload results must include validation details.")),
            ResultStatus.Conflict => Results.Problem(result.ErrorDetails?.Detail ?? "The tour gallery changed while this upload was processing. Retry the upload.", statusCode: StatusCodes.Status409Conflict),
            ResultStatus.Unavailable => Results.Problem("Media upload scanner is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => Results.Problem("Media upload could not be completed.")
        };
    }

    private static async Task<IResult> GenerateMediaImageAccessibilityDraft(
        Guid id,
        PublicMediaImageAccessibilityDraftRequest request,
        MediaImageAccessibilityDraftService service,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(httpContext);

        if (id == Guid.Empty)
        {
            return Results.BadRequest();
        }

        var errors = ValidateAccessibilityDraftRequest(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

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
            await InvalidatePublicCatalogCache(outputCacheStore, logger, ct);
            return Results.Ok(MapManagementMediaImage(result.Value));
        }

        return result.Status switch
        {
            ResultStatus.NotFound => Results.NotFound(),
            ResultStatus.Invalid => ToValidationProblem(result.ErrorDetails),
            ResultStatus.Unavailable => Results.Problem(result.ErrorDetails.Detail, statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => Results.Problem(result.ErrorDetails.Detail)
        };
    }

    private static async Task<IResult> ReviewMediaImageAccessibility(
        Guid id,
        PublicMediaImageAccessibilityReviewRequest request,
        IPublicMediaImageStore imageStore,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(httpContext);

        if (id == Guid.Empty)
        {
            return Results.BadRequest();
        }

        var image = await imageStore.GetImage(id, ct).ConfigureAwait(false);
        if (image is null)
        {
            return Results.NotFound();
        }

        var result = image.SetReviewedAccessibilityText(ToDomainLanguage(request.Language), request.AltText, request.Caption, request.IsDecorative);
        if (result.Status == ResultStatus.Invalid)
        {
            return ToValidationProblem(result.ErrorDetails ?? throw new InvalidOperationException("Invalid accessibility review results must include validation details."));
        }

        await imageStore.Upsert(image, ct).ConfigureAwait(false);
        await InvalidatePublicCatalogCache(outputCacheStore, logger, ct);
        return Results.Ok(MapManagementMediaImage(image));
    }

    private static async Task InvalidatePublicCatalogCache(IOutputCacheStore outputCacheStore, ILogger logger, CancellationToken ct)
    {
        await outputCacheStore.EvictByTagAsync(PublicCatalogHttpCache.Tag, ct);
        logger.PublicCacheAreaInvalidated(PublicCatalogHttpCache.Area);
    }

    private static async Task InvalidatePublicContentCache(IOutputCacheStore outputCacheStore, ILogger logger, CancellationToken ct)
    {
        await outputCacheStore.EvictByTagAsync(PublicContentHttpCache.Tag, ct);
        logger.PublicCacheAreaInvalidated(PublicContentHttpCache.Area);
    }

    private static CatalogTourDto MapTour(CatalogTourDraftReadModel tour, IReadOnlyList<PublicMediaImage>? images)
    {
        return new CatalogTourDto
        {
            Id = tour.CatalogTourId,
            AdminTourId = tour.AdminTourId,
            Identifier = tour.Identifier,
            Title = tour.Title,
            Slug = tour.Slug,
            IsPublished = tour.IsPublished,
            Images = MapImages(images ?? []),
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
        return [.. images.Where(image => image.HasPublicVariants)];
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

    private static IResult ToValidationProblem(ResultError error)
    {
        return Results.ValidationProblem(ToValidationProblemDictionary(error.ValidationErrors), detail: error.Detail);
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
