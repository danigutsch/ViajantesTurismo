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
    /// Retrieves a tour by ID.
    /// </summary>
    /// <param name="id">The tour ID.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The tour DTO or null if not found.</returns>
    Task<GetTourDto?> GetTourById(int id, CancellationToken ct);

    /// <summary>
    /// Retrieves all customers.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of customer DTOs.</returns>
    Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct);

    /// <summary>
    /// Retrieves customer details by ID.
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The customer details DTO or null if not found.</returns>
    Task<CustomerDetailsDto?> GetCustomerDetailsById(int id, CancellationToken ct);
}