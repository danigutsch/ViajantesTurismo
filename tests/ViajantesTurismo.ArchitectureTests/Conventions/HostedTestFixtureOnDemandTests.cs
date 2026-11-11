namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>
/// Pins the demand-driven infrastructure of <c>HostedTestFixture</c>: nothing starts before a test needs it, and a container that
/// starts twice would be recreated and lose its data, so every container starts only through the keyed on-demand gate.
/// SharedKernel's <c>OnDemandResources</c> guarantees one start per key, and <c>AspireTestApplication.StartResource</c> bounds each start
/// and names the pending resource when its budget expires.
/// </summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class HostedTestFixtureOnDemandTests
{
    private const string Creation = "private Task<AspireTestApplication> CreateInfrastructure(";
    private const string ContainerStart = "private async Task<StartedContainer> StartContainer(";

    [Fact]
    public void Fixture_composes_all_six_containers_with_explicit_start()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();
        var creation = HostedTestFixtureSource.Body(fixture, Creation);
        string[] compositions =
        [
            "builder.AddDatabaseServer(includePgWeb: false)",
            "builder.AddCache(includeRedisInsight: false)",
            "builder.AddClamAv(",
            "builder.AddSeaweedFs(",
            "builder.AddRunModeIdentityProvider(",
            "builder.AddOpenTelemetryCollectorGateway("
        ];

        // Act
        var composedOnce = compositions.Count(composition => HostedTestFixtureSource.Count(creation, composition) == 1);
        var resourcesAdded = HostedTestFixtureSource.Count(creation, "builder.Add") - HostedTestFixtureSource.Count(creation, "builder.AddParameter(");
        var explicitStarts = HostedTestFixtureSource.Count(creation, ".WithExplicitStart()");

        // Assert
        composedOnce.ShouldBe(compositions.Length);
        resourcesAdded.ShouldBe(compositions.Length);
        explicitStarts.ShouldBe(compositions.Length);
    }

    [Fact]
    public void Fixture_initialization_starts_nothing()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Assert
        fixture.ShouldContain("public ValueTask InitializeAsync() => ValueTask.CompletedTask;", StringComparison.Ordinal);
    }

    [Fact]
    public void Fixture_starts_the_infrastructure_application_without_awaiting_any_resource()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var creation = HostedTestFixtureSource.Body(fixture, Creation);

        // Assert
        creation.ShouldContain("AspireTestApplication.Start(builder, [], StartBudget, ct)", StringComparison.Ordinal);
        creation.ShouldNotContain("WaitFor(", StringComparison.Ordinal, "a WaitFor edge to a resource that never starts blocks the application start");
        creation.ShouldNotContain("AddProject", StringComparison.Ordinal, "only containers belong in the infrastructure model");
    }

    [Fact]
    public void Fixture_starts_containers_only_through_the_keyed_on_demand_gate()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var constructor = HostedTestFixtureSource.Body(fixture, "public HostedTestFixture()");
        var start = HostedTestFixtureSource.Body(fixture, ContainerStart);
        var startCalls = HostedTestFixtureSource.Count(fixture, "StartResource(");
        var commandExecutions = HostedTestFixtureSource.Count(fixture, "ExecuteCommandAsync(");
        var containerStartReferences = HostedTestFixtureSource.Count(fixture, "StartContainer(");

        // Assert
        constructor.ShouldContain("containers = new(StartContainer);", StringComparison.Ordinal);
        start.ShouldContain("await application.StartResource(resourceName, StartBudget, ct);", StringComparison.Ordinal);
        startCalls.ShouldBe(1, "a start outside the gate would start a container twice");
        commandExecutions.ShouldBe(0, "SharedKernel's StartResource issues the start command");
        containerStartReferences.ShouldBe(1, "the gate is the only caller of StartContainer");
        fixture.ShouldNotContain("BeforeStartEvent", StringComparison.Ordinal);
        fixture.ShouldNotContain("DistributedApplicationTestingBuilder", StringComparison.Ordinal);
        fixture.ShouldNotContain("ResourceNameSuffix", StringComparison.Ordinal, "SharedKernel suffixes every test application");
    }

    [Fact]
    public void Fixture_starts_every_resource_with_the_token_its_gate_hands_out()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Assert
        fixture.ShouldNotContain("TestContext.Current", StringComparison.Ordinal, "a start must not use the token of one test");
    }

    [Fact]
    public void Fixture_starts_clamav_with_the_definitions_of_its_image()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var creation = HostedTestFixtureSource.Body(fixture, Creation);

        // Assert
        creation.ShouldContain(
            "builder.AddClamAv(ResourceNames.ClamAv).WithFreshClam(false)",
            StringComparison.Ordinal,
            "a test run must not download virus definitions from the ClamAV mirrors when the container starts");
    }

    [Fact]
    public void Fixture_mounts_no_data_volume()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var creation = HostedTestFixtureSource.Body(fixture, Creation);
        var volumes = HostedTestFixtureSource.Count(creation, ".WithVolume(") + HostedTestFixtureSource.Count(creation, ".WithDataVolume(");
        var dataBindMounts = HostedTestFixtureSource.Count(creation, ".WithDataBindMount(");
        var bindMounts = HostedTestFixtureSource.Count(creation, ".WithBindMount(");
        var realmImportSettings = HostedTestFixtureSource.Count(creation, "RealmImportDirectoryConfigurationKey");

        // Assert
        volumes.ShouldBe(0, "a named volume outlives its container, so a hosted run would leave it behind");
        dataBindMounts.ShouldBe(0, "a hosted run keeps no resource data outside its containers");
        bindMounts.ShouldBe(0, "the AppHost mounts the realm import; the fixture only points it at the run's directory");
        realmImportSettings.ShouldBe(1, "the fixture configures the realm import directory instead of replacing the AppHost mount");
    }

    [Fact]
    public void Fixture_reads_infrastructure_endpoints_only_from_a_started_container()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var endpointReads = HostedTestFixtureSource.Count(fixture, ".GetEndpoint(");
        var gatedEndpointReads = HostedTestFixtureSource.Count(fixture, ").Application.GetEndpoint(");
        var connectionStringReads = HostedTestFixtureSource.Count(fixture, ".GetConnectionString(");
        var gatedConnectionStringReads = HostedTestFixtureSource.Count(fixture, ").Application.GetConnectionString(");

        // Assert
        endpointReads.ShouldBe(gatedEndpointReads);
        connectionStringReads.ShouldBe(gatedConnectionStringReads);
    }

    [Fact]
    public void Fixture_hands_out_the_database_connection_string_only_after_the_database_is_healthy()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var start = HostedTestFixtureSource.Body(fixture, ContainerStart);
        var serverHealthy = start.IndexOf("StartResource(resourceName", StringComparison.Ordinal);
        var databaseHealthy = start.IndexOf("WaitForHealthy([FixtureDatabaseName], StartBudget, ct)", StringComparison.Ordinal);
        var started = start.IndexOf("return new StartedContainer(application);", StringComparison.Ordinal);

        // Assert
        serverHealthy.ShouldBeGreaterThan(-1);
        databaseHealthy.ShouldBeGreaterThan(serverHealthy, "Aspire creates a server's databases after the server is ready");
        started.ShouldBeGreaterThan(databaseHealthy);
    }

    [Fact]
    public void Fixture_bounds_every_start_with_one_budget()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var budgetDeclarations = HostedTestFixtureSource.Count(fixture, "StartBudget = TimeSpan.FromMinutes(3);");
        var stacks = HostedTestFixtureSource.Body(fixture, "protected override AppHostLaunch Launch(");

        // Assert
        budgetDeclarations.ShouldBe(1, "one named budget bounds every start");
        stacks.ShouldContain("healthy, StartBudget, configuration)", StringComparison.Ordinal);
    }
}
