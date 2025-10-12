using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Provides query operations for the admin domain.
/// </summary>
public interface IQueryService
{
    /// <summary>
    /// Retrieves all tours.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of <see cref="GetTourDto"/> representing all tours.</returns>
    Task<IReadOnlyList<GetTourDto>> GetAllTours(CancellationToken ct);

    /// <summary>
    /// Retrieves all customers.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of customer DTOs.</returns>
    Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct);
}
