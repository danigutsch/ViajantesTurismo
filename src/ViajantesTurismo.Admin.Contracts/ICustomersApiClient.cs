namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Interface for interacting with the Customers API endpoints.
/// </summary>
public interface ICustomersApiClient
{
    /// <summary>
    /// Gets a list of customers with optional pagination.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <param name="maxItems">Maximum number of items to return (default: 100).</param>
    /// <returns>Read-only list of customer DTOs.</returns>
    Task<IReadOnlyList<GetCustomerDto>> GetCustomers(CancellationToken cancellationToken, int maxItems = 100);

    /// <summary>
    /// Gets detailed information for a specific customer by their ID.
    /// </summary>
    /// <param name="id">The unique identifier of the customer.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The detailed customer DTO if found, null otherwise.</returns>
    Task<CustomerDetailsDto?> GetCustomerById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="dto">The customer data to create.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The URI of the newly created customer resource.</returns>
    Task<Uri> CreateCustomer(CreateCustomerDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    /// <param name="id">The unique identifier of the customer to update.</param>
    /// <param name="dto">The updated customer data.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    Task UpdateCustomer(Guid id, UpdateCustomerDto dto, CancellationToken cancellationToken);
}
