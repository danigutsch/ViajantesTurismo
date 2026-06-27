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

app.MapPut("/catalog/public-content/{key}", async (
    string key,
    UpsertPublicContentRequest request,
    IPublicContentStore store,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(key))
    {
        return Results.BadRequest();
    }

    var enUs = CreateVariant(request.EnUs, PublicContentLanguage.EnUs);
    var ptBr = CreateVariant(request.PtBr, PublicContentLanguage.PtBr);

    if (enUs.IsFailure || ptBr.IsFailure)
    {
        return ToValidationProblemFromVariants([enUs, ptBr]);
    }

    var content = EditablePublicContent.Create(
        key,
        ToDomainLanguage(request.SourceLanguage),
        enUs.Value,
        ptBr.Value);

    if (content.IsFailure)
    {
        return ToValidationProblem(content.ErrorDetails);
    }

    await store.SaveContent(content.Value, ct);
    return Results.Ok(MapPublicContent(content.Value));
});

app.MapDefaultEndpoints();

await app.RunAsync();

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
    return new PublicContentDto
    {
        Key = content.Key,
        SourceLanguage = ToContractLanguage(content.SourceLanguage),
        EnUs = MapVariant(content.EnUs),
        PtBr = MapVariant(content.PtBr),
        PublicationState = content.PublicationState.ToString()
    };
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

static Result<PublicContentVariant> CreateVariant(
    PublicContentVariantDto variant,
    PublicContentLanguage language)
{
    var actualLanguage = ToDomainLanguage(variant.Language);
    if (actualLanguage != language)
    {
        var mismatch = PublicContentErrors.VariantLanguageMismatch(nameof(PublicContentVariantDto.Language), language, actualLanguage);
        var error = mismatch.ErrorDetails ?? throw new InvalidOperationException("Variant language mismatch must include validation details.");
        return Result.Invalid<PublicContentVariant>(error.Detail, ToValidationProblemDictionary(error.ValidationErrors));
    }

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
    return language switch
    {
        PublicContentLanguageDto.EnUs => PublicContentLanguage.EnUs,
        PublicContentLanguageDto.PtBr => PublicContentLanguage.PtBr,
        _ => PublicContentLanguage.None
    };
}

static PublicContentLanguageDto ToContractLanguage(PublicContentLanguage language)
{
    return language switch
    {
        PublicContentLanguage.EnUs => PublicContentLanguageDto.EnUs,
        PublicContentLanguage.PtBr => PublicContentLanguageDto.PtBr,
        _ => PublicContentLanguageDto.None
    };
}
