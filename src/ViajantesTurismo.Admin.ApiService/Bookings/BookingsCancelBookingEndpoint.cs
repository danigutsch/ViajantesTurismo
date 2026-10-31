using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PandaBear.Mediator;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.CancelBooking;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiService.Bookings;

/// <summary>Endpoint for cancelling a booking.</summary>
internal static class BookingsCancelBookingEndpoint
{
    /// <summary>Maps the endpoint for cancelling a booking onto the bookings group.</summary>
    public static void MapCancelBookingEndpoint(this RouteGroupBuilder bookingsGroup)
    {
        ArgumentNullException.ThrowIfNull(bookingsGroup);

        bookingsGroup.MapPost("/{id:guid}/cancel", CancelBooking)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.BookingWrite)
            .WithAdminMetadata("CancelBooking", "Cancels a booking by transitioning its status to Cancelled.", "Cancels a booking.");
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> CancelBooking(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var command = new CancelBookingCommand(id);

        var result = await sender.Send(command, ct);

        return await AdminEndpointResults.MatchNotFoundConflictBookingResponse<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>>(
            result,
            id,
            queryService,
            booking => TypedResults.Ok(booking),
            () => BookingErrors.BookingNotFound().ToNotFound(),
            () => result.ToConflict(),
            ct);
    }
}
