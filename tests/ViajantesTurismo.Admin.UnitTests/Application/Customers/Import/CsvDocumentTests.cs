using ViajantesTurismo.Admin.Application.Customers.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class CsvDocumentTests
{
    [Fact]
    public void Create_With_Headers_And_Row_Exposes_Both_Collections()
    {
        // Arrange
        string[] headers = ["FirstName", "LastName", "Email"];
        CsvRow[] rows = [CsvRow.Parse("John,Doe,john.doe@example.com")];

        // Act
        var documentResult = CsvDocument.Create(headers, rows);

        // Assert
        Assert.True(documentResult.IsSuccess);

        var document = documentResult.Value;
        Assert.Equal(3, document.Headers.Count);
        Assert.Equal("FirstName", document.Headers[0]);
        Assert.Equal("LastName", document.Headers[1]);
        Assert.Equal("Email", document.Headers[2]);

        var row = Assert.Single(document.Rows);
        Assert.Equal("John", row[0]);
        Assert.Equal("Doe", row[1]);
        Assert.Equal("john.doe@example.com", row[2]);
    }

    [Fact]
    public void Parse_With_Header_And_Row_Parses_Document()
    {
        // Arrange
        const string csvContent = "FirstName,LastName,Email\nJohn,Doe,john.doe@example.com";

        // Act
        var documentResult = CsvDocument.Parse(csvContent);

        // Assert
        Assert.True(documentResult.IsSuccess);

        var document = documentResult.Value;
        Assert.Equal(3, document.Headers.Count);
        Assert.Equal("FirstName", document.Headers[0]);
        Assert.Equal("LastName", document.Headers[1]);
        Assert.Equal("Email", document.Headers[2]);

        var row = Assert.Single(document.Rows);
        Assert.Equal("John", row[0]);
        Assert.Equal("Doe", row[1]);
        Assert.Equal("john.doe@example.com", row[2]);
    }

    [Fact]
    public void Parse_With_Multiple_Rows_Parses_Document()
    {
        // Arrange
        const string csvContent = "FirstName,LastName,Email\nJohn,Doe,john.doe@example.com\nJane,Smith,jane.smith@example.com\nAlice,Johnson,alice.johnson@example.com";

        // Act
        var documentResult = CsvDocument.Parse(csvContent);

        // Assert
        Assert.True(documentResult.IsSuccess);

        var document = documentResult.Value;
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

    [Fact]
    public void Create_With_Rows_Of_Different_Lengths_Fails()
    {
        // Arrange
        var differentLengthRow = CsvRow.Parse("Alice,alice.johnson@example.com");
        CsvRow[] rows =
        [
            CsvRow.Parse("John,Doe,john.doe@example.com"),
            CsvRow.Parse("Jane,Smith,jane.smith@example.com"),
            differentLengthRow
        ];

        // Act
        var documentResult = CsvDocument.Create(["FirstName", "LastName", "Email"], rows);

        // Assert
        Assert.False(documentResult.IsSuccess);
        Assert.NotNull(documentResult.ErrorDetails);
        Assert.Contains("All rows must have the same number of columns", documentResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Create_With_Empty_Headers_Fails()
    {
        // Arrange
        string[] headers = [];
        CsvRow[] rows = [CsvRow.Parse("John,Doe,john.doe@example.com")];

        // Act
        var documentResult = CsvDocument.Create(headers, rows);

        // Assert
        Assert.False(documentResult.IsSuccess);
        Assert.NotNull(documentResult.ErrorDetails);
        Assert.Contains("Headers must contain at least one column", documentResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Create_With_Header_Count_Different_From_Row_Count_Fails()
    {
        // Arrange
        string[] headers = ["FirstName", "LastName", "Email", "Phone"];
        CsvRow[] rows = [CsvRow.Parse("John,Doe,john.doe@example.com")];

        // Act
        var documentResult = CsvDocument.Create(headers, rows);

        // Assert
        Assert.False(documentResult.IsSuccess);
        Assert.NotNull(documentResult.ErrorDetails);
        Assert.Contains("Header count must match row column count", documentResult.ErrorDetails.Detail);
    }

    [Fact]
    public void Parse_With_Missing_Required_Header_Fails()
    {
        // Arrange
        const string csvContent = "FirstName,LastName\nJohn,Doe";
        string[] requiredHeaders = ["CustomerCode"];

        // Act
        var documentResult = CsvDocument.Parse(csvContent, requiredHeaders);

        // Assert
        Assert.False(documentResult.IsSuccess);
        Assert.NotNull(documentResult.ErrorDetails);
        Assert.Contains("Required header 'CustomerCode' is missing", documentResult.ErrorDetails.Detail);
    }
}
