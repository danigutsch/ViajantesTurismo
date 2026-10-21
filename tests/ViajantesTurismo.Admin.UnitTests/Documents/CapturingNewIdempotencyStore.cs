using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal sealed class CapturingNewIdempotencyStore(
    FakeUnitOfWork unitOfWork,
    IdempotencyEntry? competingEntry = null) : IIdempotencyStore
{
    private IdempotencyEntry? entry;

    internal int GetCallCount { get; private set; }

    internal CancellationToken LastGetCancellationToken { get; private set; }

    internal CancellationToken LastCompletionCancellationToken { get; private set; }

    internal int TryStartCallCount { get; private set; }

    internal IdempotencyOperation? StartedOperation { get; private set; }

    internal DateTimeOffset? StartedAt { get; private set; }

    internal TimeSpan? LockDuration { get; private set; }

    internal int StageCompletionCallCount { get; private set; }

    internal IdempotencyOperation? StagedOperation { get; private set; }

    internal DateTimeOffset? CompletedAt { get; private set; }

    internal string? StagedResultFingerprint { get; private set; }

    internal bool WasCompletionStagedBeforeSave { get; private set; }

    public ValueTask<IdempotencyStartResult> TryStart(
        IdempotencyOperation operation,
        DateTimeOffset startedAt,
        TimeSpan? lockDuration,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        TryStartCallCount++;
        StartedOperation = operation;
        StartedAt = startedAt;
        LockDuration = lockDuration;
        if (entry is not null)
        {
            return ValueTask.FromResult(IdempotencyStartResult.AlreadyStarted(entry));
        }

        if (competingEntry is not null)
        {
            entry = competingEntry;
            return ValueTask.FromResult(IdempotencyStartResult.AlreadyStarted(competingEntry));
        }

        entry = new IdempotencyEntry(
            operation,
            IdempotencyEntryState.Started,
            startedAt,
            null,
            null);
        return ValueTask.FromResult(IdempotencyStartResult.StartedNew());
    }

    public ValueTask Complete(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct) => throw new InvalidOperationException(
            "Document command completion must be staged for the caller-owned unit of work.");

    public ValueTask StageCompletion(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct)
    {
        LastCompletionCancellationToken = ct;
        ct.ThrowIfCancellationRequested();
        StageCompletionCallCount++;
        StagedOperation = operation;
        CompletedAt = completedAt;
        StagedResultFingerprint = resultFingerprint;
        WasCompletionStagedBeforeSave = unitOfWork.SaveEntitiesCallCount == 0;
        entry = new IdempotencyEntry(
            operation,
            IdempotencyEntryState.Completed,
            StartedAt ?? completedAt,
            completedAt,
            resultFingerprint);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IdempotencyEntry?> Get(
        IdempotencyOperation operation,
        CancellationToken ct)
    {
        LastGetCancellationToken = ct;
        ct.ThrowIfCancellationRequested();
        GetCallCount++;
        return ValueTask.FromResult(entry?.Operation == operation ? entry : null);
    }
}
