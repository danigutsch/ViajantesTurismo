using ViajantesTurismo.ArchitectureTests.Repository;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Checks the ownership of Management Web workflow state and components.</summary>
public sealed class ManagementFeatureOwnershipTests
{
    [Theory]
    [InlineData("Bookings/BookingFormModel.cs")]
    [InlineData("Customers/Create/CustomerCreationState.cs")]
    [InlineData("Customers/Create/WizardProgress.razor")]
    [InlineData("Customers/Import/CustomerImportHeaderMatcher.cs")]
    [InlineData("Customers/Import/ImportCustomers.razor")]
    [InlineData("Documents/ReleaseHoldDialog.razor")]
    public void Workflow_files_live_with_their_feature(string relativePath)
    {
        // Arrange
        var repositoryRoot = RepositoryRoot.Locate();
        var components = Path.Combine(repositoryRoot, "src", "ViajantesTurismo.Management.Web", "Components");
        var expectedPath = Path.Combine(components, "Pages", relativePath);

        // Act
        var exists = File.Exists(expectedPath);

        // Assert
        exists.ShouldBeTrue($"Expected a feature-owned file at {expectedPath}.");
    }
}
