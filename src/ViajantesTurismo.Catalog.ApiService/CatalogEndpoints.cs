using Microsoft.AspNetCore.OutputCaching;
using SharedKernel.ApiVersioning.AspNetCore;
using SharedKernel.Results;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.PublicTheme;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Domain.PublicTheme;

namespace ViajantesTurismo.Catalog.ApiService;

internal static class CatalogEndpoints
{
    internal static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var versionedApi = app.MapApiVersionGroup(CatalogOpenApiDocuments.CurrentApiVersion)
            .WithApiVersion(CatalogOpenApiDocuments.CurrentApiVersion);

        versionedApi.MapGet("/catalog/tours", GetTours);
        versionedApi.MapGet("/catalog/tours/{id:guid}", GetTour);
        versionedApi.MapPut("/catalog/tours/{id:guid}/presentation", UpsertTourPresentation)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy);
        versionedApi.MapGet("/catalog/tours/{id:guid}/images", ListTourImages);
        versionedApi.MapPut("/catalog/media/images/{id:guid}", UpsertMediaImage)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy);
        versionedApi.MapPost("/catalog/media/images/{id:guid}/accessibility-draft", GenerateMediaImageAccessibilityDraft)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy);

        versionedApi.MapGet("/public/catalog/tours", GetPublishedTours)
            .CacheOutput(policy => policy.Expire(CatalogHttpCache.PublicFreshness).Tag(CatalogHttpCache.PublicCatalogTag))
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy);
        versionedApi.MapGet("/public/catalog/tours/{slug}", GetPublishedTour)
            .CacheOutput(policy => policy.Expire(CatalogHttpCache.PublicFreshness).Tag(CatalogHttpCache.PublicCatalogTag))
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy);
        versionedApi.MapGet("/public/catalog/content/{**key}", GetPublicContent)
            .CacheOutput(policy => policy.Expire(CatalogHttpCache.PublicFreshness).SetVaryByQuery(CatalogHttpCache.CultureQueryKey).Tag(CatalogHttpCache.PublicContentTag))
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy);
        versionedApi.MapGet("/public/catalog/theme", GetPublicTheme)
            .CacheOutput(policy => policy.Expire(CatalogHttpCache.PublicFreshness).Tag(CatalogHttpCache.PublicThemeTag))
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy);

        versionedApi.MapGet("/catalog/public-content", async (IPublicContentStore store, HttpContext httpContext, CancellationToken ct) =>
        {
            CatalogHttpCache.SetNoStore(httpContext);
            var content = await store.ListContent(ct);
            return content.Select(MapPublicContent);
        });
        versionedApi.MapGet("/catalog/public-content/{**key}", GetPublicContentForManagement);
        versionedApi.MapPut("/catalog/public-content/{**key}", UpsertPublicContent)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy);
        versionedApi.MapGet("/catalog/public-theme", GetPublicThemeForManagement);
        versionedApi.MapPut("/catalog/public-theme", UpsertPublicTheme)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy);

        return app;
    }

    private static async Task<IResult> GetTour(Guid id, ICatalogTourReadModelStore store, IPublicMediaImageStore imageStore, IMediaObjectStore objectStore, HttpContext httpContext, CancellationToken ct)
    {
        CatalogHttpCache.SetNoStore(httpContext);

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
        return Results.Ok(MapTour(tour, images, objectStore));
    }

    private static async Task<IResult> GetPublishedTour(string slug, ICatalogTourReadModelStore store, IPublicMediaImageStore imageStore, IMediaObjectStore objectStore, HttpContext httpContext, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Results.BadRequest();
        }

        var tour = await store.GetPublishedTourBySlug(slug, ct);
        if (tour is null)
        {
            CatalogHttpCache.SetNoStore(httpContext);
            return Results.NotFound();
        }

        var images = await imageStore.ListByTour(tour.CatalogTourId, ct);
        var dto = MapTour(tour, GetReadyImages(images), objectStore);
        CatalogHttpCache.SetPublicHeaders(httpContext);
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
            CatalogHttpCache.SetNoStore(httpContext);
            return Results.BadRequest();
        }

        if (!TryGetPublicContentLanguage(language, culture, out var requestedLanguage))
        {
            CatalogHttpCache.SetNoStore(httpContext);
            return Results.BadRequest();
        }

        var content = await store.GetContent(key, ct);
        if (content is null || !content.IsPubliclyVisible)
        {
            CatalogHttpCache.SetNoStore(httpContext);
            return Results.NotFound();
        }

        var variant = content.FindPublicVariant(requestedLanguage);
        if (variant is null)
        {
            CatalogHttpCache.SetNoStore(httpContext);
            return Results.NotFound();
        }

        var dto = MapVariant(variant);
        CatalogHttpCache.SetPublicHeaders(httpContext);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetPublicContentForManagement(string key, IPublicContentStore store, HttpContext httpContext, CancellationToken ct)
    {
        CatalogHttpCache.SetNoStore(httpContext);

        if (string.IsNullOrWhiteSpace(key))
        {
            return Results.BadRequest();
        }

        var content = await store.GetContent(key, ct);
        return content is null ? Results.NotFound() : Results.Ok(MapPublicContent(content));
    }

    private static async Task<IResult> GetPublicTheme(IPublicThemeSettingsStore store, HttpContext httpContext, CancellationToken ct)
    {
        var theme = await store.GetTheme(ct) ?? PublicThemeSettings.Default();
        var dto = MapTheme(theme);
        CatalogHttpCache.SetPublicHeaders(httpContext);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetPublicThemeForManagement(IPublicThemeSettingsStore store, HttpContext httpContext, CancellationToken ct)
    {
        CatalogHttpCache.SetNoStore(httpContext);
        var theme = await store.GetTheme(ct) ?? PublicThemeSettings.Default();
        return Results.Ok(MapTheme(theme));
    }

    private static async Task<IResult> UpsertPublicTheme(
        PublicThemeSettingsDto request,
        IPublicThemeSettingsStore store,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        CatalogHttpCache.SetNoStore(httpContext);

        var theme = PublicThemeSettings.Create(
            request.PrimaryColor,
            request.AccentColor,
            request.BackgroundColor,
            request.TextColor,
            request.HeadingFontFamily,
            request.BodyFontFamily);

        if (theme.IsFailure)
        {
            return ToValidationProblem(theme.ErrorDetails);
        }

        await store.SaveTheme(theme.Value, ct);
        await InvalidatePublicThemeCache(outputCacheStore, logger, ct);
        return Results.Ok(MapTheme(theme.Value));
    }

    private static async Task<IReadOnlyList<CatalogTourDto>> GetTours(
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        IMediaObjectStore objectStore,
        HttpContext httpContext,
        CancellationToken ct)
    {
        CatalogHttpCache.SetNoStore(httpContext);
        var tours = await store.ListTours(ct);
        var imagesByTour = await imageStore.ListByTours([.. tours.Select(tour => tour.CatalogTourId)], ct);

        return
        [
            .. tours.Select(tour => MapTour(tour, GetImages(imagesByTour, tour.CatalogTourId), objectStore))
        ];
    }

    private static async Task<IReadOnlyList<CatalogTourDto>> GetPublishedTours(
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        IMediaObjectStore objectStore,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tours = await store.ListTours(ct);
        var publishedTours = tours.Where(tour => tour.IsPubliclyVisible).ToArray();
        var imagesByTour = await imageStore.ListByTours([.. publishedTours.Select(tour => tour.CatalogTourId)], ct);

        CatalogTourDto[] result =
        [
            .. publishedTours.Select(tour => MapTour(tour, GetReadyImages(GetImages(imagesByTour, tour.CatalogTourId)), objectStore))
        ];
        CatalogHttpCache.SetPublicHeaders(httpContext);
        return result;
    }

    private static async Task<IResult> UpsertPublicContent(
        string key,
        UpsertPublicContentRequest request,
        IPublicContentStore store,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        CatalogHttpCache.SetNoStore(httpContext);

        if (string.IsNullOrWhiteSpace(key))
        {
            return Results.BadRequest();
        }

        if (request.Variants is null)
        {
            var missingVariants = Result.Invalid(
                "Public content variants must be provided.",
                nameof(UpsertPublicContentRequest.Variants),
                "Variants are required.");
            return ToValidationProblem(missingVariants.ErrorDetails ?? throw new InvalidOperationException("Public content validation errors must include validation details."));
        }

        var variants = request.Variants.Select(CreateVariant).ToArray();

        if (variants.Any(variant => variant.IsFailure))
        {
            return ToValidationProblemFromVariants(variants);
        }

        var content = EditablePublicContent.Create(
            key,
            ToDomainLanguage(request.SourceLanguage),
            variants.Select(variant => variant.Value));

        if (content.IsFailure)
        {
            return ToValidationProblem(content.ErrorDetails);
        }

        var publish = content.Value.PublishIfReady();
        if (publish.IsFailure)
        {
            return ToValidationProblem(publish.ErrorDetails);
        }

        await store.SaveContent(content.Value, ct);
        await InvalidatePublicContentCache(outputCacheStore, logger, ct);
        return Results.Ok(MapPublicContent(content.Value));
    }

    private static async Task<IResult> UpsertTourPresentation(
        Guid id,
        UpsertCatalogTourPresentationRequest request,
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        IMediaObjectStore objectStore,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        CatalogHttpCache.SetNoStore(httpContext);

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
        return Results.Ok(MapTour(updated, (IReadOnlyList<PublicMediaImage>?)images, objectStore));
    }

    private static async Task<IResult> ListTourImages(
        Guid id,
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        IMediaObjectStore objectStore,
        HttpContext httpContext,
        CancellationToken ct)
    {
        CatalogHttpCache.SetNoStore(httpContext);

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
        return Results.Ok(MapImages(images, objectStore));
    }

    private static async Task<IResult> UpsertMediaImage(
        Guid id,
        PublicMediaImageDto request,
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        IMediaObjectStore objectStore,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        CatalogHttpCache.SetNoStore(httpContext);

        if (id == Guid.Empty || id != request.Id)
        {
            return Results.BadRequest();
        }

        var errors = ValidateMediaImage(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var image = ToDomainMediaImage(request);
        if (image.IsFailure)
        {
            return ToValidationProblem(image.ErrorDetails);
        }

        foreach (var link in image.Value.TourLinks)
        {
            var tour = await store.GetTour(link.CatalogTourId, ct);
            if (tour is null)
            {
                return Results.NotFound();
            }
        }

        await imageStore.Upsert(image.Value, ct);
        await InvalidatePublicCatalogCache(outputCacheStore, logger, ct);
        return Results.Ok(MapMediaImage(image.Value, objectStore));
    }

    private static async Task<IResult> GenerateMediaImageAccessibilityDraft(
        Guid id,
        PublicMediaImageAccessibilityDraftRequest request,
        MediaImageAccessibilityDraftService service,
        IMediaObjectStore objectStore,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        CatalogHttpCache.SetNoStore(httpContext);

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
            return Results.Ok(MapMediaImage(result.Value, objectStore));
        }

        return result.Status switch
        {
            ResultStatus.NotFound => Results.NotFound(),
            ResultStatus.Invalid => ToValidationProblem(result.ErrorDetails),
            ResultStatus.Unavailable => Results.Problem(result.ErrorDetails.Detail, statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => Results.Problem(result.ErrorDetails.Detail)
        };
    }

    private static async Task InvalidatePublicCatalogCache(IOutputCacheStore outputCacheStore, ILogger logger, CancellationToken ct)
    {
        await outputCacheStore.EvictByTagAsync(CatalogHttpCache.PublicCatalogTag, ct);
        logger.PublicCacheAreaInvalidated(CatalogHttpCache.PublicCatalogArea);
    }

    private static async Task InvalidatePublicContentCache(IOutputCacheStore outputCacheStore, ILogger logger, CancellationToken ct)
    {
        await outputCacheStore.EvictByTagAsync(CatalogHttpCache.PublicContentTag, ct);
        logger.PublicCacheAreaInvalidated(CatalogHttpCache.PublicContentArea);
    }

    private static async Task InvalidatePublicThemeCache(IOutputCacheStore outputCacheStore, ILogger logger, CancellationToken ct)
    {
        await outputCacheStore.EvictByTagAsync(CatalogHttpCache.PublicThemeTag, ct);
        logger.PublicCacheAreaInvalidated(CatalogHttpCache.PublicThemeArea);
    }

    private static CatalogTourDto MapTour(CatalogTourDraftReadModel tour, IReadOnlyList<PublicMediaImage>? images, IMediaObjectStore objectStore)
    {
        return new CatalogTourDto
        {
            Id = tour.CatalogTourId,
            AdminTourId = tour.AdminTourId,
            Identifier = tour.Identifier,
            Title = tour.Title,
            Slug = tour.Slug,
            IsPublished = tour.IsPublished,
            Images = MapImages(images ?? [], objectStore),
            UpdatedAt = tour.UpdatedAt
        };
    }

    private static CatalogTourImageDto[] MapImages(IReadOnlyList<PublicMediaImage> images, IMediaObjectStore objectStore)
    {
        return PublicMediaImage
            .OrderForGallery(images)
            .Select(image => MapImage(image, objectStore))
            .ToArray();
    }

    private static IReadOnlyList<PublicMediaImage> GetReadyImages(IReadOnlyList<PublicMediaImage> images)
    {
        return [.. images.Where(image => image.HasPublicVariants)];
    }

    private static CatalogTourImageDto MapImage(PublicMediaImage image, IMediaObjectStore objectStore)
    {
        return new CatalogTourImageDto
        {
            SortOrder = image.DisplayOrder,
            IsCover = image.IsCover,
            Uri = GetPublicImageUri(image, objectStore),
            AltText = image.AltText,
            IsDecorative = image.IsDecorative,
            Caption = image.Caption,
            ResponsiveVariants = image.ResponsiveVariants
                .OrderBy(variant => variant.Width)
                .Select(variant => MapResponsiveVariant(variant, objectStore))
                .ToArray()
        };
    }

    private static Dictionary<string, string[]> ValidateMediaImage(PublicMediaImageDto image)
    {
        var errors = new Dictionary<string, string[]>();

        ValidateMediaShape(errors, image);

        return errors;
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

    private static void ValidateMediaShape(Dictionary<string, string[]> errors, PublicMediaImageDto image)
    {
        if (image.Dimensions is null)
        {
            errors[nameof(PublicMediaImageDto.Dimensions)] = ["Dimensions are required."];
        }

        if (image.TourLinks is null || image.TourLinks.Any(link => link is null))
        {
            errors[nameof(PublicMediaImageDto.TourLinks)] = ["Tour links are required."];
        }

        if (image.ResponsiveVariants is null || image.ResponsiveVariants.Any(variant => variant is null))
        {
            errors[nameof(PublicMediaImageDto.ResponsiveVariants)] = ["Responsive variants are required."];
        }

        if (image.Tags is null)
        {
            errors[nameof(PublicMediaImageDto.Tags)] = ["Tags are required."];
        }

        if (string.IsNullOrWhiteSpace(image.SourceObjectKey) && !IsHttpUri(image.SourceUri))
        {
            errors[nameof(PublicMediaImageDto.SourceUri)] = ["Source URI must be an absolute HTTP or HTTPS URI when SourceObjectKey is not provided."];
        }

        if (image.ResponsiveVariants is not null && image.ResponsiveVariants.Any(static variant => variant is not null && string.IsNullOrWhiteSpace(variant.ObjectKey) && !IsHttpUri(variant.Uri)))
        {
            errors[nameof(PublicMediaImageDto.ResponsiveVariants)] = ["Responsive variants must include absolute HTTP or HTTPS URIs when ObjectKey is not provided."];
        }

        ValidateMediaAccessibilityTexts(errors, image);
    }

    private static void ValidateMediaAccessibilityTexts(Dictionary<string, string[]> errors, PublicMediaImageDto image)
    {
        if (image.AccessibilityTexts is null)
        {
            errors[nameof(PublicMediaImageDto.AccessibilityTexts)] = ["Accessibility texts are required."];
        }
        else if (image.AccessibilityTexts.Any(text => text is null))
        {
            errors[nameof(PublicMediaImageDto.AccessibilityTexts)] = ["Accessibility text entries are required."];
        }
        else if (image.AccessibilityTexts.Select(text => text.Language).Distinct().Count() != image.AccessibilityTexts.Count)
        {
            errors[nameof(PublicMediaImageDto.AccessibilityTexts)] = ["Accessibility text languages cannot be duplicated."];
        }
        else if (image.AccessibilityTexts.Any(static text => text.IsAiGenerated && !text.RequiresHumanReview))
        {
            errors[nameof(PublicMediaImageDto.AccessibilityTexts)] = ["AI-generated accessibility text requires human review."];
        }
        else if (image.AccessibilityTexts.Any(static text => text.IsAiGenerated && text.IsDecorative))
        {
            errors[nameof(PublicMediaImageDto.AccessibilityTexts)] = ["AI draft accessibility text cannot mark images decorative."];
        }

        var defaultText = image.AccessibilityTexts?.FirstOrDefault(static text => text.Language == PublicContentLanguageDto.EnUs);
        if (defaultText is not null && !DefaultAccessibilityTextMatches(image, defaultText))
        {
            errors[nameof(PublicMediaImageDto.AccessibilityTexts)] = ["Default-language accessibility text must match the top-level accessibility fields."];
        }
    }

    private static bool DefaultAccessibilityTextMatches(PublicMediaImageDto image, PublicMediaAccessibilityTextDto text)
    {
        return string.Equals(image.AltText ?? string.Empty, text.AltText ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(image.Caption, text.Caption, StringComparison.Ordinal)
            && image.IsDecorative == text.IsDecorative
            && image.RequiresHumanReview == text.RequiresHumanReview
            && image.IsAiGenerated == text.IsAiGenerated;
    }

    private static bool IsHttpUri(Uri? uri)
    {
        return uri is not null
            && uri.IsAbsoluteUri
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static IReadOnlyList<PublicMediaImage> GetImages(
        IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>> imagesByTour,
        Guid tourId)
    {
        return imagesByTour.TryGetValue(tourId, out var images) ? images : [];
    }

    private static Result<PublicMediaImage> ToDomainMediaImage(PublicMediaImageDto image)
    {
        var result = PublicMediaImage.Create(
            new PublicMediaImageMetadata
            {
                Id = image.Id,
                SourceObjectKey = GetSourceObjectKey(image),
                Checksum = image.Checksum ?? string.Empty,
                ContentType = image.ContentType ?? string.Empty,
                FileSizeBytes = image.FileSizeBytes,
                Dimensions = image.Dimensions is null
                    ? new MediaImageDimensions(0, 0)
                    : new MediaImageDimensions(image.Dimensions.Width, image.Dimensions.Height),
                ProcessingStatus = (MediaImageProcessingStatus)(int)image.ProcessingStatus,
                AltText = image.AltText ?? string.Empty,
                Caption = image.Caption,
                IsDecorative = image.IsDecorative,
                RequiresHumanReview = image.RequiresHumanReview,
                IsAiGenerated = image.IsAiGenerated,
                Attribution = image.Attribution,
                Copyright = image.Copyright
            },
            image.ResponsiveVariants.Select(ToDomainResponsiveVariant).ToArray(),
            image.Tags,
            image.TourLinks.Select(link => new MediaImageTourLink(link.CatalogTourId, link.DisplayOrder, link.IsCover)).ToArray());

        if (result.IsFailure || image.AccessibilityTexts.Count == 0)
        {
            return result;
        }

        var mediaImage = result.Value;
        foreach (var text in image.AccessibilityTexts)
        {
            var language = ToDomainLanguage(text.Language);
            Result textResult;
            if (text.IsAiGenerated)
            {
                textResult = mediaImage.SetAiDraftAccessibilityText(language, text.AltText ?? string.Empty, text.Caption);
            }
            else if (text.RequiresHumanReview)
            {
                textResult = mediaImage.SetDraftAccessibilityText(language, text.AltText ?? string.Empty, text.Caption);
            }
            else
            {
                textResult = mediaImage.SetReviewedAccessibilityText(language, text.AltText, text.Caption, text.IsDecorative);
            }

            if (textResult.IsFailure)
            {
                return Result.Invalid<PublicMediaImage>(textResult.ErrorDetails.Detail, textResult.ErrorDetails.ValidationErrors?.ToDictionary(error => error.Key, error => error.Value.ToArray(), StringComparer.Ordinal) ?? []);
            }
        }

        return result;
    }

    private static MediaImageResponsiveVariant ToDomainResponsiveVariant(MediaImageResponsiveVariantDto variant)
    {
        return new MediaImageResponsiveVariant(
            GetVariantObjectKey(variant),
            variant.Width,
            variant.Height,
            variant.ContentType ?? string.Empty,
            variant.FileSizeBytes);
    }

    private static PublicMediaImageDto MapMediaImage(PublicMediaImage image, IMediaObjectStore objectStore)
    {
        return new PublicMediaImageDto
        {
            Id = image.Id,
            SourceObjectKey = image.SourceObjectKey,
            SourceUri = objectStore.GetPublicUri(image.SourceObjectKey),
            Checksum = image.Checksum,
            ContentType = image.ContentType,
            FileSizeBytes = image.FileSizeBytes,
            Dimensions = new MediaImageDimensionsDto { Width = image.Dimensions.Width, Height = image.Dimensions.Height },
            ProcessingStatus = (MediaImageProcessingStatusDto)(int)image.ProcessingStatus,
            ResponsiveVariants = image.ResponsiveVariants.Select(variant => MapResponsiveVariant(variant, objectStore)).ToArray(),
            Tags = image.Tags,
            TourLinks = image.TourLinks
                .Select(link => new MediaImageTourLinkDto
                {
                    CatalogTourId = link.CatalogTourId,
                    DisplayOrder = link.DisplayOrder,
                    IsCover = link.IsCover
                })
                .ToArray(),
            AltText = image.AltText,
            Caption = image.Caption,
            IsDecorative = image.IsDecorative,
            RequiresHumanReview = image.RequiresHumanReview,
            IsAiGenerated = image.IsAiGenerated,
            AccessibilityTexts = image.AccessibilityTexts.Select(MapAccessibilityText).ToArray(),
            Attribution = image.Attribution,
            Copyright = image.Copyright
        };
    }

    private static PublicMediaAccessibilityTextDto MapAccessibilityText(PublicMediaImageAccessibilityText text)
    {
        return new PublicMediaAccessibilityTextDto
        {
            Language = ToContractLanguage(text.Language),
            AltText = text.AltText,
            Caption = text.Caption,
            IsDecorative = text.IsDecorative,
            RequiresHumanReview = text.RequiresHumanReview,
            IsAiGenerated = text.IsAiGenerated
        };
    }

    private static MediaImageResponsiveVariantDto MapResponsiveVariant(MediaImageResponsiveVariant variant, IMediaObjectStore objectStore)
    {
        return new MediaImageResponsiveVariantDto
        {
            ObjectKey = variant.ObjectKey,
            Uri = objectStore.GetPublicUri(variant.ObjectKey),
            Width = variant.Width,
            Height = variant.Height,
            ContentType = variant.ContentType,
            FileSizeBytes = variant.FileSizeBytes
        };
    }

    private static Uri GetPublicImageUri(PublicMediaImage image, IMediaObjectStore objectStore)
    {
        var largestVariant = image.ResponsiveVariants.OrderByDescending(variant => variant.Width).FirstOrDefault();

        return objectStore.GetPublicUri(largestVariant?.ObjectKey ?? image.SourceObjectKey);
    }

    private static string GetSourceObjectKey(PublicMediaImageDto image)
    {
        if (!string.IsNullOrWhiteSpace(image.SourceObjectKey))
        {
            return image.SourceObjectKey;
        }

        return image.SourceUri?.AbsolutePath.TrimStart('/') ?? string.Empty;
    }

    private static string GetVariantObjectKey(MediaImageResponsiveVariantDto variant)
    {
        if (!string.IsNullOrWhiteSpace(variant.ObjectKey))
        {
            return variant.ObjectKey;
        }

        return variant.Uri?.AbsolutePath.TrimStart('/') ?? string.Empty;
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

    private static PublicThemeSettingsDto MapTheme(PublicThemeSettings theme)
    {
        return new PublicThemeSettingsDto
        {
            PrimaryColor = theme.PrimaryColor,
            AccentColor = theme.AccentColor,
            BackgroundColor = theme.BackgroundColor,
            TextColor = theme.TextColor,
            HeadingFontFamily = theme.HeadingFontFamily,
            BodyFontFamily = theme.BodyFontFamily
        };
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

    private static Result<PublicContentVariant> CreateVariant(PublicContentVariantDto? variant)
    {
        if (variant is null)
        {
            return Result.Invalid<PublicContentVariant>(
                "Public content variants cannot contain null entries.",
                nameof(UpsertPublicContentRequest.Variants),
                "Variants cannot contain null entries.");
        }

        var language = ToDomainLanguage(variant.Language);

        return PublicContentVariant.Create(
            language,
            variant.Title,
            variant.Body,
            variant.SeoTitle,
            variant.MetaDescription,
            variant.ShareSummary,
            variant.RequiresHumanReview);
    }

    private static IResult ToValidationProblem(ResultError error)
    {
        return Results.ValidationProblem(ToValidationProblemDictionary(error.ValidationErrors), detail: error.Detail);
    }

    private static IResult ToValidationProblemFromVariants(IEnumerable<Result<PublicContentVariant>> results)
    {
        var validationErrors = new ValidationErrors();

        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                validationErrors.Add(result);
            }
        }

        var error = validationErrors.ToResult().ErrorDetails ?? throw new InvalidOperationException("Public content validation errors must include validation details.");
        return ToValidationProblem(error);
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
