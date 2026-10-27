using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>
/// EF Core implementation of <see cref="ICustomerStore" />.
/// </summary>
/// <param name="dbContext">Write context used to track and query customers.</param>
internal sealed class CustomerStore(AdminWriteDbContext dbContext) : ICustomerStore
{
    /// <summary>Starts tracking a new customer.</summary>
    public void Add(Customer customer) => dbContext.Customers.Add(customer);

    /// <summary>Finds a customer by id; null when absent.</summary>
    public async Task<Customer?> GetById(Guid id, CancellationToken ct) =>
        await dbContext.Customers.FindAsync([id], ct);

    /// <summary>Finds a customer by contact email; null when absent.</summary>
    public async Task<Customer?> GetByEmail(string email, CancellationToken ct) =>
        await dbContext.Customers.FirstOrDefaultAsync(c => c.ContactInfo.Email == email, ct);

    /// <summary>Marks a customer for removal.</summary>
    public void Delete(Customer customer) => dbContext.Customers.Remove(customer);

    /// <summary>Returns whether any customer has the email.</summary>
    public async Task<bool> EmailExists(string email, CancellationToken ct) =>
        await dbContext.Customers.AnyAsync(c => c.ContactInfo.Email == email, ct);

    /// <summary>Returns whether another customer, other than the excluded one, has the email.</summary>
    public async Task<bool> EmailExistsExcluding(string email, Guid excludeCustomerId, CancellationToken ct) =>
        await dbContext.Customers.AnyAsync(c => c.ContactInfo.Email == email && c.Id != excludeCustomerId, ct);
}
