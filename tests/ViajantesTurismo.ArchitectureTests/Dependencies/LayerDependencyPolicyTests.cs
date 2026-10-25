namespace ViajantesTurismo.ArchitectureTests.Dependencies;

public sealed class LayerDependencyPolicyTests
{
    [Theory]
    [InlineData("ViajantesTurismo.Catalog.ApiService", "ViajantesTurismo.Branding.AspNetCore", false)]
    [InlineData("ViajantesTurismo.Branding.AspNetCore", "ViajantesTurismo.Branding.Infrastructure", false)]
    [InlineData("ViajantesTurismo.Branding.AspNetCore", "ViajantesTurismo.Catalog.ApiService", true)]
    [InlineData("ViajantesTurismo.Branding.AspNetCore", "ViajantesTurismo.Admin.Infrastructure", true)]
    [InlineData("ViajantesTurismo.Branding.Infrastructure", "ViajantesTurismo.Branding.Contracts.IntegrationEvents", false)]
    [InlineData("ViajantesTurismo.Branding.Infrastructure", "ViajantesTurismo.Admin.Contracts.IntegrationEvents", true)]
    [InlineData("ViajantesTurismo.Branding.Infrastructure", "ViajantesTurismo.Branding.Contracts.Http", true)]
    [InlineData("ViajantesTurismo.Branding.Domain", "ViajantesTurismo.Branding.Contracts.IntegrationEvents", true)]
    [InlineData("ViajantesTurismo.Management.Web", "ViajantesTurismo.Management.Security", false)]
    [InlineData("ViajantesTurismo.Public.Web", "ViajantesTurismo.Management.Security", true)]
    [InlineData("ViajantesTurismo.Management.Web", "ViajantesTurismo.Admin.Infrastructure", true)]
    [InlineData("ViajantesTurismo.Management.Web", "ViajantesTurismo.Management.Security.Other", true)]
    [InlineData("ViajantesTurismo.Branding.AspNetCore", "ViajantesTurismo.Branding.Contracts.Http", false)]
    [InlineData("ViajantesTurismo.Branding.Contracts.Http", "ViajantesTurismo.Branding.Contracts.Application", false)]
    public void Layer_policy_preserves_narrow_adapter_and_host_boundaries(
        string referencingProjectName,
        string referencedProjectName,
        bool expectedViolation)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(referencingProjectName);
        ArgumentNullException.ThrowIfNull(referencedProjectName);

        // Act
        var violation = LayerDependencyTestsHelpers.IsLayerProjectReferenceDirectionViolation(
            referencingProjectName, referencedProjectName);

        // Assert
        violation.ShouldBe(expectedViolation);
    }
}
