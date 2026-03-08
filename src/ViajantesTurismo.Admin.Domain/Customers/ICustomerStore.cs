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
    Task<Customer?> GetById(Guid id, CancellationToken ct);

    /// <summary>
    /// Gets a customer by their email address.
    /// </summary>
    /// <param name="email">The customer email address.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The customer, or null if not found.</returns>
    Task<Customer?> GetByEmail(string email, CancellationToken ct);

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

    /// <summary>
    /// Checks if a customer with the specified email address exists, excluding the specified customer.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <param name="excludeCustomerId">The customer ID to exclude from the check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if another customer with the email exists, otherwise false.</returns>
    Task<bool> EmailExistsExcluding(string email, Guid excludeCustomerId, CancellationToken ct);
}
