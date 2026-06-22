using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.AspNet;
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

        bookingsGroup.MapPut(AdminEndpoints.Bookings.UpdateDetails.Pattern, UpdateBookingDetails)
            .WithEndpointMetadata(AdminEndpoints.Bookings.UpdateDetails);
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
