using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class CustomerImportCommandHandlerResolutionTests
{
    private const string Email = "existing@example.com";

    [Fact]
    public void Conflict_resolutions_state_whether_the_existing_customer_is_kept_and_the_import_skipped()
    {
        // Assert
        ConflictResolution.Keep.PreservesExistingCustomer.ShouldBeTrue();
        ConflictResolution.Keep.SkipsImport.ShouldBeTrue();
        ConflictResolution.Overwrite.PreservesExistingCustomer.ShouldBeFalse();
        ConflictResolution.Overwrite.SkipsImport.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_counts_a_row_for_an_existing_email_as_an_error_when_no_resolutions_are_given()
    {
        // Arrange
        var store = new FakeCustomerStore();
        var handler = new CustomerImportCommandHandler(store, new FakeUnitOfWork(), TimeProvider.System);
        var csv = CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Ana", "Silva", Email));
        (await handler.Handle(new CustomerImportCommand(csv, false), TestContext.Current.CancellationToken)).SuccessCount.ShouldBe(1);
        var again = CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Beatriz", "Silva", Email));

        // Act
        var result = await handler.Handle(new CustomerImportCommand(again, false), TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(0);
        result.ErrorCount.ShouldBe(1);
        store.AllCustomers.ShouldHaveSingleItem().PersonalInfo.FirstName.ShouldBe("Ana");
    }

    [Fact]
    public async Task Handle_counts_an_overwrite_as_an_error_when_the_existing_customer_cannot_be_loaded()
    {
        // Arrange
        var store = new FakeCustomerStore([Email]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CustomerImportCommandHandler(store, unitOfWork, TimeProvider.System);
        var csv = CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Ana", "Silva", Email));

        // Act
        var result = await handler.Handle(
            new CustomerImportCommand(csv, false),
            TestContext.Current.CancellationToken,
            new Dictionary<string, string> { [Email] = "overwrite" });

        // Assert
        result.SuccessCount.ShouldBe(0);
        result.ErrorCount.ShouldBe(1);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_stops_before_the_next_row_when_the_token_is_cancelled_while_reading_a_row()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var inner = new FakeCustomerStore();
        var unitOfWork = new FakeUnitOfWork();
        var store = new CancellingCustomerStore(inner, cancellation, cancelOnAdd: false);
        var handler = new CustomerImportCommandHandler(store, unitOfWork, TimeProvider.System);
        var csv = CustomerImportWorkflowScenario.Csv(
            CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "ana@example.com"),
            CustomerImportWorkflowCsvRows.Build("Bia", "Souza", "bia@example.com"));
        Func<Task> handle = async () => await handler.Handle(new CustomerImportCommand(csv, false), cancellation.Token);

        // Act
        await handle.ShouldThrow<OperationCanceledException>();

        // Assert
        inner.AllCustomers.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
        store.EmailExistsCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_adds_no_customer_when_the_token_is_cancelled_after_the_last_row_was_read()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var inner = new FakeCustomerStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CustomerImportCommandHandler(new CancellingCustomerStore(inner, cancellation, cancelOnAdd: false), unitOfWork, TimeProvider.System);
        var csv = CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "ana@example.com"));
        Func<Task> handle = async () => await handler.Handle(new CustomerImportCommand(csv, false), cancellation.Token);

        // Act
        await handle.ShouldThrow<OperationCanceledException>();

        // Assert
        inner.AllCustomers.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_does_not_save_when_the_token_is_cancelled_while_customers_are_added()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var inner = new FakeCustomerStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CustomerImportCommandHandler(new CancellingCustomerStore(inner, cancellation, cancelOnAdd: true), unitOfWork, TimeProvider.System);
        var csv = CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "ana@example.com"));
        Func<Task> handle = async () => await handler.Handle(new CustomerImportCommand(csv, false), cancellation.Token);

        // Act
        await handle.ShouldThrow<OperationCanceledException>();

        // Assert
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }
}
