using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.ApiService;

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
    public static WebApplication MapBookingEndpoints(this WebApplication app)
    {
        var bookingsGroup = app.MapGroup("/bookings")
            .WithGroupName("Bookings")
            .WithTags("Bookings");

        bookingsGroup.MapGet("/", GetAllBookings)
            .WithName("GetBookings")
            .WithDescription("Retrieves all bookings.")
            .WithSummary("Retrieves all bookings.");

        bookingsGroup.MapGet("/{id:long}", GetBookingById)
            .WithName("GetBookingById")
            .WithDescription("Retrieves a booking by its ID.")
            .WithSummary("Retrieves a booking by its ID.");

        bookingsGroup.MapGet("/tour/{tourId:int}", GetBookingsByTourId)
            .WithName("GetBookingsByTourId")
            .WithDescription("Retrieves all bookings for a specific tour.")
            .WithSummary("Retrieves all bookings for a specific tour.");

        bookingsGroup.MapGet("/customer/{customerId:int}", GetBookingsByCustomerId)
            .WithName("GetBookingsByCustomerId")
            .WithDescription("Retrieves all bookings for a specific customer (as primary or companion).")
            .WithSummary("Retrieves all bookings for a specific customer.");

        bookingsGroup.MapPost("/", CreateBooking)
            .WithName("CreateBooking")
            .WithDescription("Creates a new booking for a tour.")
            .WithSummary("Creates a new booking.");

        bookingsGroup.MapPut("/{id:long}", UpdateBooking)
            .WithName("UpdateBooking")
            .WithDescription("Updates an existing booking.")
            .WithSummary("Updates an existing booking.");

        bookingsGroup.MapDelete("/{id:long}", DeleteBooking)
            .WithName("DeleteBooking")
            .WithDescription("Deletes a booking.")
            .WithSummary("Deletes a booking.");

        bookingsGroup.MapPatch("/{id:long}/cancel", CancelBooking)
            .WithName("CancelBooking")
            .WithDescription("Cancels a booking by setting its status to Cancelled.")
            .WithSummary("Cancels a booking.");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<GetBookingDto>>> GetAllBookings(
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var bookings = await queryService.GetAllBookings(ct);
        return TypedResults.Ok(bookings);
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound>> GetBookingById(
        [FromRoute] long id,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var booking = await queryService.GetBookingById(id, ct);
        if (booking is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(booking);
    }

    private static async Task<Ok<IReadOnlyList<GetBookingDto>>> GetBookingsByTourId(
        [FromRoute] int tourId,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var bookings = await queryService.GetBookingsByTourId(tourId, ct);
        return TypedResults.Ok(bookings);
    }

    private static async Task<Ok<IReadOnlyList<GetBookingDto>>> GetBookingsByCustomerId(
        [FromRoute] int customerId,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var bookings = await queryService.GetBookingsByCustomerId(customerId, ct);
        return TypedResults.Ok(bookings);
    }

    private static async Task<Results<Created<GetBookingDto>, NotFound, ValidationProblem>> CreateBooking(
        [FromBody] CreateBookingDto dto,
        [FromServices] ITourStore tourStore,
        [FromServices] IQueryService queryService,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var tour = await tourStore.GetById(dto.TourId, ct);
        if (tour is null)
        {
            return TypedResults.NotFound();
        }

        var booking = tour.AddBooking(
            dto.CustomerId,
            dto.CompanionId,
            dto.TotalPrice,
            dto.Notes);

        await unitOfWork.SaveEntities(ct);

        var bookingDto = await queryService.GetBookingById(booking.Id, ct);
        if (bookingDto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Created($"/bookings/{booking.Id}", bookingDto);
    }

    private static async Task<Results<NoContent, NotFound>> UpdateBooking(
        [FromRoute] long id,
        [FromBody] UpdateBookingDto dto,
        [FromServices] ITourStore tourStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var tour = await tourStore.GetByBookingId(id, ct);
        if (tour is null)
        {
            return TypedResults.NotFound();
        }

        tour.UpdateBooking(
            id,
            dto.TotalPrice,
            dto.Notes,
            (BookingStatus)dto.Status,
            (PaymentStatus)dto.PaymentStatus);

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> DeleteBooking(
        [FromRoute] long id,
        [FromServices] ITourStore tourStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var tour = await tourStore.GetByBookingId(id, ct);
        if (tour is null)
        {
            return TypedResults.NotFound();
        }

        tour.RemoveBooking(id);

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> CancelBooking(
        [FromRoute] long id,
        [FromServices] ITourStore tourStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var tour = await tourStore.GetByBookingId(id, ct);
        if (tour is null)
        {
            return TypedResults.NotFound();
        }

        tour.CancelBooking(id);

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }
}
