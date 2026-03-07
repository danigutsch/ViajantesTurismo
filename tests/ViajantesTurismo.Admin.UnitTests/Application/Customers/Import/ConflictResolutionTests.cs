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

    [Fact]
    public void Overwrite_Resolution_Does_Not_Skip_Import_And_Does_Not_Preserve_Existing_Customer()
    {
        // Act
        var resolution = ConflictResolution.Overwrite;

        // Assert
        Assert.False(resolution.PreservesExistingCustomer);
        Assert.False(resolution.SkipsImport);
    }
}
