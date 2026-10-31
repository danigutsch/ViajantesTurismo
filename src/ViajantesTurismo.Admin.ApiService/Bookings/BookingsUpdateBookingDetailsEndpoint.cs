using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PandaBear.Mediator;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDetails;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiService.Bookings;

/// <summary>Endpoint for updating the details of a booking.</summary>
internal static class BookingsUpdateBookingDetailsEndpoint
{
    /// <summary>Maps the endpoint for updating the details of a booking onto the bookings group.</summary>
    public static void MapUpdateBookingDetailsEndpoint(this RouteGroupBuilder bookingsGroup)
    {
        ArgumentNullException.ThrowIfNull(bookingsGroup);

        bookingsGroup.MapPut("/{id:guid}/details", UpdateBookingDetails)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.BookingWrite)
            .WithAdminMetadata("UpdateBookingDetails", "Updates booking details (room type, bikes, companion).", "Updates booking details.");
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>> UpdateBookingDetails(
        [FromRoute] Guid id,
        [FromBody] UpdateBookingDetailsDto dto,
        [FromServices] ISender sender,
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

        var result = await sender.Send(command, ct);

        return await AdminEndpointResults.MatchNotFoundConflictValidationBookingResponse<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>>(
            result,
            id,
            queryService,
            booking => TypedResults.Ok(booking),
            () => BookingErrors.BookingNotFound().ToNotFound(),
            () => result.ToConflict(),
            () => result.ToValidationProblem(),
            ct);
    }
}
