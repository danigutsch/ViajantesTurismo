using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

/// <summary>
/// Wraps a fake store, records every email lookup, and can script the answer of a numbered lookup or hide
/// stored customers from <see cref="GetByEmail"/>.
/// </summary>
internal sealed class ScriptedCustomerStore(
    FakeCustomerStore inner,
    Func<int, bool?>? emailExists = null,
    bool hideCustomers = false) : ICustomerStore
{
    /// <summary>Emails passed to <c>EmailExists</c>, in call order.</summary>
    public List<string> EmailExistsCalls { get; } = [];

    /// <summary>Adds the customer to the wrapped store.</summary>
    public void Add(Customer customer) => inner.Add(customer);

    /// <summary>Gets the customer by id from the wrapped store.</summary>
    public Task<Customer?> GetById(Guid id, CancellationToken ct) => inner.GetById(id, ct);

    /// <summary>Gets the customer by email from the wrapped store, or null when customers are hidden.</summary>
    public Task<Customer?> GetByEmail(string email, CancellationToken ct) =>
        hideCustomers ? Task.FromResult<Customer?>(null) : inner.GetByEmail(email, ct);

    /// <summary>Deletes the customer from the wrapped store.</summary>
    public void Delete(Customer customer) => inner.Delete(customer);

    /// <summary>Records the email, then returns the scripted answer for this call number, or the wrapped store answer when unscripted.</summary>
    public async Task<bool> EmailExists(string email, CancellationToken ct)
    {
        EmailExistsCalls.Add(email);
        return emailExists?.Invoke(EmailExistsCalls.Count) ?? await inner.EmailExists(email, ct);
    }

    /// <summary>Checks email existence excluding one customer in the wrapped store.</summary>
    public Task<bool> EmailExistsExcluding(string email, Guid excludeCustomerId, CancellationToken ct) =>
        inner.EmailExistsExcluding(email, excludeCustomerId, ct);
}
