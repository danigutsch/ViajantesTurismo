using ViajantesTurismo.Admin.Application.Customers;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Fakes;

public sealed class FakeCustomerStore : ICustomerStore
{
    private readonly List<Customer> _customers = [];

    public void Add(Customer customer) => _customers.Add(customer);
    public Task<Customer?> GetById(Guid id, CancellationToken ct) => Task.FromResult(_customers.SingleOrDefault(c => c.Id == id));
    public void Delete(Customer customer) => _customers.Remove(customer);
    public Task<bool> EmailExists(string email, CancellationToken ct) => Task.FromResult(_customers.Any(c => c.ContactInfo.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public void AddExistingCustomer(Customer customer) => _customers.Add(customer);
}
