using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.AspNet;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDiscount;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiService;

internal static class BookingsUpdateBookingDiscountEndpoint
{
    public static void MapUpdateBookingDiscountEndpoint(this RouteGroupBuilder bookingsGroup)
    {
        ArgumentNullException.ThrowIfNull(bookingsGroup);

        bookingsGroup.MapPut(AdminEndpoints.Bookings.UpdateDiscount.Pattern, UpdateBookingDiscount)
            .WithEndpointMetadata(AdminEndpoints.Bookings.UpdateDiscount);
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>> UpdateBookingDiscount(
        [FromRoute] Guid id,
        [FromBody] UpdateBookingDiscountDto dto,
        [FromServices] UpdateBookingDiscountCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var command = new UpdateBookingDiscountCommand(
            id,
            dto.DiscountType,
            dto.DiscountAmount,
            dto.DiscountReason);

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
