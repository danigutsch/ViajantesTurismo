using ViajantesTurismo.Admin.Application.Customers.Import;

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
        Assert.Equal("John", result.Values[0]);
        Assert.Equal("Doe", result.Values[1]);
        Assert.Equal("john.doe@example.com", result.Values[2]);
    }

    [Fact]
    public void Parse_With_Whitespace_Trims_Values()
    {
        // Arrange
        const string csvLine = " John , Doe , john.doe@example.com ";

        // Act
        var result = CsvRow.Parse(csvLine);

        // Assert
        Assert.Equal("John", result.Values[0]);
        Assert.Equal("Doe", result.Values[1]);
        Assert.Equal("john.doe@example.com", result.Values[2]);
    }
}
