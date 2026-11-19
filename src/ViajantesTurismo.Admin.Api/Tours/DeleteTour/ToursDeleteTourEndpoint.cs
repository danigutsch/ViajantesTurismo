using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PandaBear.AspNetCore.Authentication;
using PandaBear.HttpCaching.AspNetCore;
using PandaBear.Mediator;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Tours.DeleteTour;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Api.Tours.DeleteTour;

/// <summary>Exposes retirement and the current Catalog reconciliation status.</summary>
internal static class ToursDeleteTourEndpoint
{
    /// <summary>Maps the retirement routes into the Admin tours group.</summary>
    public static void MapDeleteTourEndpoint(this RouteGroupBuilder toursGroup)
    {
        ArgumentNullException.ThrowIfNull(toursGroup);

        toursGroup.MapDelete("/{id:guid}", DeleteTour)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.TourDelete)
            .Produces(StatusCodes.Status429TooManyRequests)
            .WithAdminMetadata("DeleteTour", "Retires a tour and preserves its booking history.", "Retires a tour.");

        toursGroup.MapGet("/{id:guid}/retirement", GetTourRetirement)
            .RequireAuthorization(AdminAuthorization.TourDelete)
            .WithAdminMetadata("GetTourRetirement", "Retrieves the current tour retirement status.", "Retrieves tour retirement status.");
    }

    private static async Task<Results<Accepted, NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>, UnauthorizedHttpResult>> DeleteTour(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        [FromServices] IQueryService queryService,
        [FromServices] TimeProvider timeProvider,
        HttpContext context,
        CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await sender.Send(new DeleteTourCommand(id, actorId, timeProvider.GetUtcNow()), ct);
        if (result.IsFailure)
        {
            return AdminEndpointResults.MatchNotFoundConflictValidationFailure<Results<Accepted, NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>, UnauthorizedHttpResult>>(
                result,
                () => result.ToNotFound(),
                () => result.ToConflict(),
                () => TypedResults.Unauthorized());
        }

        var retirement = await queryService.GetTourRetirement(id, ct);
        return retirement?.CompletedAt is not null
            ? TypedResults.NoContent()
            : TypedResults.Accepted(new Uri($"/api/v1/tours/{id}/retirement", UriKind.Relative));
    }

    private static async Task<Results<Ok<GetTourRetirementDto>, NotFound<ProblemDetails>>> GetTourRetirement(
        [FromRoute] Guid id,
        [FromServices] IQueryService queryService,
        HttpContext context,
        CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(context);
        var retirement = await queryService.GetTourRetirement(id, ct);
        return retirement is null
            ? TourErrors.TourNotFound().ToNotFound()
            : TypedResults.Ok(retirement);
    }
}
