using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class CustomerImportWorkflowEdgeTests
{
    [Fact]
    public async Task Commit_reports_a_retry_error_when_persistence_rejects_an_import_without_success_candidates()
    {
        // Arrange
        var inner = new FakeCustomerStore();
        var store = new ScriptedCustomerStore(inner, call => call == 1);
        var unitOfWork = new ConcurrentCustomerEmailConflictUnitOfWork(inner, "other@example.com");
        var handler = new CustomerImportCommandHandler(store, unitOfWork, TimeProvider.System);
        var sut = new CustomerImportWorkflowService(store, new CustomerImportConflictDetector(store), handler, TimeProvider.System);
        var csv = CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "ana@example.com"));

        // Act
        var result = await sut.Commit(csv, new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(0);
        result.ErrorCount.ShouldBe(1);
        var error = result.ErrorRows.ShouldNotBeNull().ShouldHaveSingleItem();
        error.LineNumber.ShouldBe(1);
        error.Field.ShouldBe("Email");
        error.Message.ShouldBe("Persistence rejected a duplicate Customer email. Retry the import.");
    }

    [Fact]
    public async Task Import_requires_a_resolution_when_the_email_appears_in_the_database_after_conflict_detection()
    {
        // Arrange
        var inner = new FakeCustomerStore();
        var store = new ScriptedCustomerStore(inner, call => call == 2 ? true : null);
        var handler = new CustomerImportCommandHandler(store, new FakeUnitOfWork(), TimeProvider.System);
        var sut = new CustomerImportWorkflowService(store, new CustomerImportConflictDetector(store), handler, TimeProvider.System);
        var csv = CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "ana@example.com"));

        // Act
        var result = await sut.Import(csv, TestContext.Current.CancellationToken);

        // Assert
        var error = result.ErrorRows.ShouldNotBeNull().ShouldHaveSingleItem();
        error.LineNumber.ShouldBe(2);
        error.Message.ShouldBe("Conflict requires resolution.");
        error.Email.ShouldBe("ana@example.com");
    }

    [Fact]
    public async Task Commit_leaves_out_success_rows_for_customers_the_store_no_longer_returns()
    {
        // Arrange
        var inner = new FakeCustomerStore();
        var store = new ScriptedCustomerStore(inner, hideCustomers: true);
        var handler = new CustomerImportCommandHandler(store, new FakeUnitOfWork(), TimeProvider.System);
        var sut = new CustomerImportWorkflowService(store, new CustomerImportConflictDetector(store), handler, TimeProvider.System);
        var csv = CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "ana@example.com"));

        // Act
        var result = await sut.Commit(csv, new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(1);
        result.SuccessRows.ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    public async Task Conflict_detection_does_not_look_up_rows_without_an_email()
    {
        // Arrange
        var store = new ScriptedCustomerStore(new FakeCustomerStore());
        var detector = new CustomerImportConflictDetector(store);
        var csv = CustomerImportWorkflowScenario.Csv(
            CustomerImportWorkflowCsvRows.Build("Ana", "Silva", string.Empty),
            CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "   "));

        // Act
        var conflicts = await detector.FindDatabaseEmailConflicts(csv, TestContext.Current.CancellationToken);

        // Assert
        conflicts.ShouldBeEmpty();
        store.EmailExistsCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Import_reports_the_message_of_the_first_invalid_field()
    {
        // Arrange
        var store = new FakeCustomerStore();
        var handler = new CustomerImportCommandHandler(store, new FakeUnitOfWork(), TimeProvider.System);
        var sut = new CustomerImportWorkflowService(store, new CustomerImportConflictDetector(store), handler, TimeProvider.System);
        var csv = CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build(string.Empty, string.Empty, "ana@example.com"));

        // Act
        var result = await sut.Import(csv, TestContext.Current.CancellationToken);

        // Assert
        var error = result.ErrorRows.ShouldNotBeNull().ShouldHaveSingleItem();
        error.Field.ShouldNotBeNull();
        error.Message.ShouldNotBe("Multiple validation errors occurred.");
    }
}
