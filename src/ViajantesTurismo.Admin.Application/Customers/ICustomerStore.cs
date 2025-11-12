using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Application.Customers;

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
    Task<Customer?> GetById(Guid id, CancellationToken ct);

    /// <summary>
    /// Deletes a customer from the store.
    /// </summary>
    /// <param name="customer">The customer to delete.</param>
    void Delete(Customer customer);

    /// <summary>
    /// Checks if a customer with the specified email address already exists.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if a customer with the email exists, otherwise false.</returns>
    Task<bool> EmailExists(string email, CancellationToken ct);
}
