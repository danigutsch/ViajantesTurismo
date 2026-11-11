namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>
/// Pins the guards of <c>HostedTestFixture</c> that no other test reads: telemetry privacy, the shared-infrastructure marker of
/// every application stack, and ownership of the work directory. Restructuring the fixture must not drop them silently.
/// </summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class HostedTestFixtureSafetyTests
{
    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.SecurityCategory)]
    public void Fixture_gives_the_shared_collector_the_privacy_and_aspire_configuration()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.ReadNormalized();

        // Assert
        fixture.ShouldContain(
            "builder.AddOpenTelemetryCollectorGateway(CollectorName, Path.Combine(observability, \"otel-collector\", \"privacy.yaml\"), "
            + "Path.Combine(observability, \"otel-collector\", \"aspire.yaml\"))",
            StringComparison.Ordinal);
    }

    [Fact]
    public void Fixture_marks_every_application_stack_as_using_the_shared_infrastructure()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var stack = HostedTestFixtureSource.Body(fixture, "private async Task<AspireTestApplication> StartApplication(");

        // Assert
        stack.ShouldContain("[\"HostedTests:SharedInfrastructure\"] = \"true\"", StringComparison.Ordinal);
    }

    [Fact]
    public void Fixture_deletes_the_unique_work_directory_it_creates()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var disposal = HostedTestFixtureSource.Body(fixture, "public async ValueTask DisposeAsync()");

        // Assert
        fixture.ShouldContain("WorkDirectory { get; } = Path.Combine(Path.GetTempPath(), $\"vt-hosted-{Guid.NewGuid():N}\");", StringComparison.Ordinal);
        disposal.ShouldContain("Directory.Delete(WorkDirectory, recursive: true);", StringComparison.Ordinal);
    }
}
