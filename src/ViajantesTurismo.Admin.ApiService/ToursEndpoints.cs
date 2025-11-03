using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.ApiService.Mapping;
using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.AdminApi.Contracts;

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
    public static WebApplication MapToursEndpoints(this WebApplication app)
    {
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

        toursGroup.MapGet("/{id:int}", GetTourById)
            .WithName("GetTourById")
            .WithDescription("Retrieves a tour by its ID.")
            .WithSummary("Retrieves a tour by its ID.");

        toursGroup.MapPut("/{id:int}", UpdateTour)
            .WithName("UpdateTour")
            .WithDescription("Updates an existing tour.")
            .WithSummary("Updates an existing tour.");

        return app;
    }

    private static async Task<Results<Created<Tour>, ValidationProblem>> CreateTour(
        [FromBody] CreateTourDto tourDto,
        [FromServices] ITourStore tourStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var currency = TourMapper.MapToCurrency(tourDto.Currency);

        var result = Tour.Create(
            tourDto.Identifier,
            tourDto.Name,
            tourDto.StartDate,
            tourDto.EndDate,
            tourDto.Price,
            tourDto.DoubleRoomSupplementPrice,
            tourDto.RegularBikePrice,
            tourDto.EBikePrice,
            currency,
            tourDto.IncludedServices);

        if (result.IsFailure)
        {
            return result.ToValidationProblem();
        }

        var tour = result.Value;
        tourStore.Add(tour);

        await unitOfWork.SaveEntities(ct);

        return TypedResults.Created($"/tours/{tour.Id}", tour);
    }

    private static async Task<Ok<IReadOnlyList<GetTourDto>>> GetAllTours(
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var allTours = await queryService.GetAllTours(ct);
        return TypedResults.Ok(allTours);
    }

    private static async Task<Results<Ok<GetTourDto>, NotFound<ProblemDetails>>> GetTourById(
        [FromRoute] int id,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var tourDto = await queryService.GetTourById(id, ct);
        if (tourDto is null)
        {
            return TourErrors.TourNotFound(id).ToNotFound();
        }

        return TypedResults.Ok(tourDto);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, ValidationProblem>> UpdateTour(
        int id,
        [FromBody] UpdateTourDto tourDto,
        [FromServices] ITourStore tourStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var tour = await tourStore.GetById(id, ct);
        if (tour is null)
        {
            return TourErrors.TourNotFound(id).ToNotFound();
        }

        var currency = TourMapper.MapToCurrency(tourDto.Currency);

        var basicInfoResult = tour.UpdateDetails(tourDto.Identifier, tourDto.Name);
        if (basicInfoResult.IsFailure)
        {
            return basicInfoResult.ToValidationProblem();
        }

        var scheduleResult = tour.UpdateSchedule(tourDto.StartDate, tourDto.EndDate);
        if (scheduleResult.IsFailure)
        {
            return basicInfoResult.ToValidationProblem();
        }

        var pricingResult = tour.UpdatePricing(
            tourDto.DoubleRoomSupplementPrice,
            tourDto.RegularBikePrice,
            tourDto.EBikePrice,
            currency);
        if (pricingResult.IsFailure)
        {
            return basicInfoResult.ToValidationProblem();
        }

        var basePriceResult = tour.UpdateBasePrice(tourDto.Price);
        if (basePriceResult.IsFailure)
        {
            return basicInfoResult.ToValidationProblem();
        }

        tour.UpdateIncludedServices([.. tourDto.IncludedServices]);

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }
}