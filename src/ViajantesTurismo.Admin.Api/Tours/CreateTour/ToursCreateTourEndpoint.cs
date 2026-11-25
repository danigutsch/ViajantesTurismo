using SharedKernel.Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Contracts.Application;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Api.Tours.CreateTour;

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
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.TourWrite)
            .WithCreatedResourceLocation()
            .WithAdminMetadata("CreateTour", "Creates a new tour.", "Creates a new tour.");
    }

    private static async Task<Results<Created<GetTourDto>, NotFound<ProblemDetails>, ValidationProblem, Conflict<ProblemDetails>>> CreateTour(
        [FromBody] CreateTourDto tourDto,
        [FromServices] ISender sender,
        [FromServices] IQueryService queryService,
        LinkGenerator linkGenerator,
        HttpContext context,
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

        var result = await sender.Send(command, ct);

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

        if (createdTour is null)
        {
            return TourErrors.TourNotFound().ToNotFound();
        }

        var (location, links) = ToursEndpoints.GetResourceLinks(context, linkGenerator).For(tourId);
        return TypedResults.Created(location, createdTour with
        {
            Links = links
        });
    }
}
