using SharedKernel.EventSourcing;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CapturingProjectionCheckpointStore : IProjectionCheckpointStore
{
    public ProjectionCheckpoint? CurrentCheckpoint { get; set; }

    public ProjectionCheckpoint? SavedCheckpoint { get; private set; }

    public ValueTask<ProjectionCheckpoint?> GetCheckpoint(string projectionName, CancellationToken ct)
    {
        return ValueTask.FromResult(CurrentCheckpoint);
    }

    public ValueTask Save(ProjectionCheckpoint checkpoint, CancellationToken ct)
    {
        SavedCheckpoint = checkpoint;
        CurrentCheckpoint = checkpoint;

        return ValueTask.CompletedTask;
    }
}
