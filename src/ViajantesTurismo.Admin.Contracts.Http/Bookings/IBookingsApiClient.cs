using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>
/// Interface for interacting with the Bookings API endpoints.
/// </summary>
public interface IBookingsApiClient
{
    /// <summary>
    /// Gets all bookings.
    /// </summary>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The materialized booking-list outcome.</returns>
    Task<HttpOutcome<GetBookingDto[]>> GetAllBookings(CancellationToken ct);

    /// <summary>
    /// Gets a specific booking by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the booking.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The materialized booking outcome.</returns>
    Task<HttpOutcome<GetBookingDto>> GetBookingById(Guid id, CancellationToken ct);

    /// <summary>
    /// Gets all bookings for a specific tour.
    /// </summary>
    /// <param name="tourId">The unique identifier of the tour.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The materialized booking-list outcome.</returns>
    Task<HttpOutcome<GetBookingDto[]>> GetBookingsByTourId(Guid tourId, CancellationToken ct);

    /// <summary>
    /// Gets all bookings for a specific customer.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The materialized booking-list outcome.</returns>
    Task<HttpOutcome<GetBookingDto[]>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct);

    /// <summary>
    /// Creates a new booking.
    /// </summary>
    /// <param name="dto">The booking data to create.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The contract-level outcome of the create request.</returns>
    Task<HttpOutcome> CreateBooking(CreateBookingDto dto, CancellationToken ct);

    /// <summary>
    /// Updates the discount information for a booking.
    /// </summary>
    /// <param name="id">The unique identifier of the booking.</param>
    /// <param name="dto">The updated discount data.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The update outcome returned by the API.</returns>
    Task<HttpOutcome> UpdateBookingDiscount(Guid id, UpdateBookingDiscountDto dto, CancellationToken ct);

    /// <summary>
    /// Updates the details of a booking (companion, notes, bike type, room type).
    /// </summary>
    /// <param name="id">The unique identifier of the booking.</param>
    /// <param name="dto">The updated booking details.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The update outcome returned by the API.</returns>
    Task<HttpOutcome> UpdateBookingDetails(Guid id, UpdateBookingDetailsDto dto, CancellationToken ct);

    /// <summary>
    /// Updates the notes for a booking.
    /// </summary>
    /// <param name="id">The unique identifier of the booking.</param>
    /// <param name="dto">The updated notes data.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The update outcome returned by the API.</returns>
    Task<HttpOutcome> UpdateBookingNotes(Guid id, UpdateBookingNotesDto dto, CancellationToken ct);

    /// <summary>
    /// Cancels a booking.
    /// </summary>
    /// <param name="id">The unique identifier of the booking to cancel.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The HTTP outcome mapped by the client.</returns>
    Task<HttpOutcome> CancelBooking(Guid id, CancellationToken ct);

    /// <summary>
    /// Confirms a booking.
    /// </summary>
    /// <param name="id">The unique identifier of the booking to confirm.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The HTTP outcome mapped by the client.</returns>
    Task<HttpOutcome> ConfirmBooking(Guid id, CancellationToken ct);

    /// <summary>
    /// Completes a booking.
    /// </summary>
    /// <param name="id">The unique identifier of the booking to complete.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The HTTP outcome mapped by the client.</returns>
    Task<HttpOutcome> CompleteBooking(Guid id, CancellationToken ct);

    /// <summary>
    /// Deletes a booking.
    /// </summary>
    /// <param name="id">The unique identifier of the booking to delete.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The delete outcome returned by the API.</returns>
    Task<HttpOutcome> DeleteBooking(Guid id, CancellationToken ct);

    /// <summary>
    /// Records a payment for a booking.
    /// </summary>
    /// <param name="bookingId">The unique identifier of the booking.</param>
    /// <param name="dto">The payment data to record.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The contract-level outcome of the record-payment request.</returns>
    Task<HttpOutcome> RecordPayment(Guid bookingId, CreatePaymentDto dto, CancellationToken ct);
}
