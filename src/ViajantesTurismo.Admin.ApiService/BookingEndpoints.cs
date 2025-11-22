using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.CancelBooking;
using ViajantesTurismo.Admin.Application.Bookings.CompleteBooking;
using ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;
using ViajantesTurismo.Admin.Application.Bookings.CreateBooking;
using ViajantesTurismo.Admin.Application.Bookings.DeleteBooking;
using ViajantesTurismo.Admin.Application.Bookings.RecordPayment;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDetails;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDiscount;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingNotes;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;
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
    public static void MapBookingEndpoints(this WebApplication app)
    {
        var bookingsGroup = app.MapGroup("/bookings")
            .WithGroupName("Bookings")
            .WithTags("Bookings");

        bookingsGroup.MapGet("/", GetAllBookings)
            .WithName("GetBookings")
            .WithDescription("Retrieves all bookings.")
            .WithSummary("Retrieves all bookings.");

        bookingsGroup.MapGet("/{id:guid}", GetBookingById)
            .WithName("GetBookingById")
            .WithDescription("Retrieves a booking by its ID.")
            .WithSummary("Retrieves a booking by its ID.");

        bookingsGroup.MapGet("/tour/{tourId:guid}", GetBookingsByTourId)
            .WithName("GetBookingsByTourId")
            .WithDescription("Retrieves all bookings for a specific tour.")
            .WithSummary("Retrieves all bookings for a specific tour.");

        bookingsGroup.MapGet("/customer/{customerId:guid}", GetBookingsByCustomerId)
            .WithName("GetBookingsByCustomerId")
            .WithDescription("Retrieves all bookings for a specific customer (as primary or companion).")
            .WithSummary("Retrieves all bookings for a specific customer.");

        bookingsGroup.MapPost("/", CreateBooking)
            .WithName("CreateBooking")
            .WithDescription("Creates a new booking for a tour.")
            .WithSummary("Creates a new booking.");

        bookingsGroup.MapPut("/{id:guid}/discount", UpdateBookingDiscount)
            .WithName("UpdateBookingDiscount")
            .WithDescription("Updates the discount for a booking.")
            .WithSummary("Updates booking discount.");

        bookingsGroup.MapPut("/{id:guid}/details", UpdateBookingDetails)
            .WithName("UpdateBookingDetails")
            .WithDescription("Updates booking details (room type, bikes, companion).")
            .WithSummary("Updates booking details.");

        bookingsGroup.MapDelete("/{id:guid}", DeleteBooking)
            .WithName("DeleteBooking")
            .WithDescription("Deletes a booking.")
            .WithSummary("Deletes a booking.");

        bookingsGroup.MapPost("/{id:guid}/cancel", CancelBooking)
            .WithName("CancelBooking")
            .WithDescription("Cancels a booking by transitioning its status to Cancelled.")
            .WithSummary("Cancels a booking.");

        bookingsGroup.MapPost("/{id:guid}/confirm", ConfirmBooking)
            .WithName("ConfirmBooking")
            .WithDescription("Confirms a booking by transitioning its status to Confirmed.")
            .WithSummary("Confirms a booking.");

        bookingsGroup.MapPatch("/{id:guid}/notes", UpdateBookingNotes)
            .WithName("UpdateBookingNotes")
            .WithDescription("Updates the notes of a booking.")
            .WithSummary("Updates booking notes.");

        bookingsGroup.MapPost("/{id:guid}/complete", CompleteBooking)
            .WithName("CompleteBooking")
            .WithDescription("Completes a booking by transitioning its status to Completed.")
            .WithSummary("Completes a booking.");

        bookingsGroup.MapPost("/{id:guid}/payments", RecordPayment)
            .WithName("RecordPayment")
            .WithDescription("Records a payment for a booking.")
            .WithSummary("Records a payment.");
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

    private static async Task<Results<Created<GetBookingDto>, NotFound<ProblemDetails>, ValidationProblem>> CreateBooking(
        [FromBody] CreateBookingDto dto,
        [FromServices] CreateBookingCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
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
            return result.Status == ResultStatus.NotFound
                ? result.ToNotFound()
                : result.ToValidationProblem();
        }

        var bookingDto = await queryService.GetBookingById(result.Value, ct);

        return TypedResults.Created($"/bookings/{result.Value}", bookingDto!);
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, ValidationProblem>> UpdateBookingDiscount(
        [FromRoute] Guid id,
        [FromBody] UpdateBookingDiscountDto dto,
        [FromServices] UpdateBookingDiscountCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var command = new UpdateBookingDiscountCommand(
            id,
            dto.DiscountType,
            dto.DiscountAmount,
            dto.DiscountReason);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            return result.Status == ResultStatus.NotFound
                ? result.ToNotFound()
                : result.ToValidationProblem();
        }

        var updatedBooking = await queryService.GetBookingById(id, ct);

        return TypedResults.Ok(updatedBooking!);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>> DeleteBooking(
        [FromRoute] Guid id,
        [FromServices] DeleteBookingCommandHandler handler,
        CancellationToken ct)
    {
        var command = new DeleteBookingCommand(id);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            return result.Status switch
            {
                ResultStatus.NotFound => result.ToNotFound(),
                ResultStatus.Conflict => result.ToConflict(),
                _ => result.ToValidationProblem()
            };
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> CancelBooking(
        [FromRoute] Guid id,
        [FromServices] CancelBookingCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var command = new CancelBookingCommand(id);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            return result.Status == ResultStatus.NotFound
                ? result.ToNotFound()
                : result.ToConflict();
        }

        var updatedBooking = await queryService.GetBookingById(id, ct);

        return TypedResults.Ok(updatedBooking!);
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ConfirmBooking(
        [FromRoute] Guid id,
        [FromServices] ConfirmBookingCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var command = new ConfirmBookingCommand(id);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            return result.Status == ResultStatus.NotFound
                ? result.ToNotFound()
                : result.ToConflict();
        }

        var updatedBooking = await queryService.GetBookingById(id, ct);

        return TypedResults.Ok(updatedBooking!);
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>>> UpdateBookingNotes(
        [FromRoute] Guid id,
        [FromBody] UpdateBookingNotesDto dto,
        [FromServices] UpdateBookingNotesCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var command = new UpdateBookingNotesCommand(id, dto.Notes);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            return result.ToNotFound();
        }

        var updatedBooking = await queryService.GetBookingById(id, ct);

        return TypedResults.Ok(updatedBooking!);
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>> CompleteBooking(
        [FromRoute] Guid id,
        [FromServices] CompleteBookingCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var command = new CompleteBookingCommand(id);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            return result.Status == ResultStatus.NotFound
                ? result.ToNotFound()
                : result.IsConflict
                    ? result.ToConflict()
                    : result.ToValidationProblem();
        }

        var updatedBooking = await queryService.GetBookingById(id, ct);

        return TypedResults.Ok(updatedBooking!);
    }

    private static async Task<Results<Created<GetPaymentDto>, NotFound<ProblemDetails>, ValidationProblem>>
        RecordPayment(
            [FromRoute] Guid id,
            [FromBody] CreatePaymentDto dto,
            [FromServices] RecordPaymentCommandHandler handler,
            [FromServices] IQueryService queryService,
            CancellationToken ct)
    {
        var command = new RecordPaymentCommand(
            id,
            dto.Amount,
            dto.PaymentDate,
            dto.Method,
            dto.ReferenceNumber,
            dto.Notes);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            return result.Status == ResultStatus.NotFound
                ? result.ToNotFound()
                : result.ToValidationProblem();
        }

        var paymentId = result.Value;

        var updatedBooking = await queryService.GetBookingById(id, ct);
        var paymentDto = updatedBooking?.Payments.FirstOrDefault(p => p.Id == paymentId);

        return TypedResults.Created($"/bookings/{id}/payments/{paymentId}", paymentDto!);
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>, ValidationProblem>> UpdateBookingDetails(
        [FromRoute] Guid id,
        [FromBody] UpdateBookingDetailsDto dto,
        [FromServices] UpdateBookingDetailsCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var command = new UpdateBookingDetailsCommand(
            id,
            dto.RoomType,
            dto.PrincipalBikeType,
            dto.CompanionCustomerId,
            dto.CompanionBikeType);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            return result.Status == ResultStatus.NotFound
                ? result.ToNotFound()
                : result.ToValidationProblem();
        }

        var updatedBooking = await queryService.GetBookingById(id, ct);

        return TypedResults.Ok(updatedBooking!);
    }
}
