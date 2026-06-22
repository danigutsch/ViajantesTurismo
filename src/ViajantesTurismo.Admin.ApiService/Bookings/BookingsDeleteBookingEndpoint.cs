using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.AspNet;
using ViajantesTurismo.Admin.Application.Bookings.DeleteBooking;

namespace ViajantesTurismo.Admin.ApiService;

internal static class BookingsDeleteBookingEndpoint
{
    public static void MapDeleteBookingEndpoint(this RouteGroupBuilder bookingsGroup)
    {
        ArgumentNullException.ThrowIfNull(bookingsGroup);

        bookingsGroup.MapDelete(AdminEndpoints.Bookings.Delete.Pattern, DeleteBooking)
            .WithEndpointMetadata(AdminEndpoints.Bookings.Delete);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>> DeleteBooking(
        [FromRoute] Guid id,
        [FromServices] DeleteBookingCommandHandler handler,
        CancellationToken ct)
    {
        var command = new DeleteBookingCommand(id);

        var result = await handler.Handle(command, ct);

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
