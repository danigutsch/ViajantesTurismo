using ViajantesTurismo.Admin.Application.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class CsvRowParsingTests
{
    [Fact]
    public void Create_single_customer_row_keeps_values_in_order()
    {
        // Arrange
        string[] values = ["John", "Doe", "john.doe@example.com"];

        // Act
        var result = CsvRow.Create(values);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].ShouldBe("John");
        result[1].ShouldBe("Doe");
        result[2].ShouldBe("john.doe@example.com");
    }

    [Fact]
    public void Index_outofrange_throwsexception()
    {
        // Arrange
        var row = CsvRow.Create(["John", "Doe", "john.doe@example.com"]);

        // Act & Assert
        ShouldAssertionExtensions.ShouldThrow<IndexOutOfRangeException>(() => row[99]);
    }
}
