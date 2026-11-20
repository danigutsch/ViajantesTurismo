using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;
using PandaBear.BuildingBlocks;
using PandaBear.HttpCaching.AspNetCore;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using static ViajantesTurismo.Catalog.Api.CatalogValidationProblems;

namespace ViajantesTurismo.Catalog.Api.PublicContent;

/// <summary>Maps management and published Catalog content.</summary>
internal static class CatalogPublicContentEndpoints
{
    /// <summary>Registers editable and public content routes on their respective groups.</summary>
    internal static void MapCatalogPublicContentEndpoints(this IEndpointRouteBuilder versionedApi, IEndpointRouteBuilder managementCatalog)
    {
        versionedApi.MapGet("/public/catalog/content/{**key}", GetPublicContent)
            .WithName("GetPublicContent")
            .Produces<PublicContentVariantDto>(StatusCodes.Status200OK)
            .CacheOutput(policy => policy.Expire(PublicContentHttpCache.Freshness).SetVaryByQuery(PublicContentHttpCache.CultureQueryKey).Tag(PublicContentHttpCache.Tag))
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy)
            .AllowAnonymous();
        managementCatalog.MapGet("/public-content", async (IPublicContentStore store, LinkGenerator routes, HttpContext context, CancellationToken ct) =>
        {
            var content = await store.ListContent(ct);
            return content.Select(entry => MapPublicContent(entry, routes, context));
        }).WithName("ListPublicContentForManagement")
            .WithTags("ViajantesTurismo.Catalog.Api")
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapGet("/public-content/{**key}", GetPublicContentForManagement)
            .WithName("GetPublicContentForManagement")
            .Produces<PublicContentDto>(StatusCodes.Status200OK)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapPut("/public-content/{**key}", UpsertPublicContent)
            .WithName("UpsertPublicContent")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .RequireRateLimiting(CatalogSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(CatalogAuthorization.CatalogWrite);
    }

    private static async Task<IResult> GetPublicContent(
        string key,
        string? language,
        string? culture,
        IPublicContentStore store,
        LinkGenerator routes,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.BadRequest();
        }

        var requestedLanguage = PublicContentCultures.Resolve(string.IsNullOrWhiteSpace(language) ? culture : language);
        if (requestedLanguage is null)
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

        if (!content.TryResolvePublicVariant(requestedLanguage, out var variant))
        {
            HttpCacheHeaders.SetNoStore(httpContext);
            return Results.NotFound();
        }

        var parameters = new RouteValueDictionary
        {
            ["key"] = content.Key,
            [PublicContentHttpCache.CultureQueryKey] = variant.Language.Value
        };
        var self = CatalogEndpointLinks.Get(routes, httpContext, "self", nameof(GetPublicContent), parameters);
        var dto = MapVariant(variant, [self]);
        PublicContentHttpCache.SetPublicHeaders(httpContext);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetPublicContentForManagement(
        string key, IPublicContentStore store, LinkGenerator routes, HttpContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Results.BadRequest();
        }

        var content = await store.GetContent(key, ct);
        return content is null ? Results.NotFound() : Results.Ok(MapPublicContent(content, routes, context));
    }

    private static async Task<Results<Ok<PublicContentDto>, BadRequest, ValidationProblem>> UpsertPublicContent(
        string key,
        UpsertPublicContentRequest request,
        PublicContentUpsertService service,
        IOutputCacheStore outputCacheStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        LinkGenerator routes,
        HttpContext context,
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
        return TypedResults.Ok(MapPublicContent(content.Value, routes, context));
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

    private static PublicContentDto MapPublicContent(EditablePublicContent content, LinkGenerator routes, HttpContext context)
    {
        var dto = new PublicContentDto
        {
            Links =
            [
                CatalogEndpointLinks.Get(routes, context, "self", nameof(GetPublicContentForManagement), new RouteValueDictionary { ["key"] = content.Key })
            ],
            Key = content.Key,
            SourceLanguage = content.SourceLanguage.Value,
            PublicationState = content.PublicationState.ToString()
        };

        foreach (var variant in content.Variants.OrderBy(variant => SupportedCultureOrder(variant.Language)))
        {
            dto.Variants.Add(MapVariant(variant));
        }

        return dto;
    }

    private static PublicContentVariantDto MapVariant(PublicContentVariant variant, IReadOnlyList<HypermediaLink>? links = null)
    {
        return new PublicContentVariantDto
        {
            Links = links,
            Language = variant.Language.Value,
            Title = variant.Title,
            Body = variant.Body,
            SeoTitle = variant.SeoTitle,
            MetaDescription = variant.MetaDescription,
            ShareSummary = variant.ShareSummary,
            RequiresHumanReview = variant.RequiresHumanReview
        };
    }

    private static int SupportedCultureOrder(ContentCulture culture)
    {
        var cultures = PublicContentCultures.Supported.Cultures;
        for (var index = 0; index < cultures.Count; index++)
        {
            if (cultures[index] == culture)
            {
                return index;
            }
        }

        return cultures.Count;
    }
}
