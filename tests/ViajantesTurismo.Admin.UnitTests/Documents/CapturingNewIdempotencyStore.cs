using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>
/// Idempotency store fake that starts a new entry (or reports a competing one) and captures start and completion arguments.
/// </summary>
/// <param name="unitOfWork">The unit of work whose save count is inspected when completion is staged.</param>
/// <param name="competingEntry">An existing entry reported as already started, or null to start a new one.</param>
internal sealed class CapturingNewIdempotencyStore(
    FakeUnitOfWork unitOfWork,
    IdempotencyEntry? competingEntry = null) : IIdempotencyStore
{
    private IdempotencyEntry? entry;

    /// <summary>Number of calls to <c>Get</c>.</summary>
    internal int GetCallCount
    {
        get; private set;
    }

    /// <summary>Cancellation token passed to the last <c>Get</c>.</summary>
    internal CancellationToken LastGetCancellationToken
    {
        get; private set;
    }

    /// <summary>Cancellation token passed to the last <c>StageCompletion</c>.</summary>
    internal CancellationToken LastCompletionCancellationToken
    {
        get; private set;
    }

    /// <summary>Number of calls to <c>TryStart</c>.</summary>
    internal int TryStartCallCount
    {
        get; private set;
    }

    /// <summary>Operation passed to the last <c>TryStart</c>.</summary>
    internal IdempotencyOperation? StartedOperation
    {
        get; private set;
    }

    /// <summary>Start time passed to the last <c>TryStart</c>.</summary>
    internal DateTimeOffset? StartedAt
    {
        get; private set;
    }

    /// <summary>Lock duration passed to the last <c>TryStart</c>.</summary>
    internal TimeSpan? LockDuration
    {
        get; private set;
    }

    /// <summary>Number of calls to <c>StageCompletion</c>.</summary>
    internal int StageCompletionCallCount
    {
        get; private set;
    }

    /// <summary>Operation passed to the last <c>StageCompletion</c>.</summary>
    internal IdempotencyOperation? StagedOperation
    {
        get; private set;
    }

    /// <summary>Completion time passed to the last <c>StageCompletion</c>.</summary>
    internal DateTimeOffset? CompletedAt
    {
        get; private set;
    }

    /// <summary>Result fingerprint passed to the last <c>StageCompletion</c>.</summary>
    internal string? StagedResultFingerprint
    {
        get; private set;
    }

    /// <summary>True when completion was staged before any save of the unit of work.</summary>
    internal bool WasCompletionStagedBeforeSave
    {
        get; private set;
    }

    /// <summary>Captures the arguments, then reports the competing entry as already started or starts a new entry.</summary>
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

    /// <summary>Always throws because completion must be staged for the caller-owned unit of work.</summary>
    public ValueTask Complete(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct) => throw new InvalidOperationException(
            "Document command completion must be staged for the caller-owned unit of work.");

    /// <summary>Captures the completion arguments and stores a completed entry.</summary>
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

    /// <summary>Returns the stored entry when it matches the operation, otherwise null.</summary>
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
