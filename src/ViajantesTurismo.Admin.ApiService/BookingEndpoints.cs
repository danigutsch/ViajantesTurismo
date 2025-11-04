using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.ApiService;

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

        bookingsGroup.MapPut("/{id:long}/details", UpdateBookingDetails)
            .WithName("UpdateBookingDetails")
            .WithDescription("Updates booking details (room type, bikes, companion).")
            .WithSummary("Updates booking details.");

        bookingsGroup.MapDelete("/{id:long}", DeleteBooking)
            .WithName("DeleteBooking")
            .WithDescription("Deletes a booking.")
            .WithSummary("Deletes a booking.");

        bookingsGroup.MapPatch("/{id:long}/cancel", CancelBooking)
            .WithName("CancelBooking")
            .WithDescription("Cancels a booking by setting its status to Cancelled.")
            .WithSummary("Cancels a booking.");

        bookingsGroup.MapPatch("/{id:long}/confirm", ConfirmBooking)
            .WithName("ConfirmBooking")
            .WithDescription("Confirms a booking by setting its status to Confirmed.")
            .WithSummary("Confirms a booking.");

        bookingsGroup.MapPatch("/{id:long}/notes", UpdateBookingNotes)
            .WithName("UpdateBookingNotes")
            .WithDescription("Updates the notes of a booking.")
            .WithSummary("Updates booking notes.");

        bookingsGroup.MapPatch("/{id:long}/complete", CompleteBooking)
            .WithName("CompleteBooking")
            .WithDescription("Completes a booking by setting its status to Completed.")
            .WithSummary("Completes a booking.");

        bookingsGroup.MapPost("/{id:long}/payments", RecordPayment)
            .WithName("RecordPayment")
            .WithDescription("Records a payment for a booking.")
            .WithSummary("Records a payment.");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<GetBookingDto>>> GetAllBookings(
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var bookings = await queryService.GetAllBookings(ct);
        return TypedResults.Ok(bookings);
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>>> GetBookingById(
        [FromRoute] long id,
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

        var result = tour.AddBooking(
            dto.PrincipalCustomerId,
            BookingMapper.MapToBikeType(dto.PrincipalBikeType),
            dto.CompanionCustomerId,
            dto.CompanionBikeType.HasValue ? BookingMapper.MapToBikeType(dto.CompanionBikeType.Value) : null,
            BookingMapper.MapToRoomType(dto.RoomType),
            BookingMapper.MapToDiscountType(dto.DiscountType),
            dto.DiscountAmount,
            dto.DiscountReason,
            dto.Notes);

        if (result.IsFailure)
        {
            return result.ToValidationProblem();
        }

        var booking = result.Value;

        await unitOfWork.SaveEntities(ct);

        var bookingDto = await queryService.GetBookingById(booking.Id, ct);
        if (bookingDto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Created($"/bookings/{booking.Id}", bookingDto);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> UpdateBooking(
        [FromRoute] long id,
        [FromBody] UpdateBookingDto dto,
        [FromServices] ITourStore tourStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var tour = await tourStore.GetByBookingId(id, ct);
        if (tour is null)
        {
            return BookingErrors.BookingNotFound(id).ToNotFound();
        }

        tour.UpdateBookingNotes(id, dto.Notes);

        var targetStatus = BookingMapper.MapToBookingStatus(dto.Status);
        var booking = tour.Bookings.FirstOrDefault(b => b.Id == id);
        if (booking is not null && booking.Status != targetStatus)
        {
            var statusUpdateResult = targetStatus switch
            {
                BookingStatus.Confirmed => tour.ConfirmBooking(id),
                BookingStatus.Cancelled => tour.CancelBooking(id),
                BookingStatus.Completed => tour.CompleteBooking(id),
                _ => Result.Ok()
            };

            if (statusUpdateResult.IsFailure)
            {
                return statusUpdateResult.ToNotFound();
            }
        }

        var paymentUpdateResult = tour.UpdateBookingPaymentStatus(id, BookingMapper.MapToPaymentStatus(dto.PaymentStatus));
        if (paymentUpdateResult.IsFailure)
        {
            paymentUpdateResult.ToNotFound();
        }

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> DeleteBooking(
        [FromRoute] long id,
        [FromServices] ITourStore tourStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var tour = await tourStore.GetByBookingId(id, ct);
        if (tour is null)
        {
            return TourErrors.BookingNotFound(id).ToNotFound();
        }

        var result = tour.RemoveBooking(id);
        if (!result.IsSuccess)
        {
            return result.ToNotFound();
        }

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> CancelBooking(
        [FromRoute] long id,
        [FromServices] ITourStore tourStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var tour = await tourStore.GetByBookingId(id, ct);
        if (tour is null)
        {
            return BookingErrors.BookingNotFound(id).ToNotFound();
        }

        var result = tour.CancelBooking(id);
        if (result.IsFailure)
        {
            return result.ToNotFound();
        }

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> ConfirmBooking(
        [FromRoute] long id,
        [FromServices] ITourStore tourStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var tour = await tourStore.GetByBookingId(id, ct);
        if (tour is null)
        {
            return BookingErrors.BookingNotFound(id).ToNotFound();
        }

        var result = tour.ConfirmBooking(id);
        if (result.IsFailure)
        {
            return result.ToNotFound();
        }

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> UpdateBookingNotes(
        [FromRoute] long id,
        [FromBody] UpdateBookingNotesDto dto,
        [FromServices] ITourStore tourStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var tour = await tourStore.GetByBookingId(id, ct);
        if (tour is null)
        {
            return BookingErrors.BookingNotFound(id).ToNotFound();
        }

        tour.UpdateBookingNotes(id, dto.Notes);

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> CompleteBooking(
        [FromRoute] long id,
        [FromServices] ITourStore tourStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var tour = await tourStore.GetByBookingId(id, ct);
        if (tour is null)
        {
            return BookingErrors.BookingNotFound(id).ToNotFound();
        }

        var result = tour.CompleteBooking(id);
        if (result.IsFailure)
        {
            return result.ToNotFound();
        }

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Created<GetPaymentDto>, NotFound<ProblemDetails>, ValidationProblem>> RecordPayment(
        [FromRoute] long id,
        [FromBody] CreatePaymentDto dto,
        [FromServices] ITourStore tourStore,
        [FromServices] IQueryService queryService,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] TimeProvider timeProvider,
        CancellationToken ct)
    {
        var tour = await tourStore.GetByBookingId(id, ct);
        if (tour is null)
        {
            return BookingErrors.BookingNotFound(id).ToNotFound();
        }

        var booking = tour.Bookings.FirstOrDefault(b => b.Id == id);
        if (booking is null)
        {
            return BookingErrors.BookingNotFound(id).ToNotFound();
        }

        var result = booking.RecordPayment(
            dto.Amount,
            dto.PaymentDate,
            BookingMapper.MapToPaymentMethod(dto.Method),
            timeProvider,
            dto.ReferenceNumber,
            dto.Notes);

        if (result.IsFailure)
        {
            return result.ToValidationProblem();
        }

        var payment = result.Value;

        await unitOfWork.SaveEntities(ct);

        var updatedBooking = await queryService.GetBookingById(id, ct);
        var paymentDto = updatedBooking?.Payments.FirstOrDefault(p => p.Id == payment.Id);

        if (paymentDto is null)
        {
            return BookingErrors.BookingNotFound(id).ToNotFound();
        }

        return TypedResults.Created($"/bookings/{id}/payments/{payment.Id}", paymentDto);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, ValidationProblem>> UpdateBookingDetails(
        [FromRoute] long id,
        [FromBody] UpdateBookingDetailsDto dto,
        [FromServices] ITourStore tourStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var tour = await tourStore.GetByBookingId(id, ct);
        if (tour is null)
        {
            return BookingErrors.BookingNotFound(id).ToNotFound();
        }

        var result = tour.UpdateBookingDetails(
            id,
            BookingMapper.MapToRoomType(dto.RoomType),
            BookingMapper.MapToBikeType(dto.PrincipalBikeType),
            dto.CompanionCustomerId.HasValue ? (int)dto.CompanionCustomerId.Value : null,
            dto.CompanionBikeType.HasValue ? BookingMapper.MapToBikeType(dto.CompanionBikeType.Value) : null);

        if (result.IsFailure)
        {
            return result.ToValidationProblem();
        }

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }
}
