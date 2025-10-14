namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Repository interface for managing customers.
/// </summary>
public interface ICustomerStore
{
    /// <summary>
    /// Adds a new customer to the store.
    /// </summary>
    /// <param name="customer">The customer to add.</param>
    void Add(Customer customer);

    /// <summary>
    /// Gets a customer by their identifier.
    /// </summary>
    /// <param name="id">The customer identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The customer, or null if not found.</returns>
    Task<Customer?> GetById(int id, CancellationToken ct);

    /// <summary>
    /// Deletes a customer from the store.
    /// </summary>
    /// <param name="customer">The customer to delete.</param>
    void Delete(Customer customer);
}