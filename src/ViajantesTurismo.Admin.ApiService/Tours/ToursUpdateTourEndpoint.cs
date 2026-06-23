using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Application.Tours.UpdateTour;
using ViajantesTurismo.Admin.Contracts;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.ApiService.Tours;

/// <summary>
/// Maps the tour update endpoint as its own vertical slice.
/// </summary>
internal static class ToursUpdateTourEndpoint
{
    /// <summary>
    /// Maps the tour update endpoint to the tours route group.
    /// </summary>
    /// <param name="toursGroup">The tours route group.</param>
    public static void MapUpdateTourEndpoint(this RouteGroupBuilder toursGroup)
    {
        ArgumentNullException.ThrowIfNull(toursGroup);

        toursGroup.MapPut("/{id:guid}", UpdateTour)
            .WithAdminMetadata("UpdateTour", "Updates an existing tour.", "Updates an existing tour.");
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, ValidationProblem, Conflict<ProblemDetails>>> UpdateTour(
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
            : result.Status switch
            {
                ResultStatus.NotFound => result.ToNotFound(),
                ResultStatus.Conflict => result.ToConflict(),
                ResultStatus.Invalid => result.ToValidationProblem(),
                _ => throw new InvalidOperationException($"Unsupported result status '{result.Status}' for update tour endpoint.")
            };
    }
}
