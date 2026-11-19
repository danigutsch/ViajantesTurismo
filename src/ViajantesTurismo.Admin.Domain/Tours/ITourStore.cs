namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Repository interface for managing tours.
/// </summary>
public interface ITourStore
{
    /// <summary>
    /// Adds a new tour to the store.
    /// </summary>
    /// <param name="tour">The tour to add.</param>
    void Add(Tour tour);

    /// <summary>
    /// Gets a tour by its ID.
    /// </summary>
    /// <param name="id">The ID of the tour.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The tour if found; otherwise, null.</returns>
    Task<Tour?> GetById(Guid id, CancellationToken ct);

    /// <summary>
    /// Gets a tour that contains a specific booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The tour that owns the booking if found; otherwise, null.</returns>
    Task<Tour?> GetByBookingId(Guid bookingId, CancellationToken ct);

    /// <summary>
    /// Checks if a tour with the specified identifier already exists.
    /// </summary>
    /// <param name="identifier">The tour identifier to check.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>True if a tour with the identifier exists; otherwise, false.</returns>
    Task<bool> IdentifierExists(string identifier, CancellationToken ct);

    /// <summary>
    /// Checks if a tour with the specified identifier exists, excluding a specific tour.
    /// </summary>
    /// <param name="identifier">The tour identifier to check.</param>
    /// <param name="excludeTourId">The ID of the tour to exclude from the check.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>True if another tour with the identifier exists; otherwise, false.</returns>
    Task<bool> IdentifierExistsExcluding(string identifier, Guid excludeTourId, CancellationToken ct);

    /// <summary>
    /// Deletes a tour from the store.
    /// </summary>
    /// <param name="tour">The tour to delete.</param>
    void Delete(Tour tour);
}
