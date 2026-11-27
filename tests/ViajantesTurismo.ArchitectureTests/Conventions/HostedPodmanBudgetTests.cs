using Aspire.Hosting;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Checks shared Podman budget wiring at the two hosted infrastructure composition boundaries.</summary>
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class HostedPodmanBudgetTests
{
    [Theory]
    [InlineData("Infrastructure/HostedTestFixture.cs", "private Task<AspireTestApplication> CreateInfrastructure(",
        "AddOpenTelemetryCollectorGateway(", 0)]
    [InlineData("Catalog/Media/Infrastructure/SeaweedFsWebIdentityScenario.cs", "public static async Task<SeaweedFsWebIdentityScenario> Start(",
        "WaitFor(identity);", 2)]
    public void Both_owned_container_models_apply_one_shared_budget_after_composition_and_before_startup(
        string fixture, string creationSignature, string lastResource, int existingLogArguments)
    {
        // Arrange
        var path = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "tests", "ViajantesTurismo.HostedTests", fixture);
        var source = File.ReadAllText(path);

        // Act
        var creation = HostedTestFixtureSource.Body(source, creationSignature);
        var lastRegistration = creation.IndexOf(lastResource, StringComparison.Ordinal);
        var startup = creation.IndexOf("AspireTestApplication.Start(builder,", StringComparison.Ordinal);
        var applyBudget = creation.IndexOf("PodmanContainerBudget.Apply(builder);", StringComparison.Ordinal);
        var budgetCalls = HostedTestFixtureSource.Count(creation, "PodmanContainerBudget.Apply(builder);");
        var logArguments = HostedTestFixtureSource.Count(creation, ".WithContainerRuntimeArgs(\"--log-driver=json-file\")");

        // Assert
        logArguments.ShouldBe(existingLogArguments);
        lastRegistration.ShouldBeGreaterThanOrEqualTo(0);
        startup.ShouldBeGreaterThan(lastRegistration);
        budgetCalls.ShouldBe(1, "the fixture must wire its complete container model to the same opt-in budget");
        applyBudget.ShouldBeGreaterThan(lastRegistration);
        applyBudget.ShouldBeLessThan(startup);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Aspire_model_arguments_can_append_one_slice_and_keep_the_existing_storage_log_driver(bool optIn)
    {
        // Arrange
        const string slice = "vt634.slice";
        const string logging = "--log-driver=json-file";
        var builder = AppHostTestBuilder.Create();
        var infrastructure = builder.AddContainer("infrastructure", "postgres");
        var storage = builder.AddContainer("storage", "seaweedfs").WithContainerRuntimeArgs(logging);
        var ct = TestContext.Current.CancellationToken;
        string[] expectedInfrastructure = optIn ? ["--cgroup-parent", slice] : [];
        string[] expectedStorage = [logging, .. expectedInfrastructure];

        // Act
        if (optIn)
        {
            infrastructure.WithContainerRuntimeArgs("--cgroup-parent", slice);
            storage.WithContainerRuntimeArgs("--cgroup-parent", slice);
        }

        var infrastructureArguments = await HostedPodmanBudgetModel.Evaluate(infrastructure.Resource, ct);
        var storageArguments = await HostedPodmanBudgetModel.Evaluate(storage.Resource, ct);

        // Assert
        infrastructureArguments.ShouldBe(expectedInfrastructure);
        storageArguments.ShouldBe(expectedStorage);
    }
}
