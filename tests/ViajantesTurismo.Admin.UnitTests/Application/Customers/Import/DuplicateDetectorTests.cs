using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Tests.Shared.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class DuplicateDetectorTests
{
    [Fact]
    public void FindDuplicateEmailLineNumbers_With_Duplicate_Email_Flags_Second_Row()
    {
        // Arrange
        var headers = new[] { "FirstName", "LastName", "Email" };
        var rows = new[]
        {
            CsvRow.Parse("John,Doe,john.doe@example.com"),
            CsvRow.Parse("Jane,Doe,  JOHN.DOE@example.com  ")
        };

        var documentResult = CsvDocument.Create(headers, rows);
        var document = documentResult.Value;

        // Act
        var duplicateLineNumbers = DuplicateDetector.FindDuplicateEmailLineNumbers(document);

        // Assert
        var duplicateLineNumber = Assert.Single(duplicateLineNumbers);
        Assert.Equal(3, duplicateLineNumber);
    }

    [Fact]
    public void FindDuplicateNameLineNumbers_With_Diacritics_Variation_Flags_Second_Row()
    {
        // Arrange
        var headers = new[] { "FirstName", "LastName", "Email" };
        var rows = new[]
        {
            CsvRow.Parse("José,Silva,jose.silva@example.com"),
            CsvRow.Parse("Jose,Silva,jose2.silva@example.com")
        };

        var documentResult = CsvDocument.Create(headers, rows);
        var document = documentResult.Value;

        // Act
        var duplicateLineNumbers = DuplicateDetector.FindDuplicateNameLineNumbers(document);

        // Assert
        var duplicateLineNumber = Assert.Single(duplicateLineNumbers);
        Assert.Equal(3, duplicateLineNumber);
    }

    [Fact]
    public async Task FindDuplicateEmailLineNumbersAgainstDatabaseAsync_With_Matching_Email_Flags_Row()
    {
        // Arrange
        var headers = new[] { "FirstName", "LastName", "Email" };
        var rows = new[]
        {
            CsvRow.Parse("John,Doe,john.doe@example.com")
        };

        var documentResult = CsvDocument.Create(headers, rows);
        var document = documentResult.Value;
        var store = new FakeCustomerStore(["JOHN.DOE@EXAMPLE.COM"]);

        // Act
        var duplicateLineNumbers = await DuplicateDetector.FindDuplicateEmailLineNumbersAgainstDatabase(
            document,
            store,
            CancellationToken.None);

        // Assert
        var duplicateLineNumber = Assert.Single(duplicateLineNumbers);
        Assert.Equal(2, duplicateLineNumber);
    }
}
