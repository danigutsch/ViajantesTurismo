using System.Text;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
public sealed class ImportCustomersDuplicateResolutionTestHelperTests
{
    [Fact]
    public void Parse_single_data_row_rejects_an_invalid_row_count()
    {
        // Arrange
        var fileContent = Encoding.UTF8.GetBytes("Email\n");

        // Act
        Action parse = () => ImportCustomersDuplicateResolutionTestHelper.ParseSingleDataRow(fileContent);
        var exception = parse.ShouldThrow<InvalidDataException>();

        // Assert
        exception.Message.ShouldBe("Expected exactly one header row and one data row, but found 1 non-empty row(s).");
    }

    [Fact]
    public void Parse_single_data_row_rejects_a_column_count_mismatch()
    {
        // Arrange
        var fileContent = Encoding.UTF8.GetBytes("Email,FirstName\na@example.com\n");

        // Act
        Action parse = () => ImportCustomersDuplicateResolutionTestHelper.ParseSingleDataRow(fileContent);
        var exception = parse.ShouldThrow<InvalidDataException>();

        // Assert
        exception.Message.ShouldBe("Expected 2 data value(s) to match the headers, but found 1.");
    }
}
