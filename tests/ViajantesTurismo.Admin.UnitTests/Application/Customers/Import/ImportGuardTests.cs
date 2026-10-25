using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class ImportGuardTests
{
    [Fact]
    public void Import_building_blocks_reject_missing_inputs_and_name_the_missing_parameter()
    {
        // Arrange
        var document = CsvDocument.Create(["Email"], []).Value;
        (string Parameter, Action Call)[] cases =
        [
            ("headers", () => CsvDocument.Create(null!, [])),
            ("rows", () => CsvDocument.Create(["Email"], null!)),
            ("document", () => DuplicateDetector.FindDuplicateEmailLineNumbers(null!)),
            ("document", () => RowToCustomerMapper.MapCustomer(null!, null!, new DateOnly(2026, 1, 1))),
            ("row", () => RowToCustomerMapper.MapCustomer(document, null!, new DateOnly(2026, 1, 1))),
        ];

        // Act and assert
        foreach (var (parameter, call) in cases)
        {
            call.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe(parameter);
        }
    }

    [Fact]
    public async Task The_import_command_handler_rejects_a_missing_command()
    {
        // Arrange
        var store = new FakeCustomerStore();
        var handler = new CustomerImportCommandHandler(store, new FakeUnitOfWork(), TimeProvider.System);
        Func<Task> handle = async () => await handler.Handle(null!, TestContext.Current.CancellationToken);

        // Act
        var exception = await handle.ShouldThrow<ArgumentNullException>();

        // Assert
        exception.ParamName.ShouldBe("command");
    }

    [Fact]
    public async Task The_import_command_handler_honors_a_cancelled_token_before_reading_the_csv()
    {
        // Arrange
        var store = new FakeCustomerStore();
        var handler = new CustomerImportCommandHandler(store, new FakeUnitOfWork(), TimeProvider.System);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        Func<Task> handle = async () => await handler.Handle(new CustomerImportCommand(string.Empty, false), cancellation.Token);

        // Act and assert
        await handle.ShouldThrow<OperationCanceledException>();
    }

    [Theory]
    [InlineData(-1, 0, "successCount")]
    [InlineData(0, -1, "errorCount")]
    public void An_import_result_rejects_negative_counts(int successCount, int errorCount, string parameter)
    {
        // Arrange
        Action create = () => _ = new ImportResult(successCount, errorCount);

        // Act
        var exception = create.ShouldThrow<ArgumentOutOfRangeException>();

        // Assert
        exception.ParamName.ShouldBe(parameter);
    }
}
