using static ViajantesTurismo.ArchitectureTests.Conventions.WorkerOwnershipTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class WorkerOwnershipTests
{
    [Fact]
    public void Every_worker_host_belongs_to_one_bounded_context()
    {
        // Arrange
        var workers = FindWorkerProjects();

        // Act
        var unowned = workers.Where(worker => FindOwningContext(worker) is null).ToArray();

        // Assert
        workers.ShouldNotBeEmpty();
        unowned.ShouldBeEmpty();
    }

    [Fact]
    public void Worker_hosts_reference_only_their_own_context_projects()
    {
        // Arrange
        var workers = FindWorkerProjects();

        // Act
        var violations = workers
            .SelectMany(worker => FindOwningContext(worker) is { } context
                ? FindForeignContextReferences(worker, context).Select(reference => $"{worker} -> {reference}")
                : [])
            .ToArray();

        // Assert
        violations.ShouldBeEmpty();
    }
}
