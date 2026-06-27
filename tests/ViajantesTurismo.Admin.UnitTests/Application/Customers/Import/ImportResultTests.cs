using ViajantesTurismo.Admin.Application.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class ImportResultTests
{
    [Fact]
    public void Create_with_negative_success_count_throws_argumentOutOfRangeException()
    {
        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _ = new ImportResult(-1));

        // Assert
        Assert.Equal("successCount", exception.ParamName);
    }
}
