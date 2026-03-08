using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Tests.Shared.Fakes;

public sealed class FakeCustomerStore(IEnumerable<string>? seededEmails = null) : ICustomerStore
{
    private readonly List<Customer> _customers = [];
    private readonly HashSet<string> _seededEmails =
        [.. (seededEmails ?? []).Select(e => e.Trim().ToUpperInvariant())];

    public IReadOnlyList<Customer> AllCustomers => _customers.AsReadOnly();

    public void Add(Customer customer) => _customers.Add(customer);

    public Task<Customer?> GetById(Guid id, CancellationToken ct) =>
        Task.FromResult(_customers.SingleOrDefault(c => c.Id == id));

    public void Delete(Customer customer) => _customers.Remove(customer);

    public Task<bool> EmailExists(string email, CancellationToken ct) =>
        Task.FromResult(
            _seededEmails.Contains(email.Trim().ToUpperInvariant()) ||
            _customers.Any(c => c.ContactInfo.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> EmailExistsExcluding(string email, Guid excludeCustomerId, CancellationToken ct) =>
        Task.FromResult(
            _seededEmails.Contains(email.Trim().ToUpperInvariant()) ||
            _customers.Any(c =>
                c.ContactInfo.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && c.Id != excludeCustomerId));

    public void AddExistingCustomer(Customer customer) => _customers.Add(customer);

    public void Seed(Customer customer) => _customers.Add(customer);
}
