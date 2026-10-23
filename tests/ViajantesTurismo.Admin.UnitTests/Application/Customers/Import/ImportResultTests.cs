using ViajantesTurismo.Admin.Application.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class ImportResultTests
{
    [Fact]
    public void Create_with_negative_success_count_throws_argumentoutofrangeexception()
    {
        // Act
        var exception = ShouldAssertionExtensions.ShouldThrow<ArgumentOutOfRangeException>((Func<object?>)(() => _ = new ImportResult(-1)));

        // Assert
        exception.ParamName.ShouldBe("successCount");
    }
}
