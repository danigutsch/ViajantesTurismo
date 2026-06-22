using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.AspNet;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.CreateBooking;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiService;

/// <summary>
/// Maps the booking creation endpoint as its own vertical slice.
/// </summary>
internal static class BookingsCreateBookingEndpoint
{
    /// <summary>
    /// Maps the booking creation endpoint to the bookings route group.
    /// </summary>
    /// <param name="bookingsGroup">The bookings route group.</param>
    public static void MapCreateBookingEndpoint(this RouteGroupBuilder bookingsGroup)
    {
        ArgumentNullException.ThrowIfNull(bookingsGroup);

        bookingsGroup.MapPost(AdminEndpoints.Bookings.Create.Pattern, CreateBooking)
            .WithEndpointMetadata(AdminEndpoints.Bookings.Create);
    }

    private static async Task<Results<Created<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>> CreateBooking(
        [FromBody] CreateBookingDto dto,
        [FromServices] CreateBookingCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var command = new CreateBookingCommand(
            dto.TourId,
            dto.PrincipalCustomerId,
            dto.PrincipalBikeType,
            dto.CompanionCustomerId,
            dto.CompanionBikeType,
            dto.RoomType,
            dto.DiscountType,
            dto.DiscountAmount,
            dto.DiscountReason,
            dto.Notes);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            var failure = result.ConvertError();
            return AdminEndpointResults.MatchNotFoundConflictValidationFailure<Results<Created<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>>(
                failure,
                () => failure.ToNotFound(),
                () => failure.ToConflict(),
                () => failure.ToValidationProblem());
        }

        return await AdminEndpointResults.GetBookingResponse<Results<Created<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>>(
            result.Value,
            queryService,
            booking => TypedResults.Created($"/bookings/{result.Value}", booking),
            bookingId => BookingErrors.BookingNotFound(bookingId).ToNotFound(),
            ct);
    }
}
