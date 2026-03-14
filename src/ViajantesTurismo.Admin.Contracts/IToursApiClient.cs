namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Interface for interacting with the Tours API endpoints.
/// </summary>
public interface IToursApiClient
{
    /// <summary>
    /// Gets a list of tours with optional pagination.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <param name="maxItems">Maximum number of items to return (default: all items).</param>
    /// <returns>Array of tour DTOs.</returns>
    Task<GetTourDto[]> GetTours(CancellationToken cancellationToken, int maxItems = int.MaxValue);

    /// <summary>
    /// Gets a specific tour by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the tour.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The tour DTO if found, null otherwise.</returns>
    Task<GetTourDto?> GetTourById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new tour.
    /// </summary>
    /// <param name="dto">The tour data to create.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The URI of the newly created tour resource.</returns>
    Task<Uri> CreateTour(CreateTourDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing tour.
    /// </summary>
    /// <param name="id">The unique identifier of the tour to update.</param>
    /// <param name="dto">The updated tour data.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    Task UpdateTour(Guid id, UpdateTourDto dto, CancellationToken cancellationToken);
}
