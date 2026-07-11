using ViajantesTurismo.Admin.Application.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class CsvDocumentTests
{
    [Fact]
    public void Create_with_headers_and_row_exposes_both_collections()
    {
        // Arrange
        string[] headers = ["FirstName", "LastName", "Email"];
        CsvRow[] rows = [CsvRow.Parse("John,Doe,john.doe@example.com")];

        // Act
        var documentResult = CsvDocument.Create(headers, rows);

        // Assert
        TestAssert.True(documentResult.IsSuccess);

        var document = documentResult.Value;
        TestAssert.Equal(3, document.Headers.Count);
        TestAssert.Equal("FirstName", document.Headers[0]);
        TestAssert.Equal("LastName", document.Headers[1]);
        TestAssert.Equal("Email", document.Headers[2]);

        var row = TestAssert.ExactlyOne(document.Rows);
        TestAssert.Equal("John", row[0]);
        TestAssert.Equal("Doe", row[1]);
        TestAssert.Equal("john.doe@example.com", row[2]);
    }

    [Fact]
    public void Parse_with_header_and_row_parses_document()
    {
        // Arrange
        const string csvContent = "FirstName,LastName,Email\nJohn,Doe,john.doe@example.com";

        // Act
        var documentResult = CsvDocument.Parse(csvContent);

        // Assert
        TestAssert.True(documentResult.IsSuccess);

        var document = documentResult.Value;
        TestAssert.Equal(3, document.Headers.Count);
        TestAssert.Equal("FirstName", document.Headers[0]);
        TestAssert.Equal("LastName", document.Headers[1]);
        TestAssert.Equal("Email", document.Headers[2]);

        var row = TestAssert.ExactlyOne(document.Rows);
        TestAssert.Equal("John", row[0]);
        TestAssert.Equal("Doe", row[1]);
        TestAssert.Equal("john.doe@example.com", row[2]);
    }

    [Fact]
    public void Parse_with_multiple_rows_parses_document()
    {
        // Arrange
        const string csvContent = "FirstName,LastName,Email\nJohn,Doe,john.doe@example.com\nJane,Smith,jane.smith@example.com\nAlice,Johnson,alice.johnson@example.com";

        // Act
        var documentResult = CsvDocument.Parse(csvContent);

        // Assert
        TestAssert.True(documentResult.IsSuccess);

        var document = documentResult.Value;
        TestAssert.Equal(3, document.Rows.Count);

        var row1 = document.Rows[0];
        TestAssert.Equal("John", row1[0]);
        TestAssert.Equal("Doe", row1[1]);
        TestAssert.Equal("john.doe@example.com", row1[2]);

        var row2 = document.Rows[1];
        TestAssert.Equal("Jane", row2[0]);
        TestAssert.Equal("Smith", row2[1]);
        TestAssert.Equal("jane.smith@example.com", row2[2]);

        var row3 = document.Rows[2];
        TestAssert.Equal("Alice", row3[0]);
        TestAssert.Equal("Johnson", row3[1]);
        TestAssert.Equal("alice.johnson@example.com", row3[2]);
    }

    [Fact]
    public void Create_with_rows_of_different_lengths_fails()
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
        TestAssert.False(documentResult.IsSuccess);
        TestAssert.NotNull(documentResult.ErrorDetails);
        TestAssert.Contains("All rows must have the same number of columns", documentResult.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_with_empty_headers_fails()
    {
        // Arrange
        string[] headers = [];
        CsvRow[] rows = [CsvRow.Parse("John,Doe,john.doe@example.com")];

        // Act
        var documentResult = CsvDocument.Create(headers, rows);

        // Assert
        TestAssert.False(documentResult.IsSuccess);
        TestAssert.NotNull(documentResult.ErrorDetails);
        TestAssert.Contains("Headers must contain at least one column", documentResult.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_with_header_count_different_from_row_count_fails()
    {
        // Arrange
        string[] headers = ["FirstName", "LastName", "Email", "Phone"];
        CsvRow[] rows = [CsvRow.Parse("John,Doe,john.doe@example.com")];

        // Act
        var documentResult = CsvDocument.Create(headers, rows);

        // Assert
        TestAssert.False(documentResult.IsSuccess);
        TestAssert.NotNull(documentResult.ErrorDetails);
        TestAssert.Contains("Header count must match row column count", documentResult.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_with_missing_required_header_fails()
    {
        // Arrange
        const string csvContent = "FirstName,LastName\nJohn,Doe";
        string[] requiredHeaders = ["CustomerCode"];

        // Act
        var documentResult = CsvDocument.Parse(csvContent, requiredHeaders);

        // Assert
        TestAssert.False(documentResult.IsSuccess);
        TestAssert.NotNull(documentResult.ErrorDetails);
        TestAssert.Contains("Required header 'CustomerCode' is missing", documentResult.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_with_empty_csvcontent_fails()
    {
        // Arrange
        const string csvContent = "";

        // Act
        var documentResult = CsvDocument.Parse(csvContent);

        // Assert
        TestAssert.False(documentResult.IsSuccess);
        TestAssert.NotNull(documentResult.ErrorDetails);
        TestAssert.Contains("Headers must contain at least one column", documentResult.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_with_requiredheader_different_casing_and_whitespace_succeeds()
    {
        // Arrange
        const string csvContent = "FirstName,LastName,Email\nJohn,Doe,john.doe@example.com";
        string[] requiredHeaders = ["  email  "];

        // Act
        var documentResult = CsvDocument.Parse(csvContent, requiredHeaders);

        // Assert
        TestAssert.True(documentResult.IsSuccess);
    }

    [Fact]
    public void Parse_with_empty_data_row_does_not_ignore_it_and_fails_validation()
    {
        // Arrange
        const string csvContent = "FirstName,LastName,Email\n\nJohn,Doe,john.doe@example.com";

        // Act
        var documentResult = CsvDocument.Parse(csvContent);

        // Assert
        TestAssert.False(documentResult.IsSuccess);
        TestAssert.NotNull(documentResult.ErrorDetails);
        TestAssert.Contains("All rows must have the same number of columns", documentResult.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_with_blank_requiredheadernames_ignores_them()
    {
        // Arrange
        const string csvContent = "FirstName,LastName,Email\nJohn,Doe,john.doe@example.com";
        string[] requiredHeaders = ["   ", "Email"];

        // Act
        var documentResult = CsvDocument.Parse(csvContent, requiredHeaders);

        // Assert
        TestAssert.True(documentResult.IsSuccess);
    }

    [Fact]
    public void Create_with_rows_of_different_lengths_includes_csv_line_number_in_error_detail()
    {
        // Arrange
        CsvRow[] rows =
        [
            CsvRow.Parse("John,Doe,john.doe@example.com"),
            CsvRow.Parse("Jane,jane.smith@example.com")
        ];

        // Act
        var documentResult = CsvDocument.Create(["FirstName", "LastName", "Email"], rows);

        // Assert
        TestAssert.False(documentResult.IsSuccess);
        TestAssert.NotNull(documentResult.ErrorDetails);
        TestAssert.Contains("line 3", documentResult.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void String_indexer_with_existing_header_returns_row_value()
    {
        // Arrange
        var documentResult = CsvDocument.Create(
            headers: ["FirstName", "LastName", "Email"],
            rows: [CsvRow.Parse("John,Doe,john.doe@example.com")]
        );

        var document = documentResult.Value;
        var row = document.Rows[0];

        // Act
        var email = row[document.Headers, "Email"];

        // Assert
        TestAssert.Equal("john.doe@example.com", email);
    }

    [Fact]
    public void TryGetByHeader_with_missing_header_returns_false()
    {
        // Arrange
        var documentResult = CsvDocument.Create(
            headers: ["FirstName", "LastName", "Email"],
            rows: [CsvRow.Parse("John,Doe,john.doe@example.com")]
        );

        var document = documentResult.Value;
        var row = document.Rows[0];

        // Act
        var success = row.TryGetByHeader(document.Headers, "CustomerCode", out var value);

        // Assert
        TestAssert.False(success);
        TestAssert.Null(value);
    }
}
