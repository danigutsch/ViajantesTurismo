using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiService.Bookings;

internal static class BookingsConfirmBookingEndpoint
{
    public static void MapConfirmBookingEndpoint(this RouteGroupBuilder bookingsGroup)
    {
        ArgumentNullException.ThrowIfNull(bookingsGroup);

        bookingsGroup.MapPost("/{id:guid}/confirm", ConfirmBooking)
            .WithAdminMetadata("ConfirmBooking", "Confirms a booking by transitioning its status to Confirmed.", "Confirms a booking.");
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ConfirmBooking(
        [FromRoute] Guid id,
        [FromServices] ConfirmBookingCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var command = new ConfirmBookingCommand(id);

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
