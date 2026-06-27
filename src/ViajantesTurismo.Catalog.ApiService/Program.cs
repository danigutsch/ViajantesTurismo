using SharedKernel.Configuration;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.ServiceDefaults;
using SharedKernel.Results;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.AddCatalogInfrastructure();
builder.Services.AddValidatedOptions<IntegrationEventOptions, IntegrationEventOptionsValidator>();
builder.Services.AddSingleton<ICatalogTourReadModelStore, InMemoryCatalogTourReadModelStore>();

var app = builder.Build();

app.MapGet("/catalog/tours", async (ICatalogTourReadModelStore store, CancellationToken ct) =>
{
    var tours = await store.ListTours(ct);
    return tours.Select(MapTour);
});

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

static CatalogTourDto MapTour(CatalogTourDraftReadModel tour)
{
    return new CatalogTourDto
    {
        Id = tour.CatalogTourId,
        AdminTourId = tour.AdminTourId,
        Identifier = tour.Identifier,
        Title = tour.Title,
        Slug = CreateSlug(tour.Identifier),
        IsPublished = IsPublished(tour),
        Images = [],
        UpdatedAt = tour.UpdatedAt
    };
}

static bool IsPublished(CatalogTourDraftReadModel tour)
{
    // Publish state is intentionally false until Catalog publish events are added to the read model.
    return false;
}

static string CreateSlug(string identifier) => identifier.Trim();

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
