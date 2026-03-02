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

    [Fact]
    public void Create_With_Multiple_Row_Exposes_Rows_In_Collection()
    {
        // Arrange
        CsvRow[] rows = [
            CsvRow.Parse("John,Doe,john.doe@example.com"),
            CsvRow.Parse("Jane,Smith,jane.smith@example.com"),
            CsvRow.Parse("Alice,Johnson,alice.johnson@example.com")
        ];

        // Act
        var document = CsvDocument.Create(rows);

        // Assert
        Assert.Equal(3, document.Rows.Count);

        var row1 = document.Rows[0];
        Assert.Equal("John", row1[0]);
        Assert.Equal("Doe", row1[1]);
        Assert.Equal("john.doe@example.com", row1[2]);

        var row2 = document.Rows[1];
        Assert.Equal("Jane", row2[0]);
        Assert.Equal("Smith", row2[1]);
        Assert.Equal("jane.smith@example.com", row2[2]);

        var row3 = document.Rows[2];
        Assert.Equal("Alice", row3[0]);
        Assert.Equal("Johnson", row3[1]);
        Assert.Equal("alice.johnson@example.com", row3[2]);
    }
}
