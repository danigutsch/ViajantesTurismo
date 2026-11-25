using SharedKernel.BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Customers;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Customers;

[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CustomerEmailPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IAsyncLifetime, IClassFixture<PostgreSqlTestServerFixture>
{
    private const string ImportHeaders =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation,NationalId,IdNationality," +
        "Email,Mobile,Street,Neighborhood,PostalCode,City,State,Country," +
        "WeightKg,HeightCentimeters,BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

    private const string ImportRow =
        "Ana,Silva,Male,1990-01-01,Brazilian,Engineer,A12345678,BR," +
        "email@example.com,+5511999999999,Rua A,Centro,01000-000,Sao Paulo,SP,BR," +
        "75,175,Regular,DoubleOccupancy,SingleBed,Emergency Name,+5511888888888";

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
        found.ContactInfo.Email.Value.ShouldBe("traveler@example.com");
        var assertedAsciiI = foundAsciiI.ShouldNotBeNull();
        assertedAsciiI.ContactInfo.Email.Value.ShouldBe("indigo@example.com");
        var assertedAccentedCase = foundAccentedCase.ShouldNotBeNull();
        assertedAccentedCase.ContactInfo.Email.Value.ShouldBe("émile@example.com");
        var dbUpdateException = exception.InnerException.ShouldBeOfType<UniqueConstraintViolationException>().InnerException.ShouldBeOfType<DbUpdateException>();
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

    [Fact]
    public async Task Import_conflict_lookup_preserves_exact_case_insensitive_matching_and_request_order()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.ApplyLatestMigration(ct);
        var ascii = await Scenario.AddCustomer("jose@example.com", ct);
        var accented = await Scenario.AddCustomer("josé@example.com", ct);

        // Act
        var matches = await Scenario.FindImportConflictIds(
            [" JOSÉ@EXAMPLE.COM ", "file-only@example.com", "JOSE@EXAMPLE.COM"],
            ct);

        // Assert
        matches.ShouldBe([accented.Id, null, ascii.Id]);
    }

    [Fact]
    public async Task Import_rolls_back_every_row_when_a_competing_email_wins_during_save()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.ApplyLatestMigration(ct);
        const string existingEmail = "existing-import@example.com";
        const string newEmail = "new-import@example.com";
        const string racedEmail = "raced-import@example.com";
        var original = await Scenario.AddCustomer(existingEmail, ct);
        var rows = new[] { existingEmail, newEmail, racedEmail }
            .Select(email => ImportRow.Replace("email@example.com", email, StringComparison.Ordinal));
        var csv = ImportHeaders + "\n" + string.Join('\n', rows);
        var resolutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [existingEmail] = "overwrite"
        };

        // Act
        var (result, usedTransaction, saveCalls) = await Scenario.CommitImportWithConcurrentInsert(csv, resolutions, racedEmail, ct);
        var persisted = await Scenario.GetByEmail(existingEmail, ct);
        var count = await Scenario.CountCustomers(ct);

        // Assert
        result.SuccessCount.ShouldBe(0);
        result.Conflicts.ShouldNotBeNull().ShouldContain(conflict => conflict.Email == racedEmail);
        persisted.ShouldNotBeNull().PersonalInfo.FirstName.ShouldBe(original.PersonalInfo.FirstName);
        count.ShouldBe(2);
        saveCalls.ShouldBe(1);
        usedTransaction.ShouldBeTrue();
    }

    [Fact]
    public async Task A_dispatched_dry_run_reports_rows_without_saving_customers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.ApplyLatestMigration(ct);
        var csv = ImportHeaders + "\n" + ImportRow;

        // Act
        var (result, saveCalls) = await Scenario.DryRunImport(csv, ct);
        var count = await Scenario.CountCustomers(ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.SuccessCount.ShouldBe(1);
        result.Value.ErrorCount.ShouldBe(0);
        saveCalls.ShouldBe(0);
        count.ShouldBe(0);
    }

    [Fact]
    public async Task Retrying_after_the_first_database_flush_persists_the_import_once()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.ApplyLatestMigration(ct);
        var secondRow = ImportRow.Replace("email@example.com", "second@example.com", StringComparison.Ordinal);
        var csv = ImportHeaders + "\n" + ImportRow + "\n" + secondRow;

        // Act
        var (result, saveCalls) = await Scenario.RetryImportAfterRealSave(csv, ct);
        var count = await Scenario.CountCustomers(ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.SuccessCount.ShouldBe(2);
        result.Value.ErrorCount.ShouldBe(0);
        saveCalls.ShouldBe(2);
        count.ShouldBe(2);
    }

    [Fact]
    public void Fresh_admin_migration_allows_common_us_english_utf8_spelling()
    {
        // Arrange
        const string locale = "'en_US.UTF-8'";

        // Act
        var migrationScript = Scenario.GenerateLatestMigrationScript();

        // Assert
        migrationScript.ShouldContain(locale, StringComparison.Ordinal);
    }
}
