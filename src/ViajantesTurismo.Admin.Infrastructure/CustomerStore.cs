using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class CustomerStore(AdminWriteDbContext dbContext) : ICustomerStore
{
    public void Add(Customer customer) => dbContext.Customers.Add(customer);

    public async Task<Customer?> GetById(Guid id, CancellationToken ct) =>
        await dbContext.Customers.FindAsync([id], ct);

    public void Delete(Customer customer) => dbContext.Customers.Remove(customer);

    public async Task<bool> EmailExists(string email, CancellationToken ct) =>
        await dbContext.Customers.AnyAsync(c => c.ContactInfo.Email == email, ct);

    public async Task<bool> EmailExistsExcluding(string email, Guid excludeCustomerId, CancellationToken ct) =>
        await dbContext.Customers.AnyAsync(c => c.ContactInfo.Email == email && c.Id != excludeCustomerId, ct);
}
