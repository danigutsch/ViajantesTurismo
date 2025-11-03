using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Tours;

/// <summary>
/// Manages the storage and retrieval of <see cref="Tour"/> entities for command operations.
/// </summary>
/// <remarks>
/// This interface follows the CQRS (Command Query Responsibility Segregation) pattern:
/// <list type="bullet">
/// <item><description>Stores (like <see cref="ITourStore"/>) are used for COMMAND operations (Create, Update, Delete) that modify state.</description></item>
/// <item><description>The <see cref="IQueryService"/> is used for QUERY operations (Read) that retrieve data without modification.</description></item>
/// <item><description>Command endpoints should ONLY use stores and never use <see cref="IQueryService"/>.</description></item>
/// <item><description>Query endpoints should ONLY use <see cref="IQueryService"/> and never use stores.</description></item>
/// </list>
/// This separation allows for optimized read and write models, better scalability, and clearer separation of concerns.
/// </remarks>
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
    Task<Tour?> GetById(int id, CancellationToken ct);

    /// <summary>
    /// Gets a tour that contains a specific booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The tour that owns the booking if found; otherwise, null.</returns>
    Task<Tour?> GetByBookingId(long bookingId, CancellationToken ct);
}