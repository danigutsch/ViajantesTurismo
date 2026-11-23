using ViajantesTurismo.Admin.Api.Tours.CreateTour;
using ViajantesTurismo.Admin.Api.Tours.DeleteTour;
using ViajantesTurismo.Admin.Api.Tours.UpdateTour;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Api.Tours;

/// <summary>
/// Defines all endpoints related to tour management.
/// </summary>
internal static class ToursEndpoints
{
    private const string ListEndpoint = nameof(GetTours);
    private const string ItemEndpoint = nameof(GetTourById);

    /// <summary>
    /// Maps all tour-related endpoints to the application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    /// <returns>The web application for chaining.</returns>
    public static void MapToursEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var toursGroup = app.MapToursGroup();

        toursGroup.MapCreateTourEndpoint();

        toursGroup.MapGet("/", GetTours)
            .RequireAuthorization(AdminAuthorization.TourRead)
            .WithAdminMetadata(ListEndpoint, "Retrieves all available tours.", "Retrieves all available tours.");

        toursGroup.MapGet("/page", GetTourPage)
            .RequireAuthorization(AdminAuthorization.TourRead)
            .WithAdminMetadata(nameof(GetTourPage), "Retrieves a page of active tours.",
                "Retrieves bounded tour summaries. The optional page number is one-based.");

        toursGroup.MapPost("/options/search", SearchTourOptions)
            .RequireAuthorization(AdminAuthorization.TourRead)
            .WithAdminMetadata(nameof(SearchTourOptions), "Searches active tour choices.",
                "Searches for a bounded set of active tour choices by name or identifier.");

        toursGroup.MapGet("/{id:guid}", GetTourById)
            .RequireAuthorization(AdminAuthorization.TourRead)
            .WithAdminMetadata(ItemEndpoint, "Retrieves a tour by its ID.", "Retrieves a tour by its ID.");

        toursGroup.MapUpdateTourEndpoint();
        toursGroup.MapDeleteTourEndpoint();
    }

    private static async Task<Ok<IReadOnlyList<GetTourDto>>> GetTours(
        [FromServices] IQueryService queryService,
        LinkGenerator linkGenerator,
        HttpContext context,
        CancellationToken ct)
    {
        var allTours = await queryService.GetAllTours(ct);
        var links = GetResourceLinks(context, linkGenerator);
        return TypedResults.Ok<IReadOnlyList<GetTourDto>>(
            [.. allTours.Select(tour => tour with { Links = links.For(tour.Id).Links })]);
    }

    private static async Task<Results<Ok<GetTourPageDto>, BadRequest>> GetTourPage(
        [FromQuery] string? page,
        [FromQuery] string? sort,
        [FromQuery] bool? descending,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var sortKey = TourPageSort.Default;
        if (sort is not null && (!Enum.TryParse(sort, ignoreCase: true, out sortKey) || !Enum.IsDefined(sortKey)))
        {
            return TypedResults.BadRequest();
        }

        var tours = await queryService.GetTourPage(page, sortKey, descending.GetValueOrDefault(), ct);
        return TypedResults.Ok(tours);
    }

    private static async Task<Results<Ok<GetTourOptionsDto>, BadRequest>> SearchTourOptions(
        [FromBody] TourOptionSearchDto request,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        if (request.Query is { Length: > ContractConstants.MaxTourOptionQueryLength } || request.Limit <= 0)
        {
            return TypedResults.BadRequest();
        }

        var options = await queryService.SearchTourOptions(request, ct);
        return TypedResults.Ok(options);
    }

    private static async Task<Results<Ok<GetTourDto>, NotFound<ProblemDetails>>> GetTourById(
        [FromRoute] Guid id,
        [FromServices] IQueryService queryService,
        LinkGenerator linkGenerator,
        HttpContext context,
        CancellationToken ct)
    {
        var tourDto = await queryService.GetActiveTourById(id, ct);

        return tourDto is null
            ? TourErrors.TourNotFound().ToNotFound()
            : TypedResults.Ok(tourDto with
            {
                Links = GetResourceLinks(context, linkGenerator).For(id).Links
            });
    }

    /// <summary>Creates the link factory for tour representations.</summary>
    internal static AdminResourceLinks GetResourceLinks(HttpContext context, LinkGenerator linkGenerator) =>
        new(context, linkGenerator, ItemEndpoint, ListEndpoint);
}
