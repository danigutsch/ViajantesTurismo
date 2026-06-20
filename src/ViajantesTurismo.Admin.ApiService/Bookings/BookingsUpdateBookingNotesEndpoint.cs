using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingNotes;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiService;

internal static class BookingsUpdateBookingNotesEndpoint
{
    public static void MapUpdateBookingNotesEndpoint(this RouteGroupBuilder bookingsGroup)
    {
        ArgumentNullException.ThrowIfNull(bookingsGroup);

        bookingsGroup.MapPatch("/{id:guid}/notes", UpdateBookingNotes)
            .WithAdminMetadata("UpdateBookingNotes", "Updates the notes of a booking.", "Updates booking notes.");
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, ValidationProblem>> UpdateBookingNotes(
        [FromRoute] Guid id,
        [FromBody] UpdateBookingNotesDto dto,
        [FromServices] UpdateBookingNotesCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var command = new UpdateBookingNotesCommand(id, dto.Notes);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            return AdminEndpointResults.MatchNotFoundValidationFailure<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, ValidationProblem>>(
                result,
                () => result.ToNotFound(),
                () => result.ToValidationProblem());
        }

        var updatedBooking = await queryService.GetBookingById(id, ct);

        if (updatedBooking is null)
        {
            return BookingErrors.BookingNotFound(id).ToNotFound();
        }

        return TypedResults.Ok(updatedBooking);
    }
}
