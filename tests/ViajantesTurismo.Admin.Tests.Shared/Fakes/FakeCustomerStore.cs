using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Tests.Shared.Fakes;

public sealed class FakeCustomerStore : ICustomerStore
{
    private readonly List<Customer> _customers = [];

    public void Add(Customer customer) => _customers.Add(customer);

    public Task<Customer?> GetById(Guid id, CancellationToken ct) =>
        Task.FromResult(_customers.SingleOrDefault(c => c.Id == id));

    public void Delete(Customer customer) => _customers.Remove(customer);

    public Task<bool> EmailExists(string email, CancellationToken ct) =>
        Task.FromResult(_customers.Any(c => c.ContactInfo.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> EmailExistsExcluding(string email, Guid excludeCustomerId, CancellationToken ct) =>
        Task.FromResult(_customers.Any(c =>
            c.ContactInfo.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && c.Id != excludeCustomerId));

    public void AddExistingCustomer(Customer customer) => _customers.Add(customer);

    public void Seed(Customer customer) => _customers.Add(customer);
}
