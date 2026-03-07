using ViajantesTurismo.Admin.Application.Customers.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class ImportResultTests
{
    [Fact]
    public void Create_With_Error_Count_Exposes_ErrorCount()
    {
        // Act
        var result = new ImportResult(2, 1);

        // Assert
        Assert.Equal(1, result.ErrorCount);
    }

    [Fact]
    public void Create_With_Success_Count_Exposes_SuccessCount()
    {
        // Act
        var result = new ImportResult(3);

        // Assert
        Assert.Equal(3, result.SuccessCount);
    }

    [Fact]
    public void Create_With_Negative_Success_Count_Throws_ArgumentOutOfRangeException()
    {
        // Act
        static void Action() => _ = new ImportResult(-1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(Action);
    }
}
