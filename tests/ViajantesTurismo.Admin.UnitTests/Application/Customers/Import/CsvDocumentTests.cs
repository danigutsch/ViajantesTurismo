using ViajantesTurismo.Admin.Application.Customers.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class CsvDocumentTests
{
    [Fact]
    public void Create_With_Row_Exposes_Row_In_Collection()
    {
        // Arrange
        CsvRow[] rows = [CsvRow.Parse("John,Doe,john.doe@example.com")];

        // Act
        var document = CsvDocument.Create(rows);

        // Assert
        var row = Assert.Single(document.Rows);
        Assert.Equal("John", row[0]);
        Assert.Equal("Doe", row[1]);
        Assert.Equal("john.doe@example.com", row[2]);
    }
}
