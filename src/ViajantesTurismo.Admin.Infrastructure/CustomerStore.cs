using ViajantesTurismo.Admin.Domain;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class CustomerStore(ApplicationDbContext dbContext) : ICustomerStore
{
    public void Add(Customer customer) => dbContext.Customers.Add(customer);

    public async Task<Customer?> GetById(int id, CancellationToken ct) =>
        await dbContext.Customers.FindAsync([id], ct);

    public void Delete(Customer customer) => dbContext.Customers.Remove(customer);
}