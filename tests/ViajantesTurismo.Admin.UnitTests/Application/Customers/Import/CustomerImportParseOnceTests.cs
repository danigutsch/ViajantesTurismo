using System.Text;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class CustomerImportParseOnceTests
{
    private static readonly string TwoRowCsv = CustomerImportWorkflowScenario.Csv(
        CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "ana@example.com"),
        CustomerImportWorkflowCsvRows.Build("Bia", "Souza", "bia@example.com"));

    [Fact]
    public async Task The_conflict_detector_accepts_an_already_parsed_document()
    {
        // Arrange
        var detector = new CustomerImportConflictDetector(new FakeCustomerStore(["ana@example.com"]));
        var document = CsvDocument.Parse(TwoRowCsv).Value;

        // Act
        var conflicts = await detector.FindDatabaseEmailConflicts(document, TestContext.Current.CancellationToken);

        // Assert
        conflicts.ShouldHaveSingleItem().Email.ShouldBe("ana@example.com");
    }

    [Fact]
    public async Task The_command_handler_imports_the_parsed_document_instead_of_the_csv_text()
    {
        // Arrange
        var store = new FakeCustomerStore();
        var handler = new CustomerImportCommandHandler(store, new FakeUnitOfWork(), TimeProvider.System);
        var document = CsvDocument.Parse(TwoRowCsv).Value;
        var command = new CustomerImportCommand(string.Empty, false) { Document = document };

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(2);
        result.ErrorCount.ShouldBe(0);
    }

    [Fact]
    public async Task Importing_utf8_bytes_matches_importing_the_decoded_text()
    {
        // Arrange
        var fromText = new CustomerImportWorkflowScenario();
        var fromBytes = new CustomerImportWorkflowScenario();
        var csv = TwoRowCsv;

        // Act
        var textResult = await fromText.Service.Import(csv, TestContext.Current.CancellationToken);
        var byteResult = await fromBytes.Service.Import(Encoding.UTF8.GetBytes(csv), TestContext.Current.CancellationToken);

        // Assert
        byteResult.SuccessCount.ShouldBe(textResult.SuccessCount);
        byteResult.ErrorCount.ShouldBe(textResult.ErrorCount);
        byteResult.SuccessRows.ShouldNotBeNull().Select(row => row.Email)
            .ShouldBe(textResult.SuccessRows.ShouldNotBeNull().Select(row => row.Email));
    }

    [Fact]
    public async Task Committing_utf8_bytes_applies_the_resolutions_like_the_decoded_text()
    {
        // Arrange
        var scenario = new CustomerImportWorkflowScenario();
        var csv = TwoRowCsv;

        // Act
        var result = await scenario.Service.Commit(
            Encoding.UTF8.GetBytes(csv),
            new Dictionary<string, string>(),
            TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(2);
        result.ErrorCount.ShouldBe(0);
    }

    [Fact]
    public async Task Importing_unreadable_bytes_reports_the_same_csv_error_as_unreadable_text()
    {
        // Arrange
        var fromText = new CustomerImportWorkflowScenario();
        var fromBytes = new CustomerImportWorkflowScenario();

        // Act
        var textResult = await fromText.Service.Import(string.Empty, TestContext.Current.CancellationToken);
        var byteResult = await fromBytes.Service.Import(ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken);

        // Assert
        byteResult.SuccessCount.ShouldBe(0);
        var error = byteResult.ErrorRows.ShouldNotBeNull().ShouldHaveSingleItem();
        error.Field.ShouldBe("Csv");
        error.Message.ShouldBe(textResult.ErrorRows.ShouldNotBeNull().ShouldHaveSingleItem().Message);
    }
}
