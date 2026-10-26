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
    public List<string> EmailExistsCalls { get; } = [];

    public void Add(Customer customer) => inner.Add(customer);

    public Task<Customer?> GetById(Guid id, CancellationToken ct) => inner.GetById(id, ct);

    public Task<Customer?> GetByEmail(string email, CancellationToken ct) =>
        hideCustomers ? Task.FromResult<Customer?>(null) : inner.GetByEmail(email, ct);

    public void Delete(Customer customer) => inner.Delete(customer);

    public async Task<bool> EmailExists(string email, CancellationToken ct)
    {
        EmailExistsCalls.Add(email);
        return emailExists?.Invoke(EmailExistsCalls.Count) ?? await inner.EmailExists(email, ct);
    }

    public Task<bool> EmailExistsExcluding(string email, Guid excludeCustomerId, CancellationToken ct) =>
        inner.EmailExistsExcluding(email, excludeCustomerId, ct);
}
