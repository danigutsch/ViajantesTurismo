using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Application.Tours.UpdateTour;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.ApiService;

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

        var toursGroup = app.MapGroup("/tours")
            .WithGroupName("Tours")
            .WithTags("Tours");

        toursGroup.MapPost("/", CreateTour)
            .WithName("CreateTour")
            .WithDescription("Creates a new tour.")
            .WithSummary("Creates a new tour.");

        toursGroup.MapGet("/", GetAllTours)
            .WithName("GetTours")
            .WithDescription("Retrieves all available tours.")
            .WithSummary("Retrieves all available tours.");

        toursGroup.MapGet("/{id:guid}", GetTourById)
            .WithName("GetTourById")
            .WithDescription("Retrieves a tour by its ID.")
            .WithSummary("Retrieves a tour by its ID.");

        toursGroup.MapPut("/{id:guid}", UpdateTour)
            .WithName("UpdateTour")
            .WithDescription("Updates an existing tour.")
            .WithSummary("Updates an existing tour.");
    }

    private static async Task<Results<Created<GetTourDto>, ValidationProblem, Conflict<ProblemDetails>>> CreateTour(
        [FromBody] CreateTourDto tourDto,
        [FromServices] CreateTourCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tourDto);

        var command = new CreateTourCommand(
            tourDto.Identifier,
            tourDto.Name,
            tourDto.StartDate,
            tourDto.EndDate,
            tourDto.Price,
            tourDto.SingleRoomSupplementPrice,
            tourDto.RegularBikePrice,
            tourDto.EBikePrice,
            tourDto.Currency,
            [.. tourDto.IncludedServices],
            tourDto.MinCustomers,
            tourDto.MaxCustomers);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            return result.Status switch
            {
                ResultStatus.Conflict => result.ToConflict(),
                _ => result.ToValidationProblem()
            };
        }

        var tourId = result.Value;
        var createdTour = await queryService.GetTourById(tourId, ct);

        return TypedResults.Created($"/tours/{tourId}", createdTour!);
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

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, ValidationProblem>> UpdateTour(
        Guid id,
        [FromBody] UpdateTourDto tourDto,
        [FromServices] UpdateTourCommandHandler handler,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tourDto);

        var command = new UpdateTourCommand(
            id,
            tourDto.Identifier,
            tourDto.Name,
            tourDto.StartDate,
            tourDto.EndDate,
            tourDto.Price,
            tourDto.SingleRoomSupplementPrice,
            tourDto.RegularBikePrice,
            tourDto.EBikePrice,
            TourMapper.MapToCurrency(tourDto.Currency),
            [.. tourDto.IncludedServices],
            tourDto.MinCustomers,
            tourDto.MaxCustomers);

        var result = await handler.Handle(command, ct);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToValidationProblem();
    }
}
