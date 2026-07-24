using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
public sealed class TelemetryPrivacyConfigurationTests
{
    [Fact]
    public void Npgsql_registrations_share_tracing_privacy_configuration_without_disabling_tracing_or_parameter_logging()
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
        var sharedNpgsql = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "SharedKernel",
            "SharedKernel.Npgsql",
            "NpgsqlDataSourceBuilderExtensions.cs"));

        // Act
        var combinedSource = string.Concat(adminInfrastructure, brandingInfrastructure, catalogInfrastructure);
        var tracingConfigurationSource = string.Concat(sharedNpgsql, combinedSource);

        // Assert
        sharedNpgsql.ShouldContain("ConfigureTracingWithoutFirstResponseEvent", StringComparison.Ordinal);
        sharedNpgsql.ShouldContain("EnableFirstResponseEvent(enable: false)", StringComparison.Ordinal);
        (combinedSource.Split("ConfigureTracingWithoutFirstResponseEvent()", StringSplitOptions.None).Length - 1).ShouldBe(3);
        combinedSource.ShouldNotContain("EnableFirstResponseEvent", StringComparison.Ordinal);
        tracingConfigurationSource.ShouldNotContain("DisableTracing = true", StringComparison.Ordinal);
        tracingConfigurationSource.ShouldNotContain("EnableParameterLogging", StringComparison.Ordinal);
    }
}
