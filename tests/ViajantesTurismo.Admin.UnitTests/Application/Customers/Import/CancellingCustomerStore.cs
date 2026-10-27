using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

/// <summary>Customer store decorator that cancels a token source on add or on the first email check.</summary>
/// <param name="inner">The store that receives all calls.</param>
/// <param name="cancellation">The token source to cancel.</param>
/// <param name="cancelOnAdd">True to cancel on add, false to cancel after an email-exists check.</param>
internal sealed class CancellingCustomerStore(
    FakeCustomerStore inner,
    CancellationTokenSource cancellation,
    bool cancelOnAdd) : ICustomerStore
{
    /// <summary>Gets how many times <c>EmailExists</c> was called.</summary>
    public int EmailExistsCallCount
    {
        get; private set;
    }

    /// <summary>Adds the customer to the inner store and cancels when configured to cancel on add.</summary>
    public void Add(Customer customer)
    {
        inner.Add(customer);
        if (cancelOnAdd)
        {
            cancellation.Cancel();
        }
    }

    /// <summary>Delegates to the inner store.</summary>
    public Task<Customer?> GetById(Guid id, CancellationToken ct) => inner.GetById(id, ct);

    /// <summary>Delegates to the inner store.</summary>
    public Task<Customer?> GetByEmail(string email, CancellationToken ct) => inner.GetByEmail(email, ct);

    /// <summary>Delegates to the inner store.</summary>
    public void Delete(Customer customer) => inner.Delete(customer);

    /// <summary>Counts the call, checks the inner store, then cancels when not cancelling on add.</summary>
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

    /// <summary>Delegates to the inner store.</summary>
    public Task<bool> EmailExistsExcluding(string email, Guid excludeCustomerId, CancellationToken ct) =>
        inner.EmailExistsExcluding(email, excludeCustomerId, ct);
}
