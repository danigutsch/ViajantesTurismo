using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts;
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

app.MapGet("/catalog/tours", async (ICatalogTourReadModelStore store, CancellationToken ct) =>
{
    var tours = await store.ListTours(ct);
    return tours.Select(MapTour);
});

app.MapGet("/catalog/tours/{id:guid}", async (Guid id, ICatalogTourReadModelStore store, CancellationToken ct) =>
{
    if (id == Guid.Empty)
    {
        return Results.BadRequest();
    }

    var tour = await store.GetTour(id, ct);
    return tour is null ? Results.NotFound() : Results.Ok(MapTour(tour));
});

app.MapPut("/catalog/tours/{id:guid}/presentation", UpsertTourPresentation);

app.MapGet("/public/catalog/tours", async (ICatalogTourReadModelStore store, CancellationToken ct) =>
{
    var tours = await store.ListTours(ct);
    return tours.Where(IsPublished).Select(MapTour);
});

app.MapGet("/public/catalog/tours/{slug}", async (string slug, ICatalogTourReadModelStore store, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(slug))
    {
        return Results.BadRequest();
    }

    var tour = await store.GetPublishedTourBySlug(slug, ct);
    return tour is null ? Results.NotFound() : Results.Ok(MapTour(tour));
});

app.MapGet("/public/catalog/content/{key}", async (
    string key,
    string? language,
    string? culture,
    IPublicContentStore store,
    CancellationToken ct) =>
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
});

app.MapGet("/catalog/public-content", async (IPublicContentStore store, CancellationToken ct) =>
{
    var content = await store.ListContent(ct);
    return content.Select(MapPublicContent);
});

app.MapGet("/catalog/public-content/{key}", async (string key, IPublicContentStore store, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(key))
    {
        return Results.BadRequest();
    }

    var content = await store.GetContent(key, ct);
    return content is null ? Results.NotFound() : Results.Ok(MapPublicContent(content));
});

app.MapPut("/catalog/public-content/{key}", UpsertPublicContent);

app.MapDefaultEndpoints();

await app.RunAsync();

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

    await store.SaveContent(content.Value, ct);
    return Results.Ok(MapPublicContent(content.Value));
}

static async Task<IResult> UpsertTourPresentation(
    Guid id,
    UpsertCatalogTourPresentationRequest request,
    ICatalogTourReadModelStore store,
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

    return updated is null ? Results.NotFound() : Results.Ok(MapTour(updated));
}

static CatalogTourDto MapTour(CatalogTourDraftReadModel tour)
{
    return new CatalogTourDto
    {
        Id = tour.CatalogTourId,
        AdminTourId = tour.AdminTourId,
        Identifier = tour.Identifier,
        Title = tour.Title,
        Slug = tour.Slug,
        IsPublished = tour.IsPublished,
        Images = [],
        UpdatedAt = tour.UpdatedAt
    };
}

static bool IsPublished(CatalogTourDraftReadModel tour)
{
    return tour.IsPublished;
}

static PublicContentVariant? GetApprovedVariant(EditablePublicContent content, PublicContentLanguage requestedLanguage)
{
    var variant = content.Variants.FirstOrDefault(variant => variant.Language == requestedLanguage && !variant.RequiresHumanReview);
    if (variant is not null || requestedLanguage == PublicContentLanguage.EnUs)
    {
        return variant;
    }

    return content.Variants.FirstOrDefault(variant => variant.Language == PublicContentLanguage.EnUs && !variant.RequiresHumanReview);
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
