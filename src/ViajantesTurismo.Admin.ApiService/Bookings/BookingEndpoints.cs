using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiService.Bookings;

/// <summary>
/// Defines all endpoints related to booking management.
/// </summary>
internal static class BookingEndpoints
{
    /// <summary>
    /// Maps all booking-related endpoints to the application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    /// <returns>The web application for chaining.</returns>
    public static void MapBookingEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var bookingsGroup = app.MapBookingsGroup();

        bookingsGroup.MapGet("/", GetAllBookings)
            .WithAdminMetadata("GetBookings", "Retrieves all bookings.", "Retrieves all bookings.");

        bookingsGroup.MapGet("/{id:guid}", GetBookingById)
            .WithAdminMetadata("GetBookingById", "Retrieves a booking by its ID.", "Retrieves a booking by its ID.");

        bookingsGroup.MapGet("/tour/{tourId:guid}", GetBookingsByTourId)
            .WithAdminMetadata("GetBookingsByTourId", "Retrieves all bookings for a specific tour.", "Retrieves all bookings for a specific tour.");

        bookingsGroup.MapGet("/customer/{customerId:guid}", GetBookingsByCustomerId)
            .WithAdminMetadata("GetBookingsByCustomerId", "Retrieves all bookings for a specific customer (as primary or companion).", "Retrieves all bookings for a specific customer.");

        bookingsGroup.MapCreateBookingEndpoint();

        bookingsGroup.MapUpdateBookingDiscountEndpoint();
        bookingsGroup.MapUpdateBookingDetailsEndpoint();
        bookingsGroup.MapDeleteBookingEndpoint();
        bookingsGroup.MapCancelBookingEndpoint();
        bookingsGroup.MapConfirmBookingEndpoint();
        bookingsGroup.MapUpdateBookingNotesEndpoint();
        bookingsGroup.MapCompleteBookingEndpoint();
        bookingsGroup.MapRecordPaymentEndpoint();
    }

    private static async Task<Ok<IReadOnlyList<GetBookingDto>>> GetAllBookings(
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var bookings = await queryService.GetAllBookings(ct);
        return TypedResults.Ok(bookings);
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>>> GetBookingById(
        [FromRoute] Guid id,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var booking = await queryService.GetBookingById(id, ct);
        if (booking is null)
        {
            return BookingErrors.BookingNotFound(id).ToNotFound();
        }

        return TypedResults.Ok(booking);
    }

    private static async Task<Ok<IReadOnlyList<GetBookingDto>>> GetBookingsByTourId(
        [FromRoute] Guid tourId,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var bookings = await queryService.GetBookingsByTourId(tourId, ct);
        return TypedResults.Ok(bookings);
    }

    private static async Task<Ok<IReadOnlyList<GetBookingDto>>> GetBookingsByCustomerId(
        [FromRoute] Guid customerId,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var bookings = await queryService.GetBookingsByCustomerId(customerId, ct);
        return TypedResults.Ok(bookings);
    }

}
