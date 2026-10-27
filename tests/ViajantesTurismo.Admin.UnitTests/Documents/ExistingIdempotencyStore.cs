using PandaBear.Idempotency;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>
/// Idempotency store fake that reports one preexisting entry for its operation.
/// </summary>
/// <param name="entry">The entry returned for its own operation.</param>
internal sealed class ExistingIdempotencyStore(IdempotencyEntry entry) : IIdempotencyStore
{
    /// <summary>Reports the entry as already started after checking the operation matches.</summary>
    public ValueTask<IdempotencyStartResult> TryStart(
        IdempotencyOperation operation,
        DateTimeOffset startedAt,
        TimeSpan? lockDuration,
        CancellationToken ct)
    {
        EnsureExpectedOperation(operation);
        return ValueTask.FromResult(IdempotencyStartResult.AlreadyStarted(entry));
    }

    /// <summary>Always throws because a replay cannot be completed again.</summary>
    public ValueTask Complete(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct) => throw new InvalidOperationException("A completed replay cannot be completed again.");

    /// <summary>Always throws because an existing operation cannot be completed again.</summary>
    public ValueTask StageCompletion(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct) => throw new InvalidOperationException("An existing operation cannot be completed again.");

    /// <summary>Returns the entry after checking the operation matches.</summary>
    public ValueTask<IdempotencyEntry?> Get(
        IdempotencyOperation operation,
        CancellationToken ct)
    {
        EnsureExpectedOperation(operation);
        return ValueTask.FromResult<IdempotencyEntry?>(entry);
    }

    private void EnsureExpectedOperation(IdempotencyOperation operation)
    {
        if (operation != entry.Operation)
        {
            throw new InvalidOperationException("The idempotency operation did not match the configured entry.");
        }
    }
}
