using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.CompleteBooking;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiService;

internal static class BookingsCompleteBookingEndpoint
{
    public static void MapCompleteBookingEndpoint(this RouteGroupBuilder bookingsGroup)
    {
        ArgumentNullException.ThrowIfNull(bookingsGroup);

        bookingsGroup.MapPost("/{id:guid}/complete", CompleteBooking)
            .WithAdminMetadata("CompleteBooking", "Completes a booking by transitioning its status to Completed.", "Completes a booking.");
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>> CompleteBooking(
        [FromRoute] Guid id,
        [FromServices] CompleteBookingCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var command = new CompleteBookingCommand(id);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            return AdminEndpointResults.MatchNotFoundConflictValidationFailure<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>>(
                result,
                () => result.ToNotFound(),
                () => result.ToConflict(),
                () => result.ToValidationProblem());
        }

        return await AdminEndpointResults.GetBookingResponse<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>>(
            id,
            queryService,
            booking => TypedResults.Ok(booking),
            bookingId => BookingErrors.BookingNotFound(bookingId).ToNotFound(),
            ct);
    }
}
