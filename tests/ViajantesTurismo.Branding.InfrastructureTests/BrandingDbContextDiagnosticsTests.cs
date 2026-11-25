namespace ViajantesTurismo.Branding.InfrastructureTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
public sealed class BrandingDbContextDiagnosticsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Branding_logo_reclamation_runs_in_the_catalog_host_only(bool runtimeHost)
    {
        // Arrange
        using var scope = BrandingInfrastructureRegistrationScope.Create(addOutboxRelay: runtimeHost);

        // Act
        var registered = scope.HostedServiceNames().Contains("BrandingLogoReclamationHostedService", StringComparer.Ordinal);

        // Assert
        registered.ShouldBe(runtimeHost);
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    public void Branding_context_gates_sensitive_logging_by_environment(string environmentName, bool expected)
    {
        // Arrange
        using var scope = BrandingInfrastructureRegistrationScope.Create(environmentName);

        // Act
        var sensitiveLogging = scope.IsSensitiveDataLoggingEnabled();

        // Assert
        sensitiveLogging.ShouldBe(expected);
    }

    [Fact]
    public void Branding_context_registers_npgsql_tracing_and_metrics()
    {
        // Arrange
        using var scope = BrandingInfrastructureRegistrationScope.Create();

        // Act
        var sourceEnabled = scope.IsActivitySourceEnabled("Npgsql");
        var hasMeterProvider = scope.HasMeterProvider();

        // Assert
        sourceEnabled.ShouldBeTrue();
        hasMeterProvider.ShouldBeTrue();
    }
}
