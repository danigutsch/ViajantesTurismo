using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Customers;

namespace ViajantesTurismo.Admin.Testing.Fakes;

public sealed class ConcurrentCustomerEmailConflictUnitOfWork(
    FakeCustomerStore customerStore,
    string conflictingEmail) : IUnitOfWork
{
    public Task SaveEntities(CancellationToken ct)
    {
        customerStore.SeedEmail(conflictingEmail);
        return Task.FromException(new CustomerEmailConflictException());
    }
}
