using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Common.Sanitizers;
using ViajantesTurismo.ServiceDefaults;
using SharedKernel.Results;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.AddCatalogInfrastructure();

var app = builder.Build();

app.MapGet("/catalog/tours", GetTours);

app.MapGet("/catalog/tours/{id:guid}", GetTour);

app.MapPut("/catalog/tours/{id:guid}/presentation", UpsertTourPresentation);
app.MapGet("/catalog/tours/{id:guid}/images", ListTourImages);
app.MapPut("/catalog/media/images/{id:guid}", UpsertMediaImage);

app.MapGet("/public/catalog/tours", GetPublishedTours);

app.MapGet("/public/catalog/tours/{slug}", GetPublishedTour);

app.MapGet("/public/catalog/content/{**key}", GetPublicContent);

app.MapGet("/catalog/public-content", async (IPublicContentStore store, CancellationToken ct) =>
{
    var content = await store.ListContent(ct);
    return content.Select(MapPublicContent);
});

app.MapGet("/catalog/public-content/{**key}", GetPublicContentForManagement);

app.MapPut("/catalog/public-content/{**key}", UpsertPublicContent);

app.MapDefaultEndpoints();

await app.RunAsync();

static async Task<IResult> GetTour(Guid id, ICatalogTourReadModelStore store, IPublicMediaImageStore imageStore, CancellationToken ct)
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
    return Results.Ok(MapTour(tour, images));
}

static async Task<IResult> GetPublishedTour(string slug, ICatalogTourReadModelStore store, IPublicMediaImageStore imageStore, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(slug))
    {
        return Results.BadRequest();
    }

    var tour = await store.GetPublishedTourBySlug(slug, ct);
    if (tour is null)
    {
        return Results.NotFound();
    }

    var images = await imageStore.ListByTour(tour.CatalogTourId, ct);
    return Results.Ok(MapTour(tour, GetReadyImages(images)));
}

static async Task<IResult> GetPublicContent(
    string key,
    string? language,
    string? culture,
    IPublicContentStore store,
    CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(key))
    {
        return Results.BadRequest();
    }

    if (!TryGetPublicContentLanguage(language, culture, out var requestedLanguage))
    {
        return Results.BadRequest();
    }

    var content = await store.GetContent(key, ct);
    if (content is null || content.PublicationState != PublicContentPublicationState.Published)
    {
        return Results.NotFound();
    }

    var variant = GetApprovedVariant(content, requestedLanguage);
    return variant is null ? Results.NotFound() : Results.Ok(MapVariant(variant));
}

static async Task<IResult> GetPublicContentForManagement(string key, IPublicContentStore store, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(key))
    {
        return Results.BadRequest();
    }

    var content = await store.GetContent(key, ct);
    return content is null ? Results.NotFound() : Results.Ok(MapPublicContent(content));
}

static async Task<IReadOnlyList<CatalogTourDto>> GetTours(
    ICatalogTourReadModelStore store,
    IPublicMediaImageStore imageStore,
    CancellationToken ct)
{
    var tours = await store.ListTours(ct);
    var imagesByTour = await imageStore.ListByTours([.. tours.Select(tour => tour.CatalogTourId)], ct);

    return
    [
        .. tours.Select(tour => MapTour(tour, GetImages(imagesByTour, tour.CatalogTourId)))
    ];
}

static async Task<IReadOnlyList<CatalogTourDto>> GetPublishedTours(
    ICatalogTourReadModelStore store,
    IPublicMediaImageStore imageStore,
    CancellationToken ct)
{
    var tours = await store.ListTours(ct);
    var publishedTours = tours.Where(IsPublished).ToArray();
    var imagesByTour = await imageStore.ListByTours([.. publishedTours.Select(tour => tour.CatalogTourId)], ct);

    return
    [
        .. publishedTours.Select(tour => MapTour(tour, GetReadyImages(GetImages(imagesByTour, tour.CatalogTourId))))
    ];
}

static async Task<IResult> UpsertPublicContent(
    string key,
    UpsertPublicContentRequest request,
    IPublicContentStore store,
    CancellationToken ct)
{
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

    if (content.Value.Variants.All(variant => !variant.RequiresHumanReview))
    {
        var publish = content.Value.Publish();
        if (publish.IsFailure)
        {
            return ToValidationProblem(publish.ErrorDetails);
        }
    }

    await store.SaveContent(content.Value, ct);
    return Results.Ok(MapPublicContent(content.Value));
}

static async Task<IResult> UpsertTourPresentation(
    Guid id,
    UpsertCatalogTourPresentationRequest request,
    ICatalogTourReadModelStore store,
    IPublicMediaImageStore imageStore,
    CancellationToken ct)
{
    if (id == Guid.Empty)
    {
        return Results.BadRequest();
    }

    var title = StringSanitizer.Sanitize(request.Title) ?? string.Empty;
    var slug = StringSanitizer.Sanitize(request.Slug) ?? string.Empty;
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(title))
    {
        errors[nameof(request.Title)] = ["Title is required."];
    }
    else if (title.Length > ContractConstants.MaxNameLength)
    {
        errors[nameof(request.Title)] = [$"Title cannot exceed {ContractConstants.MaxNameLength} characters."];
    }

    if (string.IsNullOrWhiteSpace(slug))
    {
        errors[nameof(request.Slug)] = ["Slug is required."];
    }
    else if (slug.Length > ContractConstants.MaxSlugLength)
    {
        errors[nameof(request.Slug)] = [$"Slug cannot exceed {ContractConstants.MaxSlugLength} characters."];
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var updated = await store.UpdatePresentation(
        id,
        new CatalogTourPresentationUpdate(title, slug, request.IsPublished),
        ct);

    if (updated is null)
    {
        return Results.NotFound();
    }

    var images = await imageStore.ListByTour(id, ct);
    return Results.Ok(MapTour(updated, (IReadOnlyList<PublicMediaImage>?)images));
}

static async Task<IResult> ListTourImages(
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
    return Results.Ok(MapImages(images));
}

static async Task<IResult> UpsertMediaImage(
    Guid id,
    PublicMediaImageDto request,
    ICatalogTourReadModelStore store,
    IPublicMediaImageStore imageStore,
    CancellationToken ct)
{
    if (id == Guid.Empty || id != request.Id)
    {
        return Results.BadRequest();
    }

    var errors = ValidateMediaImage(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    foreach (var link in request.TourLinks)
    {
        var tour = await store.GetTour(link.CatalogTourId, ct);
        if (tour is null)
        {
            return Results.NotFound();
        }
    }

    var image = ToDomainMediaImage(request);
    await imageStore.Upsert(image, ct);
    return Results.Ok(MapMediaImage(image));
}

static CatalogTourDto MapTour(CatalogTourDraftReadModel tour, IReadOnlyList<PublicMediaImage>? images = null)
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

static IReadOnlyList<CatalogTourImageDto> MapImages(IReadOnlyList<PublicMediaImage> images)
{
    return images
        .OrderByDescending(IsCover)
        .ThenBy(GetDisplayOrder)
        .ThenBy(image => image.Id)
        .Select(MapImage)
        .ToArray();
}

static IReadOnlyList<PublicMediaImage> GetReadyImages(IReadOnlyList<PublicMediaImage> images)
{
    return [.. images.Where(image => image.ProcessingStatus == MediaImageProcessingStatus.Ready && image.ResponsiveVariants.Count > 0)];
}

static CatalogTourImageDto MapImage(PublicMediaImage image)
{
    return new CatalogTourImageDto
    {
        SortOrder = GetDisplayOrder(image),
        IsCover = IsCover(image),
        Uri = GetPublicImageUri(image),
        AltText = image.AltText,
        Caption = image.Caption,
        ResponsiveVariants = image.ResponsiveVariants
            .OrderBy(variant => variant.Width)
            .Select(MapResponsiveVariant)
            .ToArray()
    };
}

static Dictionary<string, string[]> ValidateMediaImage(PublicMediaImageDto image)
{
    var errors = new Dictionary<string, string[]>();

    if (image.SourceUri is null || !image.SourceUri.IsAbsoluteUri)
    {
        errors[nameof(PublicMediaImageDto.SourceUri)] = ["Source URI must be absolute."];
    }

    var altText = StringSanitizer.Sanitize(image.AltText) ?? string.Empty;
    if (string.IsNullOrWhiteSpace(altText))
    {
        errors[nameof(PublicMediaImageDto.AltText)] = ["Alt text is required."];
    }
    else if (altText.Length > ContractConstants.MaxAltTextLength)
    {
        errors[nameof(PublicMediaImageDto.AltText)] = [$"Alt text cannot exceed {ContractConstants.MaxAltTextLength} characters."];
    }

    if (image.Dimensions is null)
    {
        errors[nameof(PublicMediaImageDto.Dimensions)] = ["Dimensions are required."];
    }
    else if (image.Dimensions.Width <= 0 || image.Dimensions.Height <= 0)
    {
        errors[nameof(PublicMediaImageDto.Dimensions)] = ["Dimensions must be positive."];
    }

    ValidateRequiredLength(errors, nameof(PublicMediaImageDto.Checksum), image.Checksum, ContractConstants.MaxChecksumLength);
    ValidateRequiredLength(errors, nameof(PublicMediaImageDto.ContentType), image.ContentType, ContractConstants.MaxContentTypeLength);

    if (image.FileSizeBytes <= 0)
    {
        errors[nameof(PublicMediaImageDto.FileSizeBytes)] = ["File size must be positive."];
    }

    if (image.ProcessingStatus == MediaImageProcessingStatusDto.None || !Enum.IsDefined(image.ProcessingStatus))
    {
        errors[nameof(PublicMediaImageDto.ProcessingStatus)] = ["Processing status is required."];
    }

    if (image.TourLinks is null || image.TourLinks.Count == 0)
    {
        errors[nameof(PublicMediaImageDto.TourLinks)] = ["At least one tour link is required."];
    }
    else if (image.TourLinks.Any(link => link is null || link.CatalogTourId == Guid.Empty || link.DisplayOrder < 0))
    {
        errors[nameof(PublicMediaImageDto.TourLinks)] = ["Tour links require a tour id and non-negative display order."];
    }

    if (image.ResponsiveVariants is null || image.ResponsiveVariants.Any(IsInvalidResponsiveVariant))
    {
        errors[nameof(PublicMediaImageDto.ResponsiveVariants)] = ["Responsive variants must include absolute URIs, positive dimensions, content type, and file size."];
    }
    else if (image.ProcessingStatus == MediaImageProcessingStatusDto.Ready && image.ResponsiveVariants.Count == 0)
    {
        errors[nameof(PublicMediaImageDto.ResponsiveVariants)] = ["Ready images require at least one processed public variant."];
    }

    if (image.Tags is null || image.Tags.Any(tag => string.IsNullOrWhiteSpace(StringSanitizer.Sanitize(tag))))
    {
        errors[nameof(PublicMediaImageDto.Tags)] = ["Tags cannot contain blank values."];
    }

    ValidateOptionalLength(errors, nameof(PublicMediaImageDto.Caption), image.Caption, ContractConstants.MaxCaptionLength);
    ValidateOptionalLength(errors, nameof(PublicMediaImageDto.Attribution), image.Attribution, ContractConstants.MaxAttributionLength);
    ValidateOptionalLength(errors, nameof(PublicMediaImageDto.Copyright), image.Copyright, ContractConstants.MaxCopyrightLength);

    return errors;
}

static bool IsInvalidResponsiveVariant(MediaImageResponsiveVariantDto variant)
{
    var contentType = StringSanitizer.Sanitize(variant.ContentType);

    return variant.Uri is null
        || !variant.Uri.IsAbsoluteUri
        || variant.Width <= 0
        || variant.Height <= 0
        || string.IsNullOrWhiteSpace(contentType)
        || contentType.Length > ContractConstants.MaxContentTypeLength
        || variant.FileSizeBytes <= 0;
}

static IReadOnlyList<PublicMediaImage> GetImages(
    IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>> imagesByTour,
    Guid tourId)
{
    return imagesByTour.TryGetValue(tourId, out var images) ? images : [];
}

static void ValidateRequiredLength(Dictionary<string, string[]> errors, string field, string? value, int maxLength)
{
    var sanitized = StringSanitizer.Sanitize(value);
    if (string.IsNullOrWhiteSpace(sanitized))
    {
        errors[field] = [$"{field} is required."];
    }
    else if (sanitized.Length > maxLength)
    {
        errors[field] = [$"{field} cannot exceed {maxLength} characters."];
    }
}

static void ValidateOptionalLength(Dictionary<string, string[]> errors, string field, string? value, int maxLength)
{
    var sanitized = StringSanitizer.Sanitize(value);
    if (sanitized?.Length > maxLength)
    {
        errors[field] = [$"{field} cannot exceed {maxLength} characters."];
    }
}

static PublicMediaImage ToDomainMediaImage(PublicMediaImageDto image)
{
    return new PublicMediaImage(
        image.Id,
        image.SourceUri,
        StringSanitizer.Sanitize(image.Checksum) ?? string.Empty,
        StringSanitizer.Sanitize(image.ContentType) ?? string.Empty,
        image.FileSizeBytes,
        new MediaImageDimensions(image.Dimensions.Width, image.Dimensions.Height),
        (MediaImageProcessingStatus)(int)image.ProcessingStatus,
        image.ResponsiveVariants.Select(ToDomainResponsiveVariant).ToArray(),
        StringSanitizer.SanitizeCollection(image.Tags),
        image.TourLinks.Select(link => new MediaImageTourLink(link.CatalogTourId, link.DisplayOrder, link.IsCover)).ToArray(),
        StringSanitizer.Sanitize(image.AltText) ?? string.Empty,
        StringSanitizer.Sanitize(image.Caption),
        StringSanitizer.Sanitize(image.Attribution),
        StringSanitizer.Sanitize(image.Copyright));
}

static MediaImageResponsiveVariant ToDomainResponsiveVariant(MediaImageResponsiveVariantDto variant)
{
    return new MediaImageResponsiveVariant(
        variant.Uri,
        variant.Width,
        variant.Height,
        StringSanitizer.Sanitize(variant.ContentType) ?? string.Empty,
        variant.FileSizeBytes);
}

static PublicMediaImageDto MapMediaImage(PublicMediaImage image)
{
    return new PublicMediaImageDto
    {
        Id = image.Id,
        SourceUri = image.SourceUri,
        Checksum = image.Checksum,
        ContentType = image.ContentType,
        FileSizeBytes = image.FileSizeBytes,
        Dimensions = new MediaImageDimensionsDto { Width = image.Dimensions.Width, Height = image.Dimensions.Height },
        ProcessingStatus = (MediaImageProcessingStatusDto)(int)image.ProcessingStatus,
        ResponsiveVariants = image.ResponsiveVariants.Select(MapResponsiveVariant).ToArray(),
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
        Attribution = image.Attribution,
        Copyright = image.Copyright
    };
}

static MediaImageResponsiveVariantDto MapResponsiveVariant(MediaImageResponsiveVariant variant)
{
    return new MediaImageResponsiveVariantDto
    {
        Uri = variant.Uri,
        Width = variant.Width,
        Height = variant.Height,
        ContentType = variant.ContentType,
        FileSizeBytes = variant.FileSizeBytes
    };
}

static Uri GetPublicImageUri(PublicMediaImage image)
{
    return image.ResponsiveVariants.OrderByDescending(variant => variant.Width).FirstOrDefault()?.Uri ?? image.SourceUri;
}

static int GetDisplayOrder(PublicMediaImage image)
{
    return image.TourLinks.Count == 0 ? 0 : image.TourLinks.Min(link => link.DisplayOrder);
}

static bool IsCover(PublicMediaImage image)
{
    return image.TourLinks.Any(link => link.IsCover);
}

static bool IsPublished(CatalogTourDraftReadModel tour)
{
    return tour.IsPublished;
}

static PublicContentVariant? GetApprovedVariant(EditablePublicContent content, PublicContentLanguage requestedLanguage)
{
    var variant = content.Variants.FirstOrDefault(variant => variant.Language == requestedLanguage && !variant.RequiresHumanReview);
    return variant is not null || requestedLanguage == PublicContentLanguage.EnUs
        ? variant
        : content.Variants.FirstOrDefault(variant => variant.Language == PublicContentLanguage.EnUs && !variant.RequiresHumanReview);
}

static PublicContentDto MapPublicContent(EditablePublicContent content)
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

static PublicContentVariantDto MapVariant(PublicContentVariant variant)
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

static Result<PublicContentVariant> CreateVariant(PublicContentVariantDto? variant)
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

static IResult ToValidationProblem(ResultError error)
{
    return Results.ValidationProblem(ToValidationProblemDictionary(error.ValidationErrors), detail: error.Detail);
}

static IResult ToValidationProblemFromVariants(IEnumerable<Result<PublicContentVariant>> results)
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

static Dictionary<string, string[]> ToValidationProblemDictionary(IReadOnlyDictionary<string, IReadOnlyList<string>>? validationErrors)
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

static PublicContentLanguage ToDomainLanguage(PublicContentLanguageDto language)
{
    return language == PublicContentLanguageDto.None || !Enum.IsDefined(language)
        ? PublicContentLanguage.None
        : (PublicContentLanguage)(int)language;
}

static PublicContentLanguageDto ToContractLanguage(PublicContentLanguage language)
{
    return language == PublicContentLanguage.None || !Enum.IsDefined(language)
        ? PublicContentLanguageDto.None
        : (PublicContentLanguageDto)(int)language;
}

static bool TryGetPublicContentLanguage(string? language, string? culture, out PublicContentLanguage publicContentLanguage)
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
