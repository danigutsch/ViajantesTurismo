using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.AspNet;
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

        bookingsGroup.MapPost(AdminEndpoints.Bookings.Complete.Pattern, CompleteBooking)
            .WithEndpointMetadata(AdminEndpoints.Bookings.Complete);
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>> CompleteBooking(
        [FromRoute] Guid id,
        [FromServices] CompleteBookingCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var command = new CompleteBookingCommand(id);

        var result = await handler.Handle(command, ct);

        return await AdminEndpointResults.MatchNotFoundConflictValidationBookingResponse<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>>(
            result,
            id,
            queryService,
            booking => TypedResults.Ok(booking),
            () => BookingErrors.BookingNotFound(id).ToNotFound(),
            () => result.ToConflict(),
            () => result.ToValidationProblem(),
            ct);
    }
}
