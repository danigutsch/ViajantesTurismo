using ViajantesTurismo.Admin.Application.Customers.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class ConflictResolutionTests
{
    [Fact]
    public void Keep_Resolution_Skips_Import_And_Preserves_Existing_Customer()
    {
        // Act
        var resolution = ConflictResolution.Keep;

        // Assert
        Assert.True(resolution.PreservesExistingCustomer);
        Assert.True(resolution.SkipsImport);
    }
}
