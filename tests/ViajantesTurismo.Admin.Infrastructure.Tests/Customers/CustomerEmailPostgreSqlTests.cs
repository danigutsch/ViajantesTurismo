using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Customers;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Customers;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
public sealed class CustomerEmailPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IAsyncLifetime
{
    private CustomerEmailPostgreSqlScenario? scenario;

    private CustomerEmailPostgreSqlScenario Scenario =>
        scenario ?? throw new InvalidOperationException("Test scenario is not initialized.");

    public async ValueTask InitializeAsync()
    {
        scenario = await CustomerEmailPostgreSqlScenario.Create(fixture, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (scenario is not null)
        {
            await scenario.DisposeAsync();
        }
    }

    [Fact]
    public async Task Customer_email_collation_and_unique_index_are_enforced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.ApplyLatestMigration(ct);
        var original = await Scenario.AddCustomer("Traveler@Example.com", ct);
        _ = await Scenario.AddCustomer("jose@example.com", ct);
        _ = await Scenario.AddCustomer("josé@example.com", ct);
        _ = await Scenario.AddCustomer("INDIGO@example.com", ct);
        _ = await Scenario.AddCustomer("Émile@example.com", ct);
        _ = await Scenario.AddCustomer("emile@example.com", ct);

        // Act
        var found = await Scenario.GetByEmail("traveler@example.com", ct);
        var foundAsciiI = await Scenario.GetByEmail("indigo@example.com", ct);
        var foundAccentedCase = await Scenario.GetByEmail("émile@example.com", ct);
        Func<Task> duplicateInsert = () => Scenario.AddCustomer("TRAVELER@example.com", ct);
        var exception = await duplicateInsert.ShouldThrow<CustomerEmailConflictException>();
        Func<Task> duplicateAsciiI = () => Scenario.AddCustomer("indigo@example.com", ct);
        _ = await duplicateAsciiI.ShouldThrow<CustomerEmailConflictException>();
        var count = await Scenario.CountCustomers(ct);

        // Assert
        found.ShouldNotBeNull();
        found.Id.ShouldBe(original.Id);
        found.ContactInfo.Email.ShouldBe("Traveler@Example.com");
        var assertedAsciiI = foundAsciiI.ShouldNotBeNull();
        assertedAsciiI.ContactInfo.Email.ShouldBe("INDIGO@example.com");
        var assertedAccentedCase = foundAccentedCase.ShouldNotBeNull();
        assertedAccentedCase.ContactInfo.Email.ShouldBe("Émile@example.com");
        var dbUpdateException = exception.InnerException.ShouldBeOfType<DbUpdateException>();
        var postgresException = dbUpdateException.InnerException.ShouldBeOfType<PostgresException>();
        postgresException.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        postgresException.ConstraintName.ShouldBe("UX_CustomerContactInfo_Email");
        count.ShouldBe(6);
    }

    [Fact]
    public async Task Fresh_admin_migration_rolls_back_and_reapplies_with_email_uniqueness()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.CreateCustomerEmailExtension(ct);
        var extensionExistsBeforeUp = await Scenario.CustomerEmailExtensionExists(ct);
        await Scenario.ApplyLatestMigration(ct);
        var original = await Scenario.AddCustomer("Rollback@Example.com", ct);

        // Act
        var foundAfterUp = await Scenario.GetByEmail("rollback@example.com", ct);
        await Scenario.ApplyMigration("0", ct);
        var extensionExistsAfterDown = await Scenario.CustomerEmailExtensionExists(ct);
        await Scenario.ApplyLatestMigration(ct);
        var readded = await Scenario.AddCustomer("Reapply@Example.com", ct);
        var foundAfterReapply = await Scenario.GetByEmail("REAPPLY@example.com", ct);

        // Assert
        extensionExistsBeforeUp.ShouldBeTrue();
        extensionExistsAfterDown.ShouldBeTrue();
        foundAfterUp.ShouldNotBeNull();
        foundAfterUp.Id.ShouldBe(original.Id);
        foundAfterReapply.ShouldNotBeNull();
        foundAfterReapply.Id.ShouldBe(readded.Id);
    }
}
