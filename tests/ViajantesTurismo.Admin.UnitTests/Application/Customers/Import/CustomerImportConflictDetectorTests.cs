using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class CustomerImportConflictDetectorTests
{
    [Fact]
    public async Task Unreadable_csv_content_reports_no_conflicts()
    {
        // Arrange
        var detector = new CustomerImportConflictDetector(new FakeCustomerStore(["existing@example.com"]));

        // Act
        var conflicts = await detector.FindDatabaseEmailConflicts(string.Empty, TestContext.Current.CancellationToken);

        // Assert
        conflicts.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_file_without_repeated_or_existing_emails_reports_no_conflicts()
    {
        // Arrange
        var detector = new CustomerImportConflictDetector(new FakeCustomerStore(["existing@example.com"]));
        var csv = CustomerImportWorkflowScenario.Csv(
            CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "ana@example.com"),
            CustomerImportWorkflowCsvRows.Build("Bia", "Souza", "bia@example.com"));

        // Act
        var conflicts = await detector.FindDatabaseEmailConflicts(csv, TestContext.Current.CancellationToken);

        // Assert
        conflicts.ShouldBeEmpty();
    }

    [Fact]
    public async Task Conflicts_follow_the_order_of_the_file_lines()
    {
        // Arrange
        var detector = new CustomerImportConflictDetector(new FakeCustomerStore(["a@example.com", "b@example.com"]));
        var csv = CustomerImportWorkflowScenario.Csv(
            CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "b@example.com"),
            CustomerImportWorkflowCsvRows.Build("Bia", "Souza", "a@example.com"));

        // Act
        var conflicts = await detector.FindDatabaseEmailConflicts(csv, TestContext.Current.CancellationToken);

        // Assert
        conflicts.Select(conflict => conflict.Email).ShouldBe(["b@example.com", "a@example.com"]);
    }

    [Fact]
    public async Task An_email_repeated_on_several_lines_is_reported_once()
    {
        // Arrange
        var detector = new CustomerImportConflictDetector(new FakeCustomerStore());
        var csv = CustomerImportWorkflowScenario.Csv(
            CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "same@example.com"),
            CustomerImportWorkflowCsvRows.Build("Bia", "Souza", "same@example.com"),
            CustomerImportWorkflowCsvRows.Build("Clara", "Lima", "same@example.com"));

        // Act
        var conflicts = await detector.FindDatabaseEmailConflicts(csv, TestContext.Current.CancellationToken);

        // Assert
        conflicts.ShouldHaveSingleItem().Email.ShouldBe("same@example.com");
    }

    [Fact]
    public async Task A_repeated_name_with_a_blank_email_is_not_reported_as_an_email_conflict()
    {
        // Arrange
        var detector = new CustomerImportConflictDetector(new FakeCustomerStore());
        var csv = CustomerImportWorkflowScenario.Csv(
            CustomerImportWorkflowCsvRows.Build("Ana", "Silva", "ana@example.com"),
            CustomerImportWorkflowCsvRows.Build("Ana", "Silva", string.Empty));

        // Act
        var conflicts = await detector.FindDatabaseEmailConflicts(csv, TestContext.Current.CancellationToken);

        // Assert
        conflicts.ShouldBeEmpty();
    }
}
