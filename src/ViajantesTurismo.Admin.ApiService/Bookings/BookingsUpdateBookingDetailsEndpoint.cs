using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDetails;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiService;

internal static class BookingsUpdateBookingDetailsEndpoint
{
    public static void MapUpdateBookingDetailsEndpoint(this RouteGroupBuilder bookingsGroup)
    {
        ArgumentNullException.ThrowIfNull(bookingsGroup);

        bookingsGroup.MapPut("/{id:guid}/details", UpdateBookingDetails)
            .WithAdminMetadata("UpdateBookingDetails", "Updates booking details (room type, bikes, companion).", "Updates booking details.");
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>> UpdateBookingDetails(
        [FromRoute] Guid id,
        [FromBody] UpdateBookingDetailsDto dto,
        [FromServices] UpdateBookingDetailsCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var command = new UpdateBookingDetailsCommand(
            id,
            dto.RoomType,
            dto.PrincipalBikeType,
            dto.CompanionCustomerId,
            dto.CompanionBikeType);

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
