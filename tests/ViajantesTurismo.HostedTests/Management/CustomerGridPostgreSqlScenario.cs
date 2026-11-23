using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Seeds customers with independently ordered names, emails, and nationalities.</summary>
internal static class CustomerGridPostgreSqlScenario
{
    /// <summary>Stores customers in the migrated Admin database and returns the source rows for comparison.</summary>
    internal static async Task<IReadOnlyList<Customer>> Seed(string connectionString, CancellationToken ct)
    {
        string[] firstNames = ["Clara", "Alice", "Bela"];
        string[] lastNames = ["Sousa", "Lima", "Paz"];
        string[] nationalities = ["Canada", "Brazil", "Portugal"];
        var customers = Enumerable.Range(0, 23)
            .Select(index => EntityBuilders.BuildCustomer(new CustomerOptions(
                FirstName: firstNames[index % firstNames.Length],
                LastName: lastNames[index / firstNames.Length % lastNames.Length],
                Email: $"sort-{23 - index:D3}@example.test",
                Nationality: nationalities[index / 2 % nationalities.Length])))
            .ToArray();

        await using var context = new AdminWriteDbContextDesignTimeFactory().CreateDbContext([]);
        context.Database.SetConnectionString(connectionString);
        await context.Database.MigrateAsync(ct);
        context.Customers.AddRange(customers);
        await context.SaveEntities(ct);
        return customers;
    }
}
