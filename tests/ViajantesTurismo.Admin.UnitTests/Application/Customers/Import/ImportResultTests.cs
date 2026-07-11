using ViajantesTurismo.Admin.Application.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class ImportResultTests
{
    [Fact]
    public void Create_with_negative_success_count_throws_argumentoutofrangeexception()
    {
        // Act
        var exception = TestAssert.Throws<ArgumentOutOfRangeException>(() => _ = new ImportResult(-1));

        // Assert
        TestAssert.Equal("successCount", exception.ParamName);
    }
}
