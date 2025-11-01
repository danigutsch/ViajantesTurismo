using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common.Results;

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

        bookingsGroup.MapPatch("/{id:long}/confirm", ConfirmBooking)
            .WithName("ConfirmBooking")
            .WithDescription("Confirms a booking by setting its status to Confirmed.")
            .WithSummary("Confirms a booking.");

        bookingsGroup.MapPatch("/{id:long}/details", UpdateBookingDetails)
            .WithName("UpdateBookingDetails")
            .WithDescription("Updates the price and notes of a booking.")
            .WithSummary("Updates booking details.");

        bookingsGroup.MapPatch("/{id:long}/complete", CompleteBooking)
            .WithName("CompleteBooking")
            .WithDescription("Completes a booking by setting its status to Completed.")
            .WithSummary("Completes a booking.");

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
            var problemDetails = new ProblemDetails()
            {
                Status = 400,
                Title = "Resource Not Found",
                Detail = $"No tour found containing booking with ID {id}."
            };
            return TypedResults.NotFound(problemDetails);
        }

        tour.UpdateBookingPrice(id, dto.TotalPrice);
        tour.UpdateBookingNotes(id, dto.Notes);

        var targetStatus = (BookingStatus)dto.Status;
        var booking = tour.Bookings.FirstOrDefault(b => b.Id == id);
        if (booking is not null && booking.Status != targetStatus)
        {
            Result statusUpdateResult;
            switch (targetStatus)
            {
                case BookingStatus.Confirmed:
                    statusUpdateResult = tour.ConfirmBooking(id);
                    break;
                case BookingStatus.Cancelled:
                    tour.CancelBooking(id);
                    statusUpdateResult = Result.Ok();
                    break;
                case BookingStatus.Completed:
                    tour.CompleteBooking(id);
                    statusUpdateResult = Result.Ok();
                    break;
                default:
                    statusUpdateResult = Result.Ok();
                    break;
            }

            if (statusUpdateResult.IsFailure)
            {
                var problemDetails = new ProblemDetails()
                {
                    Status = StatusCodes.Status404NotFound,
                    Detail = statusUpdateResult.ErrorDetails!.Detail
                };
                return TypedResults.NotFound(problemDetails);
            }
        }

        var paymentUpdateResult = tour.UpdateBookingPaymentStatus(id, (PaymentStatus)dto.PaymentStatus);
        if (paymentUpdateResult.IsFailure)
        {
            var problemDetails = new ProblemDetails()
            {
                Status = (int)paymentUpdateResult.Status,
                Title = paymentUpdateResult.Status.ToString(),
                Detail = paymentUpdateResult.ErrorDetails.Detail
            };
            return TypedResults.NotFound(problemDetails);
        }

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

    private static async Task<Results<NoContent, NotFound>> ConfirmBooking(
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

        var result = tour.ConfirmBooking(id);
        if (result.IsFailure)
        {
            return TypedResults.NotFound();
        }

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> UpdateBookingDetails(
        [FromRoute] long id,
        [FromBody] UpdateBookingDetailsDto dto,
        [FromServices] ITourStore tourStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var tour = await tourStore.GetByBookingId(id, ct);
        if (tour is null)
        {
            return TypedResults.NotFound();
        }

        tour.UpdateBookingPrice(id, dto.TotalPrice);
        tour.UpdateBookingNotes(id, dto.Notes);

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> CompleteBooking(
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

        tour.CompleteBooking(id);

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }
}
