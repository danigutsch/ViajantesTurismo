using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PandaBear.Mediator;
using ViajantesTurismo.Admin.Application.Bookings.DeleteBooking;

namespace ViajantesTurismo.Admin.ApiService.Bookings;

/// <summary>Endpoint for deleting a booking.</summary>
internal static class BookingsDeleteBookingEndpoint
{
    /// <summary>Maps the endpoint for deleting a booking onto the bookings group.</summary>
    public static void MapDeleteBookingEndpoint(this RouteGroupBuilder bookingsGroup)
    {
        ArgumentNullException.ThrowIfNull(bookingsGroup);

        bookingsGroup.MapDelete("/{id:guid}", DeleteBooking)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.BookingDelete)
            .WithAdminMetadata("DeleteBooking", "Deletes a booking.", "Deletes a booking.");
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>> DeleteBooking(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        var command = new DeleteBookingCommand(id);

        var result = await sender.Send(command, ct);

        if (result.IsFailure)
        {
            return AdminEndpointResults.MatchNotFoundConflictValidationFailure<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>>(
                result,
                () => result.ToNotFound(),
                () => result.ToConflict(),
                () => result.ToValidationProblem());
        }

        return TypedResults.NoContent();
    }
}
