using Microsoft.EntityFrameworkCore;
using PandaBear.BuildingBlocks;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Infrastructure.Customers;

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
    public async Task<Customer?> GetByEmail(string email, CancellationToken ct)
    {
        if (ToStoredEmail(email) is not { } address)
        {
            return null;
        }

        return await dbContext.Customers.FirstOrDefaultAsync(customer => customer.ContactInfo.Email == address, ct);
    }

    /// <summary>Marks a customer for removal.</summary>
    public void Delete(Customer customer) => dbContext.Customers.Remove(customer);

    /// <summary>Returns whether any customer has the email.</summary>
    public async Task<bool> EmailExists(string email, CancellationToken ct) =>
        ToStoredEmail(email) is { } address
        && await dbContext.Customers.AnyAsync(customer => customer.ContactInfo.Email == address, ct);

    /// <summary>Returns whether another customer, other than the excluded one, has the email.</summary>
    public async Task<bool> EmailExistsExcluding(string email, Guid excludeCustomerId, CancellationToken ct) =>
        ToStoredEmail(email) is { } address
        && await dbContext.Customers.AnyAsync(customer => customer.ContactInfo.Email == address && customer.Id != excludeCustomerId, ct);

    /// <summary>
    /// Builds the stored form of an email for a lookup, or <see langword="null"/> for a blank email, which no customer has.
    /// The column is case-insensitive, so the lower-casing of <see cref="EmailAddress"/> does not change the match.
    /// </summary>
    private static EmailAddress? ToStoredEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : EmailAddress.Restore(email);
}
