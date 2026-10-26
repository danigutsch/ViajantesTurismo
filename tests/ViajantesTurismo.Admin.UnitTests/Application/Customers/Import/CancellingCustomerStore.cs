using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

internal sealed class CancellingCustomerStore(
    FakeCustomerStore inner,
    CancellationTokenSource cancellation,
    bool cancelOnAdd) : ICustomerStore
{
    public int EmailExistsCallCount
    {
        get; private set;
    }

    public void Add(Customer customer)
    {
        inner.Add(customer);
        if (cancelOnAdd)
        {
            cancellation.Cancel();
        }
    }

    public Task<Customer?> GetById(Guid id, CancellationToken ct) => inner.GetById(id, ct);

    public Task<Customer?> GetByEmail(string email, CancellationToken ct) => inner.GetByEmail(email, ct);

    public void Delete(Customer customer) => inner.Delete(customer);

    public async Task<bool> EmailExists(string email, CancellationToken ct)
    {
        EmailExistsCallCount++;
        var exists = await inner.EmailExists(email, ct);
        if (!cancelOnAdd)
        {
            await cancellation.CancelAsync();
        }

        return exists;
    }

    public Task<bool> EmailExistsExcluding(string email, Guid excludeCustomerId, CancellationToken ct) =>
        inner.EmailExistsExcluding(email, excludeCustomerId, ct);
}
