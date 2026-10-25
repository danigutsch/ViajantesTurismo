namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>
/// Pins the demand-driven infrastructure of <c>HostedTestFixture</c>: nothing starts before a test needs it, and a container that
/// starts twice would be recreated and lose its data, so every container has exactly one gated, bounded start path.
/// </summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class HostedTestFixtureOnDemandTests
{
    private static readonly string[] ResourceStarts =
    [
        "ResourceNames.DatabaseServer",
        "ResourceNames.Cache",
        "ResourceNames.IdentityProvider",
        "ResourceNames.ClamAv",
        "ResourceNames.SeaweedFs",
        "CollectorName"
    ];

    [Fact]
    public void Fixture_composes_all_six_containers_with_explicit_start()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();
        var creation = HostedTestFixtureSource.Body(fixture, "private async Task<InfrastructureHost> CreateInfrastructure(");
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
        var creation = HostedTestFixtureSource.Body(fixture, "private async Task<InfrastructureHost> CreateInfrastructure(");

        // Assert
        creation.ShouldContain("await AspireTestApplication.Start(builder, [], StartBudget, ct)", StringComparison.Ordinal);
        fixture.ShouldNotContain("[ResourceNames.DatabaseServer", StringComparison.Ordinal);
        creation.ShouldNotContain("WaitFor(", StringComparison.Ordinal, "a WaitFor edge to a resource that never starts blocks the application start");
        creation.ShouldNotContain("AddProject", StringComparison.Ordinal, "only containers belong in the infrastructure model");
    }

    [Fact]
    public void Fixture_has_one_start_path_per_resource_through_the_on_demand_gate()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var constructor = HostedTestFixtureSource.Body(fixture, "public HostedTestFixture()");
        var startCommands = HostedTestFixtureSource.Count(fixture, "KnownResourceCommands.StartCommand");
        var commandExecutions = HostedTestFixtureSource.Count(fixture, "ExecuteCommandAsync(");
        var startCalls = HostedTestFixtureSource.Count(fixture, "StartResource(");
        var gatedStartCalls = HostedTestFixtureSource.Count(constructor, "StartResource(");
        var resourcesStartedOnce = ResourceStarts.Count(resource => HostedTestFixtureSource.Count(constructor, $"StartResource({resource},") == 1);
        var gates = HostedTestFixtureSource.Count(fixture, "private readonly OnDemandResource<");

        // Assert
        startCommands.ShouldBe(1);
        commandExecutions.ShouldBe(1);
        startCalls.ShouldBe(ResourceStarts.Length);
        gatedStartCalls.ShouldBe(ResourceStarts.Length);
        resourcesStartedOnce.ShouldBe(ResourceStarts.Length);
        gates.ShouldBe(ResourceStarts.Length + 1);
    }

    [Fact]
    public void Fixture_reads_infrastructure_endpoints_only_after_the_resource_is_healthy()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var constructor = HostedTestFixtureSource.Body(fixture, "public HostedTestFixture()");
        var start = HostedTestFixtureSource.Body(fixture, "private async Task<T> StartResource<T>(");
        var endpointReads = HostedTestFixtureSource.Count(fixture, ".GetEndpoint(");
        var gatedEndpointReads = HostedTestFixtureSource.Count(constructor, ".GetEndpoint(");
        var connectionStringReads = HostedTestFixtureSource.Count(fixture, ".GetConnectionString(");
        var gatedConnectionStringReads = HostedTestFixtureSource.Count(constructor, ".GetConnectionString(");
        var healthy = start.IndexOf("WaitForResourceHealthyAsync(", StringComparison.Ordinal);
        var read = start.IndexOf("read(host.Application)", StringComparison.Ordinal);

        // Assert
        endpointReads.ShouldBe(gatedEndpointReads);
        connectionStringReads.ShouldBe(gatedConnectionStringReads);
        healthy.ShouldBeGreaterThan(-1);
        read.ShouldBeGreaterThan(healthy);
    }

    [Fact]
    public void Fixture_hands_out_the_database_connection_string_only_after_the_database_is_healthy()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var constructor = HostedTestFixtureSource.Body(fixture, "public HostedTestFixture()");
        var start = HostedTestFixtureSource.Body(fixture, "private async Task<T> StartResource<T>(");
        var serverHealthy = start.IndexOf("WaitForResourceHealthyAsync(resourceName, budget.Token)", StringComparison.Ordinal);
        var databaseHealthy = start.IndexOf("WaitForResourceHealthyAsync(databaseName, budget.Token)", StringComparison.Ordinal);
        var read = start.IndexOf("read(host.Application)", StringComparison.Ordinal);

        // Assert
        constructor.ShouldContain(
            "StartResource(ResourceNames.DatabaseServer, application => application.GetConnectionString(FixtureDatabaseName, ct), ct, FixtureDatabaseName)",
            StringComparison.Ordinal,
            "Aspire creates a server's databases after the server is ready, so the start must wait for the database");
        serverHealthy.ShouldBeGreaterThan(-1);
        databaseHealthy.ShouldBeGreaterThan(serverHealthy);
        read.ShouldBeGreaterThan(databaseHealthy);
    }

    [Fact]
    public void Fixture_names_the_application_whose_start_budget_expires()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var infrastructure = HostedTestFixtureSource.Body(fixture, "private async Task<InfrastructureHost> CreateInfrastructure(");
        var stack = HostedTestFixtureSource.Body(fixture, "private async Task<AspireTestApplication> StartApplication(");
        var applicationStarts = HostedTestFixtureSource.Count(fixture, "AspireTestApplication.Start(");

        // Assert
        applicationStarts.ShouldBe(2);
        infrastructure.ShouldContain(
            "catch (OperationCanceledException cancellation) when (!ct.IsCancellationRequested)",
            StringComparison.Ordinal,
            "an expired budget must surface as a TimeoutException that names the application, not as a bare cancellation that the gate caches");
        infrastructure.ShouldContain("throw new TimeoutException($\"The infrastructure application did not start within {StartBudget}.\", cancellation);", StringComparison.Ordinal);
        stack.ShouldContain("catch (OperationCanceledException cancellation) when (!ct.IsCancellationRequested)", StringComparison.Ordinal);
        stack.ShouldContain("throw new TimeoutException($\"The '{stack}' application stack did not start within {StartBudget}.\", cancellation);", StringComparison.Ordinal);
    }

    [Fact]
    public void Fixture_bounds_each_resource_start_with_its_own_budget()
    {
        // Arrange
        var fixture = HostedTestFixtureSource.Read();

        // Act
        var start = HostedTestFixtureSource.Body(fixture, "private async Task<T> StartResource<T>(");

        // Assert
        HostedTestFixtureSource.Count(fixture, "StartBudget = TimeSpan.FromMinutes(3);").ShouldBe(1);
        start.ShouldContain("budget.CancelAfter(StartBudget);", StringComparison.Ordinal);
        start.ShouldContain("ExecuteCommandAsync(resourceName, KnownResourceCommands.StartCommand, budget.Token)", StringComparison.Ordinal);
        start.ShouldContain("WaitForResourceHealthyAsync(resourceName, budget.Token)", StringComparison.Ordinal);
        start.ShouldContain("if (!started.Success)", StringComparison.Ordinal);
    }
}
