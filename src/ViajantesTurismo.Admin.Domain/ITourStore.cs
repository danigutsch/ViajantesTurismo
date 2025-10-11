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
}
