using ViajantesTurismo.Admin.Application.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class ImportResultTests
{
    [Fact]
    public void Create_With_Negative_Success_Count_Throws_ArgumentOutOfRangeException()
    {
        // Act
        static void Action() => _ = new ImportResult(-1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(Action);
    }
}
