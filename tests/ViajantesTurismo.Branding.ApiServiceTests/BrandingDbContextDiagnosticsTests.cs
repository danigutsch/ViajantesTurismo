namespace ViajantesTurismo.Branding.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
public sealed class BrandingDbContextDiagnosticsTests
{
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
    public void Branding_context_does_not_subscribe_to_exception_bearing_npgsql_traces()
    {
        // Arrange
        using var scope = BrandingInfrastructureRegistrationScope.Create();

        // Act
        var sourceEnabled = scope.IsActivitySourceEnabled("Npgsql");
        var hasMeterProvider = scope.HasMeterProvider();

        // Assert
        sourceEnabled.ShouldBeFalse();
        hasMeterProvider.ShouldBeTrue();
    }
}
