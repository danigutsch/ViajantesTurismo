using ViajantesTurismo.Admin.Application.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class CsvRowParsingTests
{
    [Fact]
    public void Parse_single_customer_row_returns_parsed_row()
    {
        // Arrange
        const string csvLine = "John,Doe,john.doe@example.com";

        // Act
        var result = CsvRow.Parse(csvLine);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result[0]);
        Assert.Equal("Doe", result[1]);
        Assert.Equal("john.doe@example.com", result[2]);
    }

    [Fact]
    public void Parse_with_whitespace_trims_values()
    {
        // Arrange
        const string csvLine = " John , Doe , john.doe@example.com ";

        // Act
        var result = CsvRow.Parse(csvLine);

        // Assert
        Assert.Equal("John", result[0]);
        Assert.Equal("Doe", result[1]);
        Assert.Equal("john.doe@example.com", result[2]);
    }

    [Fact]
    public void Index_outOfRange_throwsException()
    {
        // Arrange
        var row = CsvRow.Parse("John,Doe,john.doe@example.com");

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => row[99]);
    }
}
