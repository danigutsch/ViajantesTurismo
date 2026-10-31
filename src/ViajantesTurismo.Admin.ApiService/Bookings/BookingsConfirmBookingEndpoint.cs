using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PandaBear.Mediator;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiService.Bookings;

/// <summary>Endpoint for confirming a booking.</summary>
internal static class BookingsConfirmBookingEndpoint
{
    /// <summary>Maps the endpoint for confirming a booking onto the bookings group.</summary>
    public static void MapConfirmBookingEndpoint(this RouteGroupBuilder bookingsGroup)
    {
        ArgumentNullException.ThrowIfNull(bookingsGroup);

        bookingsGroup.MapPost("/{id:guid}/confirm", ConfirmBooking)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.BookingWrite)
            .WithAdminMetadata("ConfirmBooking", "Confirms a booking by transitioning its status to Confirmed.", "Confirms a booking.");
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ConfirmBooking(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var command = new ConfirmBookingCommand(id);

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
