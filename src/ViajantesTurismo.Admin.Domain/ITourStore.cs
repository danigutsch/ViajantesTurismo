namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Manages the storage and retrieval of <see cref="Tour"/> entities.
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
    Task<Tour?> GetById(int id, CancellationToken ct);
}