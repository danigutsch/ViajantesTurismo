using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Customers;

namespace ViajantesTurismo.Admin.Testing.Fakes;

public sealed class RolledBackCustomerEmailConflictUnitOfWork(FakeCustomerStore customerStore) : IUnitOfWork
{
    public Task SaveEntities(CancellationToken ct)
    {
        foreach (var customer in customerStore.AllCustomers.ToArray())
        {
            customerStore.Delete(customer);
        }

        return Task.FromException(new CustomerEmailConflictException());
    }
}
