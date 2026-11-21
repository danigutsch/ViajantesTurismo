using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PandaBear.Mediator;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.CreateBooking;
using ViajantesTurismo.Admin.Contracts.Application;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Api.Bookings.CreateBooking;

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

        bookingsGroup.MapPost("/", CreateBooking)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.BookingWrite)
            .WithCreatedResourceLocation()
            .WithAdminMetadata("CreateBooking", "Creates a new booking for a tour.", "Creates a new booking.");
    }

    private static async Task<Results<Created<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>> CreateBooking(
        [FromBody] CreateBookingDto dto,
        [FromServices] ISender sender,
        [FromServices] IQueryService queryService,
        LinkGenerator linkGenerator,
        HttpContext context,
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

        var result = await sender.Send(command, ct);

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
            booking =>
            {
                var (location, links) = BookingEndpoints.GetResourceLinks(context, linkGenerator).For(booking.Id);
                return TypedResults.Created(location, booking with
                {
                    Links = links
                });
            },
            bookingId => BookingErrors.BookingNotFound().ToNotFound(),
            ct);
    }
}
