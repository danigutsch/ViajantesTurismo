using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
public sealed class TelemetryPrivacyConfigurationTests
{
    [Fact]
    public void Npgsql_registrations_disable_first_response_events_without_disabling_tracing_or_enabling_parameter_logging()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();
        var adminInfrastructure = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "ViajantesTurismo.Admin.Infrastructure",
            "InfrastructureDependencyInjection.cs"));
        var brandingInfrastructure = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "ViajantesTurismo.Branding.Infrastructure",
            "BrandingInfrastructureDependencyInjection.cs"));
        var catalogInfrastructure = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "ViajantesTurismo.Catalog.Infrastructure",
            "InfrastructureDependencyInjection.cs"));

        // Act
        var combinedSource = string.Concat(adminInfrastructure, brandingInfrastructure, catalogInfrastructure);

        // Assert
        adminInfrastructure.ShouldContain("EnableFirstResponseEvent(enable: false)", StringComparison.Ordinal);
        brandingInfrastructure.ShouldContain("EnableFirstResponseEvent(enable: false)", StringComparison.Ordinal);
        catalogInfrastructure.ShouldContain("EnableFirstResponseEvent(enable: false)", StringComparison.Ordinal);
        combinedSource.ShouldNotContain("DisableTracing = true", StringComparison.Ordinal);
        combinedSource.ShouldNotContain("EnableParameterLogging", StringComparison.Ordinal);
    }
}
