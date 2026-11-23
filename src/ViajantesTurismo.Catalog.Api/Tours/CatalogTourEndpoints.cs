using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi;
using PandaBear.EventSourcing;
using PandaBear.HttpCaching.AspNetCore;
using PandaBear.Results;
using ViajantesTurismo.Catalog.Api.Media;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Media;
using static ViajantesTurismo.Catalog.Api.CatalogValidationProblems;
using static ViajantesTurismo.Catalog.Api.Media.CatalogMediaResponses;

namespace ViajantesTurismo.Catalog.Api.Tours;

/// <summary>Maps public tours and management presentation and publication operations.</summary>
internal static class CatalogTourEndpoints
{
    /// <summary>Registers the Catalog tour routes on their public and management groups.</summary>
    internal static void MapCatalogTourEndpoints(this IEndpointRouteBuilder versionedApi, IEndpointRouteBuilder managementCatalog)
    {
        managementCatalog.MapGet("/tours", GetTours)
            .WithName("GetTours")
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapGet("/tours/page", GetTourPage)
            .WithName("GetTourPage")
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapGet("/tours/{id:guid}", GetTour)
            .WithName("GetTour")
            .Produces<CatalogTourDto>(StatusCodes.Status200OK)
            .RequireAuthorization(CatalogAuthorization.CatalogRead);
        managementCatalog.MapPut("/tours/{id:guid}/presentation", UpsertTourPresentation)
            .WithName("UpsertTourPresentation")
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
            .WithName("PublishTour")
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
            .WithName("UnpublishTour")
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

        versionedApi.MapGet("/public/catalog/tours", GetPublishedTours)
            .WithName("GetPublishedTours")
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy)
            .AllowAnonymous();
        versionedApi.MapGet("/public/catalog/tours/{slug}", GetPublishedTour)
            .WithName("GetPublishedTour")
            .Produces<TourDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy)
            .AllowAnonymous();
    }

    private static async Task<IResult> GetTour(
        Guid id, ICatalogTourReadModelStore store, IPublicMediaImageStore imageStore, LinkGenerator routes, HttpContext context, CancellationToken ct)
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
        return Results.Ok(MapManagementTour(tour, images, routes, context));
    }

    private static async Task<IResult> GetPublishedTour(
        string slug, ICatalogTourReadModelStore store, IPublicMediaImageStore imageStore, LinkGenerator routes, HttpContext httpContext, CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(httpContext);
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
        var dto = MapPublicTourDetail(tour, GetReadyImages(images), routes, httpContext);
        return Results.Ok(dto);
    }

    private static async Task<IReadOnlyList<CatalogTourDto>> GetTours(
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        LinkGenerator routes,
        HttpContext context,
        CancellationToken ct)
    {
        var tours = await store.ListTours(ct);
        var imagesByTour = await imageStore.ListByTours([.. tours.Select(tour => tour.CatalogTourId)], ct);

        return
        [
            .. tours.Select(tour => MapManagementTour(tour, GetImages(imagesByTour, tour.CatalogTourId), routes, context))
        ];
    }

    private static async ValueTask<Results<Ok<CatalogTourPageDto>, BadRequest>> GetTourPage(
        ICatalogTourListReader reader,
        CancellationToken ct,
        string? page = null,
        string? sort = null,
        bool descending = false)
    {
        var order = CatalogTourListSort.Title;
        if (sort is not null && (!Enum.TryParse(sort, ignoreCase: true, out order) || !Enum.IsDefined(order)))
        {
            return TypedResults.BadRequest();
        }

        return TypedResults.Ok(await reader.ListPage(page, order, descending, ct));
    }

    private static async Task<IReadOnlyList<TourSummaryDto>> GetPublishedTours(
        ICatalogTourReadModelStore store,
        IPublicMediaImageStore imageStore,
        LinkGenerator routes,
        HttpContext httpContext,
        CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(httpContext);
        var tours = await store.ListTours(ct);
        var publishedTours = tours.Where(tour => tour.IsPubliclyVisible).ToArray();
        var imagesByTour = await imageStore.ListByTours([.. publishedTours.Select(tour => tour.CatalogTourId)], ct);

        TourSummaryDto[] result =
        [
            .. publishedTours.Select(tour => MapPublicTourSummary(
                tour, GetReadyImages(GetImages(imagesByTour, tour.CatalogTourId)), routes, httpContext))
        ];
        return result;
    }

    private static async Task<Results<Ok<CatalogTourDto>, Accepted, BadRequest, ValidationProblem, NotFound, ProblemHttpResult>> UpsertTourPresentation(
        Guid id,
        UpsertCatalogTourPresentationRequest request,
        CatalogTourPresentationService presentationService,
        IPublicMediaImageStore imageStore,
        ILogger<CatalogApiHostEntryPoint> logger,
        LinkGenerator routes,
        HttpContext context,
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
        Result<CatalogTourDraftReadModel> updated;
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
        catch (ExpectedStreamRevisionConflictException)
        {
            return TypedResults.Problem("The tour changed while this edit was in progress. Reload it and try again.", statusCode: StatusCodes.Status409Conflict);
        }
        catch (CatalogTourProjectionPendingException exception)
        {
            logger.TourProjectionPending(exception.GetType().Name);
            return AcceptPendingProjection(id);
        }

        switch (updated.Status)
        {
            case ResultStatus.NotFound:
                return TypedResults.NotFound();
            case ResultStatus.Conflict:
                return TypedResults.Problem(updated.ErrorDetails?.Detail, statusCode: StatusCodes.Status409Conflict);
            case ResultStatus.Invalid:
                return ToValidationProblem(updated.ErrorDetails ?? throw new InvalidOperationException("Invalid presentation results must include validation details."));
            case ResultStatus.Ok:
                break;
            default:
                throw new InvalidOperationException($"Unexpected Catalog tour presentation result status '{updated.Status}'.");
        }

        return TypedResults.Ok(MapManagementTour(updated.Value, (IReadOnlyList<PublicMediaImage>?)images, routes, context));
    }

    private static async Task<Results<NoContent, Accepted, BadRequest, ValidationProblem, NotFound, ProblemHttpResult>> PublishTour(
        Guid id,
        CatalogTourPublicationRequest request,
        CatalogTourPresentationService presentationService,
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
            var published = await presentationService.Publish(id, request.ExpectedVersion, ct).ConfigureAwait(false);
            switch (published.Status)
            {
                case ResultStatus.NotFound:
                    return TypedResults.NotFound();
                case ResultStatus.Conflict:
                    return TypedResults.Problem(published.ErrorDetails?.Detail, statusCode: StatusCodes.Status409Conflict);
                case ResultStatus.Invalid:
                    return TypedResults.ValidationProblem(ToValidationProblemDictionary(published.ErrorDetails?.ValidationErrors));
                case ResultStatus.Ok:
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected Catalog tour publication result status '{published.Status}'.");
            }
        }
        catch (ExpectedStreamRevisionConflictException)
        {
            return TypedResults.Problem("The tour changed while publication was in progress. Reload it and try again.", statusCode: StatusCodes.Status409Conflict);
        }
        catch (CatalogTourProjectionPendingException exception)
        {
            logger.TourProjectionPending(exception.GetType().Name);
            return AcceptPendingProjection(id);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, Accepted, BadRequest, ValidationProblem, NotFound, ProblemHttpResult>> UnpublishTour(
        Guid id,
        CatalogTourPublicationRequest request,
        CatalogTourPresentationService presentationService,
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
            var unpublished = await presentationService.Unpublish(id, request.ExpectedVersion, ct).ConfigureAwait(false);
            if (unpublished.Status == ResultStatus.NotFound)
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
            return AcceptPendingProjection(id);
        }

        return TypedResults.NoContent();
    }

    private static Accepted AcceptPendingProjection(Guid id)
    {
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

    private static CatalogTourDto MapManagementTour(
        CatalogTourDraftReadModel tour, IReadOnlyList<PublicMediaImage>? images, LinkGenerator routes, HttpContext context)
    {
        var id = new RouteValueDictionary { ["id"] = tour.CatalogTourId };
        return new CatalogTourDto
        {
            Links =
            [
                CatalogEndpointLinks.Get(routes, context, "self", nameof(GetTour), id),
                CatalogEndpointLinks.Get(routes, context, "images", CatalogMediaEndpoints.TourImagesEndpointName, id)
            ],
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

    private static TourSummaryDto MapPublicTourSummary(
        CatalogTourDraftReadModel tour, IReadOnlyList<PublicMediaImage> images, LinkGenerator routes, HttpContext context)
    {
        return new TourSummaryDto
        {
            Links =
            [
                CatalogEndpointLinks.Get(routes, context, "self", nameof(GetPublishedTour), new RouteValueDictionary { ["slug"] = tour.Slug })
            ],
            Title = tour.Title,
            Slug = tour.Slug,
            Summary = tour.Summary,
            Images = MapImages(images),
            UpdatedAt = tour.UpdatedAt
        };
    }

    private static TourDetailsDto MapPublicTourDetail(
        CatalogTourDraftReadModel tour, IReadOnlyList<PublicMediaImage> images, LinkGenerator routes, HttpContext context)
    {
        return new TourDetailsDto
        {
            Links =
            [
                CatalogEndpointLinks.Get(routes, context, "self", nameof(GetPublishedTour), new RouteValueDictionary { ["slug"] = tour.Slug }),
                CatalogEndpointLinks.Get(routes, context, "collection", nameof(GetPublishedTours))
            ],
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

    private static IReadOnlyList<PublicMediaImage> GetImages(
        IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>> imagesByTour,
        Guid tourId)
    {
        return imagesByTour.TryGetValue(tourId, out var images) ? images : [];
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
}
