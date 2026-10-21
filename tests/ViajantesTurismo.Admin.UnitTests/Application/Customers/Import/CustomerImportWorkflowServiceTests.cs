using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class CustomerImportWorkflowServiceTests
{
    private const string CsvHeaders =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation,NationalId,IdNationality," +
        "Email,Mobile,Street,Neighborhood,PostalCode,City,State,Country," +
        "WeightKg,HeightCentimeters,BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

    [Theory]
    [InlineData(1999, 12, 31, 0, 1)]
    [InlineData(2000, 1, 1, 1, 0)]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, "Regression")]
    [Trait(PandaBear.Testing.TestTraitNames.ScopeName, PandaBear.Testing.SharedKernelTestTraitNames.UnitScope)]
    public async Task Import_captures_one_date_for_summary_and_persistence_across_midnight(int year, int month, int day, int successes, int errors)
    {
        // Arrange
        var clock = new FakeTimeProvider(new DateTimeOffset(year, month, day, 23, 59, 59, TimeSpan.Zero))
        {
            AutoAdvance = TimeSpan.FromDays(1)
        };
        var store = new FakeCustomerStore();
        var handler = new CustomerImportCommandHandler(store, new FakeUnitOfWork(), clock);
        var sut = new CustomerImportWorkflowService(store, new CustomerImportConflictDetector(store), handler, clock);
        var csv = $"{CsvHeaders}\n{CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "birthday@example.com")}";
        // Act
        var result = await sut.Import(csv, TestContext.Current.CancellationToken);
        // Assert
        result.SuccessCount.ShouldBe(successes);
        result.ErrorCount.ShouldBe(errors);
        (result.SuccessRows?.Count ?? 0).ShouldBe(successes);
        (result.ErrorRows?.Count ?? 0).ShouldBe(errors);
        store.AllCustomers.Count.ShouldBe(successes);
        clock.ReadCount.ShouldBe(1);
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, "Regression")]
    [Trait(PandaBear.Testing.TestTraitNames.ScopeName, PandaBear.Testing.SharedKernelTestTraitNames.UnitScope)]
    public async Task Import_summary_and_persistence_reject_the_same_underage_customer()
    {
        // Arrange
        var clock = new FakeTimeProvider(new DateTimeOffset(1999, 12, 31, 23, 59, 59, TimeSpan.Zero));
        var store = new FakeCustomerStore();
        var handler = new CustomerImportCommandHandler(store, new FakeUnitOfWork(), clock);
        var sut = new CustomerImportWorkflowService(store, new CustomerImportConflictDetector(store), handler, clock);
        var csv = $"{CsvHeaders}\n{CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "boundary@example.com")}";
        // Act
        var result = await sut.Import(csv, TestContext.Current.CancellationToken);
        // Assert
        result.SuccessCount.ShouldBe(0);
        result.ErrorCount.ShouldBe(1);
        result.ErrorRows.ShouldNotBeNull().ShouldHaveSingleItem().Field.ShouldBe("BirthDate");
        store.AllCustomers.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportAsync_with_fileanddatabaseduplicates_reports_conflicts_using_configured_normalization_rules()
    {
        // Arrange
        const string firstEmail = "jose.silva@example.com";
        const string nameDuplicateEmail = "jose.silva.2@example.com";
        const string dbDuplicateEmail = "existing@example.com";

        var csv =
            $"""
             {CsvHeaders}
             {CustomerImportWorkflowCsvRows.Build("José", "Silva", firstEmail)}
             {CustomerImportWorkflowCsvRows.Build("Jose", "Silva", nameDuplicateEmail)}
             {CustomerImportWorkflowCsvRows.Build("Maria", "Souza", dbDuplicateEmail)}
             """;

        var store = new FakeCustomerStore([dbDuplicateEmail]);
        var unitOfWork = new FakeUnitOfWork();
        var conflictDetector = new CustomerImportConflictDetector(store);
        var handler = new CustomerImportCommandHandler(store, unitOfWork, TimeProvider.System);
        var sut = new CustomerImportWorkflowService(store, conflictDetector, handler, TimeProvider.System);

        // Act
        var result = await sut.Import(csv, CancellationToken.None);

        // Assert
        (result.Conflicts).ShouldNotBeNull();
        (result.Conflicts.Count).ShouldBe(2);
        (result.Conflicts).ShouldContain(c => c.Email.Equals(nameDuplicateEmail, StringComparison.OrdinalIgnoreCase));
        (result.Conflicts).ShouldContain(c => c.Email.Equals(dbDuplicateEmail, StringComparison.OrdinalIgnoreCase));
        (result.SuccessCount).ShouldBe(0);
        (result.ErrorCount).ShouldBe(0);
    }

    [Fact]
    public async Task Import_reports_a_concurrent_persistence_email_conflict_without_false_success()
    {
        // Arrange
        const string email = "concurrent@example.com";
        var csv = $"{CsvHeaders}\n{CustomerImportWorkflowCsvRows.Build("Maria", "Souza", email)}";
        var store = new FakeCustomerStore();
        var conflictDetector = new CustomerImportConflictDetector(store);
        var unitOfWork = new ConcurrentCustomerEmailConflictUnitOfWork(store, email);
        var handler = new CustomerImportCommandHandler(store, unitOfWork, TimeProvider.System);
        var sut = new CustomerImportWorkflowService(store, conflictDetector, handler, TimeProvider.System);

        // Act
        var result = await sut.Import(csv, TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(0);
        result.SuccessRows.ShouldBeNull();
        result.Conflicts.ShouldNotBeNull();
        result.Conflicts.ShouldHaveSingleItem(conflict =>
            conflict.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Commit_reports_a_concurrent_persistence_email_conflict_without_false_success()
    {
        // Arrange
        const string email = "concurrent-commit@example.com";
        var csv = $"{CsvHeaders}\n{CustomerImportWorkflowCsvRows.Build("Maria", "Souza", email)}";
        var store = new FakeCustomerStore();
        var conflictDetector = new CustomerImportConflictDetector(store);
        var unitOfWork = new ConcurrentCustomerEmailConflictUnitOfWork(store, email);
        var handler = new CustomerImportCommandHandler(store, unitOfWork, TimeProvider.System);
        var sut = new CustomerImportWorkflowService(store, conflictDetector, handler, TimeProvider.System);

        // Act
        var result = await sut.Commit(csv, new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(0);
        result.SuccessRows.ShouldBeNull();
        result.Conflicts.ShouldNotBeNull();
        result.Conflicts.ShouldHaveSingleItem(conflict =>
            conflict.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Commit_returns_a_retry_error_when_the_competing_record_disappears()
    {
        // Arrange
        const string email = "transient-conflict@example.com";
        var csv = $"{CsvHeaders}\n{CustomerImportWorkflowCsvRows.Build("Maria", "Souza", email)}";
        var store = new FakeCustomerStore();
        var conflictDetector = new CustomerImportConflictDetector(store);
        var unitOfWork = new RolledBackCustomerEmailConflictUnitOfWork(store);
        var handler = new CustomerImportCommandHandler(store, unitOfWork, TimeProvider.System);
        var sut = new CustomerImportWorkflowService(store, conflictDetector, handler, TimeProvider.System);

        // Act
        var result = await sut.Commit(csv, new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(0);
        result.ErrorCount.ShouldBe(1);
        result.SuccessRows.ShouldBeNull();
        result.Conflicts.ShouldBeNull();
        var errorRows = result.ErrorRows.ShouldNotBeNull();
        var error = errorRows.ShouldHaveSingleItem();
        error.LineNumber.ShouldBe(2);
        error.Email.ShouldBe(email);
        error.Message.ShouldContain("Retry", StringComparison.Ordinal);
    }
}
