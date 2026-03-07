using ViajantesTurismo.Admin.Application.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class CsvRowParsingTests
{
    [Fact]
    public void Parse_Single_Customer_Row_Returns_Parsed_Row()
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
    public void Parse_With_Whitespace_Trims_Values()
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
    public void Parse_With_Valid_Index_Returns_Value()
    {
        // Arrange
        var row = CsvRow.Parse("John,Doe,john.doe@example.com");

        // Act
        var firstValue = row[0];

        // Assert
        Assert.Equal("John", firstValue);
    }

    [Fact]
    public void Index_OutOfRange_ThrowsException()
    {
        // Arrange
        var row = CsvRow.Parse("John,Doe,john.doe@example.com");

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => row[99]);
    }
}
