using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiService.Tours;

/// <summary>
/// Defines all endpoints related to tour management.
/// </summary>
internal static class ToursEndpoints
{
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

        toursGroup.MapGet("/", GetAllTours)
            .WithAdminMetadata("GetTours", "Retrieves all available tours.", "Retrieves all available tours.");

        toursGroup.MapGet("/{id:guid}", GetTourById)
            .WithAdminMetadata("GetTourById", "Retrieves a tour by its ID.", "Retrieves a tour by its ID.");

        toursGroup.MapUpdateTourEndpoint();
    }

    private static async Task<Ok<IReadOnlyList<GetTourDto>>> GetAllTours(
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var allTours = await queryService.GetAllTours(ct);
        return TypedResults.Ok(allTours);
    }

    private static async Task<Results<Ok<GetTourDto>, NotFound<ProblemDetails>>> GetTourById(
        [FromRoute] Guid id,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var tourDto = await queryService.GetTourById(id, ct);

        return tourDto is null
            ? TourErrors.TourNotFound(id).ToNotFound()
            : TypedResults.Ok(tourDto);
    }

}
