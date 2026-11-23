using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>
/// Interface for interacting with the Tours API endpoints.
/// </summary>
public interface IToursApiClient
{
    /// <summary>
    /// Gets a list of tours with optional pagination.
    /// </summary>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <param name="maxItems">Maximum number of items to return (default: all items).</param>
    /// <returns>The materialized tour-list outcome.</returns>
    Task<HttpOutcome<GetTourDto[]>> GetTours(CancellationToken ct, int maxItems = int.MaxValue);

    /// <summary>Gets a bounded page of active tour summaries.</summary>
    /// <param name="page">The one-based page to request.</param>
    /// <param name="sort">The requested column.</param>
    /// <param name="descending">Whether the selected column uses descending order.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The outcome containing the requested tour summaries and total.</returns>
    Task<HttpOutcome<GetTourPageDto>> GetTourPage(int page, TourPageSort sort, bool descending, CancellationToken ct);

    /// <summary>Searches for a bounded set of active tour choices.</summary>
    /// <param name="request">The optional search text and requested result limit.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The materialized option-search outcome.</returns>
    Task<HttpOutcome<GetTourOptionsDto>> SearchTourOptions(TourOptionSearchDto request, CancellationToken ct);

    /// <summary>
    /// Gets a specific tour by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the tour.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The materialized tour outcome.</returns>
    Task<HttpOutcome<GetTourDto>> GetTourById(Guid id, CancellationToken ct);

    /// <summary>
    /// Creates a new tour.
    /// </summary>
    /// <param name="dto">The tour data to create.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The contract-level outcome of the create request.</returns>
    Task<HttpOutcome> CreateTour(CreateTourDto dto, CancellationToken ct);

    /// <summary>
    /// Updates an existing tour.
    /// </summary>
    /// <param name="id">The unique identifier of the tour to update.</param>
    /// <param name="dto">The updated tour data.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The update outcome returned by the API.</returns>
    Task<HttpOutcome> UpdateTour(Guid id, UpdateTourDto dto, CancellationToken ct);
}
