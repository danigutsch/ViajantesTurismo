using PandaBear.Testing;

namespace ViajantesTurismo.Catalog.InfrastructureTests.Media;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class CatalogTourMutationLockPostgreSqlTests(HostedTestFixture shared) : IAsyncLifetime
{
    private GalleryPlacementPostgreSqlScenario? scenario;

    private GalleryPlacementPostgreSqlScenario Scenario =>
        scenario ?? throw new InvalidOperationException("Test scenario is not initialized.");

    public async ValueTask InitializeAsync()
    {
        scenario = await GalleryPlacementPostgreSqlScenario.Create(shared, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var current = scenario;
        scenario = null;
        if (current is not null)
        {
            await current.DisposeAsync();
        }
    }

    [Fact]
    public async Task Waiting_for_a_mutation_ignores_locks_in_other_test_databases()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var sibling = await GalleryPlacementPostgreSqlScenario.Create(shared, ct);
        var tourId = Guid.CreateVersion7();
        var first = sibling.CreateMutationLock();
        var second = sibling.CreateMutationLock();
        await using var held = await first.Acquire([tourId], ct);
        var waiting = second.Acquire([tourId], ct).AsTask();

        await using var pending = new AdvisoryLockWaitScope(held.DisposeAsync, async () =>
        {
            await using var acquired = await waiting.WaitAsync(ct);
        });
        await sibling.WaitForBlockedMutation(ct);
        using var observation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        observation.CancelAfter(TimeSpan.FromSeconds(1));

        // Act
        Func<Task> observe = () => Scenario.WaitForBlockedMutation(observation.Token);

        // Assert
        await observe.ShouldThrowAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task Tour_mutations_wait_across_lock_instances_and_release_all_linked_tours()
    {
        // Arrange
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        var ct = timeout.Token;
        var firstTour = Guid.CreateVersion7();
        var secondTour = Guid.CreateVersion7();
        var first = Scenario.CreateMutationLock();
        var second = Scenario.CreateMutationLock();
        await using var held = await first.Acquire([secondTour, firstTour], ct);

        // Act
        var waiting = second.Acquire([firstTour, secondTour], ct).AsTask();
        await Scenario.WaitForBlockedMutation(ct);
        await using var unrelated = await second.Acquire([Guid.CreateVersion7()], ct);

        // Assert
        waiting.IsCompleted.ShouldBeFalse();

        // Act
        await held.DisposeAsync();
        await using var acquired = await waiting.WaitAsync(ct);

        // Assert
        waiting.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task A_canceled_multi_tour_acquisition_releases_its_already_acquired_locks()
    {
        // Arrange
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        var ct = timeout.Token;
        var firstTour = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondTour = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var first = Scenario.CreateMutationLock();
        var second = Scenario.CreateMutationLock();
        await using var held = await first.Acquire([secondTour], ct);
        using var canceled = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Act
        var waiting = second.Acquire([secondTour, firstTour], canceled.Token).AsTask();
        await Scenario.WaitForBlockedMutation(ct);
        await canceled.CancelAsync();
        Func<Task> acquire = async () => await waiting;

        // Assert
        await acquire.ShouldThrow<OperationCanceledException>();
        await using var recovered = await second.Acquire([firstTour], ct);
    }
}
