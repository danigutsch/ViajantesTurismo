using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.CancelBooking;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiService;

internal static class BookingsCancelBookingEndpoint
{
    public static void MapCancelBookingEndpoint(this RouteGroupBuilder bookingsGroup)
    {
        ArgumentNullException.ThrowIfNull(bookingsGroup);

        bookingsGroup.MapPost("/{id:guid}/cancel", CancelBooking)
            .WithAdminMetadata("CancelBooking", "Cancels a booking by transitioning its status to Cancelled.", "Cancels a booking.");
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> CancelBooking(
        [FromRoute] Guid id,
        [FromServices] CancelBookingCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var command = new CancelBookingCommand(id);

        var result = await handler.Handle(command, ct);

        return await AdminEndpointResults.MatchNotFoundConflictBookingResponse<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>>(
            result,
            id,
            queryService,
            booking => TypedResults.Ok(booking),
            () => BookingErrors.BookingNotFound(id).ToNotFound(),
            () => result.ToConflict(),
            ct);
    }
}
