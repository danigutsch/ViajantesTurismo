using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Contracts;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.ApiService.Tours;

/// <summary>
/// Maps the tour creation endpoint as its own vertical slice.
/// </summary>
internal static class ToursCreateTourEndpoint
{
    /// <summary>
    /// Maps the tour creation endpoint to the tours route group.
    /// </summary>
    /// <param name="toursGroup">The tours route group.</param>
    public static void MapCreateTourEndpoint(this RouteGroupBuilder toursGroup)
    {
        ArgumentNullException.ThrowIfNull(toursGroup);

        toursGroup.MapPost("/", CreateTour)
            .WithAdminMetadata("CreateTour", "Creates a new tour.", "Creates a new tour.");
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
}
