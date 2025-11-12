using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Customers;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class CustomerStore(ApplicationDbContext dbContext) : ICustomerStore
{
    public void Add(Customer customer) => dbContext.Customers.Add(customer);

    public async Task<Customer?> GetById(Guid id, CancellationToken ct) =>
        await dbContext.Customers.FindAsync([id], ct);

    public void Delete(Customer customer) => dbContext.Customers.Remove(customer);

    public async Task<bool> EmailExists(string email, CancellationToken ct) =>
        await dbContext.Customers.AnyAsync(c => c.ContactInfo.Email == email, ct);
}
