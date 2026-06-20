namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Interface for interacting with the Bookings API endpoints.
/// </summary>
public interface IBookingsApiClient
{
    /// <summary>
    /// Gets all bookings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Array of booking DTOs.</returns>
    Task<GetBookingDto[]> GetAllBookings(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a specific booking by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the booking.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The booking DTO if found, null otherwise.</returns>
    Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all bookings for a specific tour.
    /// </summary>
    /// <param name="tourId">The unique identifier of the tour.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Array of booking DTOs for the specified tour.</returns>
    Task<GetBookingDto[]> GetBookingsByTourId(Guid tourId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all bookings for a specific customer.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Array of booking DTOs for the specified customer.</returns>
    Task<GetBookingDto[]> GetBookingsByCustomerId(Guid customerId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new booking.
    /// </summary>
    /// <param name="dto">The booking data to create.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The URI of the newly created booking resource.</returns>
    Task<Uri> CreateBooking(CreateBookingDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the discount information for a booking.
    /// </summary>
    /// <param name="id">The unique identifier of the booking.</param>
    /// <param name="dto">The updated discount data.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    Task UpdateBookingDiscount(Guid id, UpdateBookingDiscountDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the details of a booking (companion, notes, bike type, room type).
    /// </summary>
    /// <param name="id">The unique identifier of the booking.</param>
    /// <param name="dto">The updated booking details.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    Task UpdateBookingDetails(Guid id, UpdateBookingDetailsDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the notes for a booking.
    /// </summary>
    /// <param name="id">The unique identifier of the booking.</param>
    /// <param name="dto">The updated notes data.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    Task UpdateBookingNotes(Guid id, UpdateBookingNotesDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a booking.
    /// </summary>
    /// <param name="id">The unique identifier of the booking to cancel.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    Task CancelBooking(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Confirms a booking.
    /// </summary>
    /// <param name="id">The unique identifier of the booking to confirm.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    Task ConfirmBooking(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Completes a booking.
    /// </summary>
    /// <param name="id">The unique identifier of the booking to complete.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    Task CompleteBooking(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a booking.
    /// </summary>
    /// <param name="id">The unique identifier of the booking to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    Task DeleteBooking(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Records a payment for a booking.
    /// </summary>
    /// <param name="bookingId">The unique identifier of the booking.</param>
    /// <param name="dto">The payment data to record.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The URI of the newly created payment resource.</returns>
    Task<Uri> RecordPayment(Guid bookingId, CreatePaymentDto dto, CancellationToken cancellationToken);
}
